using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Security.Cryptography;
using System.Text;
using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Engine.Content;

namespace DeepSpaceSaga.Engine;

/// <summary>
/// Authoritative trade quote issuer (EP-0001-US-0015-TK-0004): prices one trade at the ship's docked station
/// along the sequential curve of <see cref="TradeQuoteCalculator"/>, computes the exact maximum over every
/// authoritative limit, binds the result to station, ship, module, command type, item, quantity and market
/// revision, and keeps it in a bounded session cache for the executor (SimulationEngine.TradeExecution.cs).
/// The cache, the RequestId map and the nonce are session state only — never simulated, never saved — so
/// every load starts a fresh quote session and every earlier quote becomes stale.
/// </summary>
public sealed partial class SimulationEngine
{
    private const int QuoteCacheLimit = 1024;
    private const string RequestIdConflict = "request_id_conflict";

    private readonly Dictionary<string, IssuedQuote> _issuedQuotes = new(StringComparer.Ordinal);
    private readonly Dictionary<string, string> _quoteIdByRequestId = new(StringComparer.Ordinal);
    private readonly Queue<(string QuoteId, string? RequestId)> _issuedQuoteOrder = new();
    private string _quoteNonce = "";
    private long _quoteCounter;

    private sealed record IssuedQuote(TradeQuoteSnapshot Quote, QuoteContext Context);

    /// <summary>
    /// Everything a quote's numbers depend on. A quote is only executable while the current context equals the
    /// one captured at issue time, which also catches changes that are not market transactions (player money,
    /// port fees, docking, module state, the ship's cargo and tank, station Credits, active price factors).
    /// </summary>
    private readonly record struct QuoteContext(
        string StationObjectId,
        long MarketRevision,
        long PlayerCredits,
        long StationCredits,
        long? StationBudgetCredits,
        long StockQuantity,
        long? TargetStock,
        long? MaxStock,
        string PriceFactors,
        bool IsDocked,
        string? DockedStationObjectId,
        bool ModuleCanExecute,
        long CargoQuantity,
        long FreeCargoKg,
        long FuelAmountKg,
        long FuelCapacityKg);

    /// <summary>Quote numbers for the current state; <see cref="DisabledReason"/> is set when nothing can execute.</summary>
    private readonly record struct QuoteTerms(
        long MaximumQuantity,
        long ExecutableQuantity,
        TradeQuotePriceResult? Price,
        string? DisabledReason,
        ImmutableArray<string> LimitReasons);

    /// <summary>
    /// Price one trade at the ship's docked station without changing the world. Invalid gameplay input yields a
    /// disabled quote whose reason is the same code the command itself would be rejected with; a disabled quote
    /// is never cached. A repeated <see cref="TradeQuoteRequest.RequestId"/> with the same binding returns the
    /// same quote while it is still current; with another binding it is refused as <c>request_id_conflict</c>.
    /// </summary>
    public TradeQuoteSnapshot GetTradeQuote(TradeQuoteRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        lock (_worldStateLock)
        {
            bool resolved = TryResolveTradeTarget(request.ObjectId, request.ModuleId, request.CommandType,
                request.ItemTypeId, request.Quantity, out var target, out string reasonCode);

            if (!string.IsNullOrEmpty(request.RequestId) &&
                _quoteIdByRequestId.TryGetValue(request.RequestId, out var knownQuoteId) &&
                _issuedQuotes.TryGetValue(knownQuoteId, out var known))
            {
                // A live quote owns this RequestId: another binding may neither replace nor evict it.
                if (!HasSameBinding(known.Quote, request))
                    return DisabledQuote(request, RequestIdConflict, 0);
                if (resolved && CaptureQuoteContext(target) == known.Context)
                    return known.Quote;
            }

            if (!resolved)
                return DisabledQuote(request, reasonCode, 0);

            QuoteTerms terms;
            QuoteContext context;
            try
            {
                terms = ComputeQuoteTerms(target, request.CommandType);
                if (terms.DisabledReason is { } disabled)
                    return DisabledQuote(request, disabled, terms.MaximumQuantity);
                context = CaptureQuoteContext(target);
            }
            catch (OverflowException)
            {
                return DisabledQuote(request, ValueOverflow, 0);
            }

            var price = terms.Price!;
            var quote = new TradeQuoteSnapshot(
                request.RequestId,
                $"QTE-{_quoteNonce}-{++_quoteCounter}",
                target.Station.MarketRevision,
                target.Station.InitialMotion.ObjectId,
                request.ObjectId,
                request.ModuleId,
                request.CommandType,
                request.ItemTypeId,
                request.Quantity,
                terms.ExecutableQuantity,
                terms.MaximumQuantity,
                price.TotalCredits,
                price.Curve,
                null,
                terms.LimitReasons,
                price.PriceReasons);
            RememberQuote(quote, context);
            return quote;
        }
    }

    private static bool HasSameBinding(TradeQuoteSnapshot quote, TradeQuoteRequest request) =>
        string.Equals(quote.ObjectId, request.ObjectId, StringComparison.Ordinal) &&
        string.Equals(quote.ModuleId, request.ModuleId, StringComparison.Ordinal) &&
        string.Equals(quote.CommandType, request.CommandType, StringComparison.Ordinal) &&
        string.Equals(quote.ItemTypeId, request.ItemTypeId, StringComparison.Ordinal) &&
        quote.RequestedQuantity == request.Quantity;

    private TradeQuoteSnapshot DisabledQuote(TradeQuoteRequest request, string reasonCode, long maximumQuantity)
    {
        var (stationObjectId, marketRevision) = KnownDockedMarket(request.ObjectId);
        return new TradeQuoteSnapshot(
            request.RequestId, "", marketRevision ?? 0, stationObjectId ?? "", request.ObjectId, request.ModuleId,
            request.CommandType, request.ItemTypeId, request.Quantity, 0, maximumQuantity, 0,
            ImmutableArray<TradePriceStep>.Empty, reasonCode, ImmutableArray<string>.Empty);
    }

    /// <summary>
    /// Maximum is the longest prefix of the sequential curve every current authoritative limit allows,
    /// independent of the requested quantity. Buy/Refuel are all-or-nothing: a request above the maximum is
    /// disabled with its first limiter (player money, station stock, cargo/tank room;
    /// station Credits headroom as value_overflow). Sell fills the largest prefix the station budget and its
    /// free storage allow and names what limited it; missing cargo, or a payout the player's balance cannot
    /// hold, disables the whole quote.
    /// </summary>
    private QuoteTerms ComputeQuoteTerms(in TradeTarget target, string commandType)
    {
        long basePrice = target.ItemType.BasePriceCredits ?? 0;
        if (basePrice < 1)
            return Disabled(0, CommandReasonCodes.UnknownItemType);

        var station = target.Station;
        long stock = station.Inventory[target.StationInventoryIndex].StockQuantity;
        var factors = ResolveStationPriceFactorSources(station, target.ItemTypeIndex, target.ItemType);
        var factorValues = new int[factors.Length];
        var staticReasons = new TradePriceReason[factors.Length + 1];
        staticReasons[0] = new TradePriceReason(TradeQuoteCalculator.BasePriceReason, TradeQuoteCalculator.PermilleDenominator,
            target.ItemType.TypeId);
        for (int i = 0; i < factors.Length; i++)
        {
            factorValues[i] = factors[i].FactorPermille;
            staticReasons[i + 1] = new TradePriceReason(factors[i].Code, factors[i].FactorPermille, factors[i].SourceId);
        }

        var direction = commandType switch
        {
            TradeCommandTypes.Sell => TradeQuoteDirection.Sell,
            TradeCommandTypes.Refuel => TradeQuoteDirection.Refuel,
            _ => TradeQuoteDirection.Buy,
        };
        var input = new TradeQuotePriceInput(basePrice, factorValues, stock, ResolveTargetStock(station, target.ItemType),
            direction, 0, staticReasons);
        long qty = target.Quantity;

        if (direction == TradeQuoteDirection.Sell)
        {
            long cargoQty = CargoQuantityOf(target.Module, target.ItemTypeIndex);
            long room = ResolveMaxStock(station, target.ItemType) is { } maxStock
                ? Math.Max(0, maxStock - stock)
                : long.MaxValue - stock;
            long purse = Math.Max(0, SellPurse(station));
            long playerHeadroom = long.MaxValue - PlayerCredits;
            long maximum = TradeQuoteCalculator.MaxAffordablePrefix(
                input, Math.Min(cargoQty, room), Math.Min(purse, playerHeadroom));
            if (qty > cargoQty)
                return Disabled(maximum, CommandReasonCodes.InsufficientCargoQuantity);
            // The player's balance never shortens a Sell: a request it cannot hold is refused whole.
            if (TradeQuoteCalculator.MaxAffordablePrefix(input, qty, playerHeadroom) < qty)
                return Disabled(maximum, ValueOverflow);

            long budgetCap = TradeQuoteCalculator.MaxAffordablePrefix(input, qty, purse);
            long executable = Math.Min(qty, Math.Min(budgetCap, room));
            var limits = ImmutableArray.CreateBuilder<string>();
            if (budgetCap < qty && budgetCap == executable) limits.Add(CommandReasonCodes.StationBudgetExceeded);
            if (room < qty && room == executable) limits.Add(CommandReasonCodes.StationCapacityExceeded);
            if (executable <= 0)
                return Disabled(maximum, limits[0]);

            return new QuoteTerms(maximum, executable, TradeQuoteCalculator.Calculate(input with { Quantity = executable }),
                null, limits.ToImmutable());
        }

        long byRoom;
        string roomReason;
        if (direction == TradeQuoteDirection.Refuel)
        {
            byRoom = Math.Max(0, (target.ModuleType.FuelCapacityKg ?? 0) - target.Module.FuelAmountKg);
            roomReason = CommandReasonCodes.FuelCapacityExceeded;
        }
        else
        {
            long freeKg = FreeCargoKg(target);
            byRoom = target.ItemType.UnitMassKg > 0 ? freeKg / target.ItemType.UnitMassKg : long.MaxValue;
            roomReason = CommandReasonCodes.CargoCapacityExceeded;
        }

        // Every station receives the full payment in Credits, even when its trading budget is capped.
        long stationHeadroom = long.MaxValue - Math.Max(0, station.Credits);
        long creditLimit = Math.Min(Math.Max(0, PlayerCredits), stationHeadroom);
        long buyMaximum = TradeQuoteCalculator.MaxAffordablePrefix(input, Math.Min(stock, byRoom), creditLimit);
        if (qty > buyMaximum)
        {
            long reachable = Math.Min(qty, stock);
            string reason =
                TradeQuoteCalculator.MaxAffordablePrefix(input, reachable, Math.Max(0, PlayerCredits)) < reachable
                    ? CommandReasonCodes.InsufficientPlayerCredits
                    : qty > stock ? CommandReasonCodes.InsufficientStationStock
                    : qty > byRoom ? roomReason
                    : ValueOverflow;
            return Disabled(buyMaximum, reason);
        }

        return new QuoteTerms(buyMaximum, qty, TradeQuoteCalculator.Calculate(input with { Quantity = qty }),
            null, ImmutableArray<string>.Empty);

        static QuoteTerms Disabled(long maximum, string reason) =>
            new(maximum, 0, null, reason, ImmutableArray<string>.Empty);
    }

    /// <summary>
    /// Positive stock target of a bounded market's cargo row; null (neutral stock factor, constant curve) for
    /// Fuel, rows without a target and every station without a market profile (D-U2).
    /// </summary>
    private long? ResolveTargetStock(SpaceObjectRuntime station, ItemTypeDefinition itemType) =>
        itemType.StorageKind == ItemStorageKind.Cargo && TryGetMarket(station, out var profile, out var economy) &&
        TryMarketLimits(profile, economy, station.StationSize, itemType.TypeId, out var limits) && limits.Target > 0
            ? limits.Target
            : null;

    private QuoteContext CaptureQuoteContext(in TradeTarget target)
    {
        var factors = ResolveStationPriceFactorSources(target.Station, target.ItemTypeIndex, target.ItemType);
        var fingerprint = new StringBuilder();
        foreach (var factor in factors)
            fingerprint.Append(factor.Code).Append(':').Append(factor.SourceId).Append(':').Append(factor.FactorPermille).Append(';');

        return new QuoteContext(
            target.Station.InitialMotion.ObjectId,
            target.Station.MarketRevision,
            PlayerCredits,
            target.Station.Credits,
            target.Station.MarketBudgetCredits,
            target.Station.Inventory[target.StationInventoryIndex].StockQuantity,
            ResolveTargetStock(target.Station, target.ItemType),
            ResolveMaxStock(target.Station, target.ItemType),
            fingerprint.ToString(),
            target.Ship.IsDocked,
            target.Ship.DockedStationObjectId,
            CanExecuteModuleCommand(target.Module),
            CargoQuantityOf(target.Module, target.ItemTypeIndex),
            FreeCargoKg(target),
            target.Module.FuelAmountKg,
            target.ModuleType.FuelCapacityKg ?? 0);
    }

    private void RememberQuote(TradeQuoteSnapshot quote, QuoteContext context)
    {
        _issuedQuotes[quote.QuoteId] = new IssuedQuote(quote, context);
        string? requestId = string.IsNullOrEmpty(quote.RequestId) ? null : quote.RequestId;
        if (requestId is not null)
            _quoteIdByRequestId[requestId] = quote.QuoteId;
        _issuedQuoteOrder.Enqueue((quote.QuoteId, requestId));
        // FIFO by issue order: only the most recent issues stay executable. Ids already consumed or
        // invalidated are simply absent from the dictionary, so dequeuing them is a no-op.
        while (_issuedQuoteOrder.Count > QuoteCacheLimit)
        {
            var (evictedQuoteId, evictedRequestId) = _issuedQuoteOrder.Dequeue();
            ForgetQuote(evictedQuoteId, evictedRequestId);
        }
    }

    /// <summary>Drop one quote and, if it still owns it, its RequestId binding.</summary>
    private void ForgetQuote(string quoteId, string? requestId)
    {
        _issuedQuotes.Remove(quoteId);
        if (requestId is not null && _quoteIdByRequestId.TryGetValue(requestId, out var owner) &&
            string.Equals(owner, quoteId, StringComparison.Ordinal))
            _quoteIdByRequestId.Remove(requestId);
    }

    /// <summary>Forget every issued quote and start a new id space. Called by the constructor and every load.</summary>
    private void ResetQuoteSession()
    {
        _issuedQuotes.Clear();
        _quoteIdByRequestId.Clear();
        _issuedQuoteOrder.Clear();
        Span<byte> nonce = stackalloc byte[8];
        RandomNumberGenerator.Fill(nonce);
        _quoteNonce = Convert.ToHexString(nonce);
        _quoteCounter = 0;
    }

    /// <summary>A committed market change outdates every quote issued for that station (EP-0001-US-0015-TK-0003).</summary>
    partial void OnMarketRevisionCommitted(string stationObjectId)
    {
        List<IssuedQuote>? stale = null;
        foreach (var issued in _issuedQuotes.Values)
        {
            if (string.Equals(issued.Quote.StationObjectId, stationObjectId, StringComparison.Ordinal))
                (stale ??= []).Add(issued);
        }

        if (stale is null) return;
        foreach (var issued in stale)
            ForgetQuote(issued.Quote.QuoteId, string.IsNullOrEmpty(issued.Quote.RequestId) ? null : issued.Quote.RequestId);
    }

    /// <summary>
    /// Check an incoming quote binding against the issued quote and the current world, without mutating or
    /// consuming anything. A malformed or foreign binding is <c>invalid_quote</c>; an unknown, consumed,
    /// evicted or outdated one is <c>stale_quote</c>.
    /// </summary>
    private bool TryValidateTradeQuote(
        PlayerCommand command, [NotNullWhen(true)] out TradeQuoteSnapshot? quote, out string reasonCode)
    {
        quote = null;
        if (string.IsNullOrWhiteSpace(command.QuoteId) || command.MarketRevision is not { } revision || revision < 1)
        {
            reasonCode = CommandReasonCodes.InvalidQuote;
            return false;
        }

        if (!_issuedQuotes.TryGetValue(command.QuoteId, out var issued))
        {
            reasonCode = CommandReasonCodes.StaleQuote;
            return false;
        }

        if (!TryResolveTradeTarget(command.ObjectId, command.ModuleId, command.CommandType, command.ItemTypeId,
                command.Quantity, out var target, out reasonCode))
            return false;

        var bound = issued.Quote;
        if (!string.Equals(bound.StationObjectId, target.Station.InitialMotion.ObjectId, StringComparison.Ordinal) ||
            !string.Equals(bound.ObjectId, command.ObjectId, StringComparison.Ordinal) ||
            !string.Equals(bound.ModuleId, command.ModuleId, StringComparison.Ordinal) ||
            !string.Equals(bound.CommandType, command.CommandType, StringComparison.Ordinal) ||
            !string.Equals(bound.ItemTypeId, command.ItemTypeId, StringComparison.Ordinal) ||
            bound.RequestedQuantity != target.Quantity ||
            bound.MarketRevision != revision)
        {
            reasonCode = CommandReasonCodes.InvalidQuote;
            return false;
        }

        if (CaptureQuoteContext(target) != issued.Context)
        {
            reasonCode = CommandReasonCodes.StaleQuote;
            return false;
        }

        quote = bound;
        reasonCode = "";
        return true;
    }
}
