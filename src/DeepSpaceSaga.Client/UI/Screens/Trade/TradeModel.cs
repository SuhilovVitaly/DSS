using DeepSpaceSaga.Contracts;

namespace DeepSpaceSaga.Client.UI.Screens.Trade;

internal enum TradeMode { Buy, Sell, Refuel }
internal enum TradeFilter { All, Resources, Goods }
internal enum TradeSort { Name, Price, StationStock, Cargo }

/// <summary>
/// Display projection of the authoritative server quote. <see cref="Maximum"/>, <see cref="Total"/> and
/// <see cref="ExecutableQuantity"/> always come from the server <see cref="TradeQuoteSnapshot"/>; the client never
/// multiplies a unit price or derives its own maximum. <see cref="DisabledReason"/> and <see cref="LimitReason"/>
/// are <c>TradeUX.*</c> locale keys (<see cref="LimitReason"/> is empty when the server names no limiter).
/// </summary>
internal readonly record struct TradeQuote(long Maximum, long Total, long CargoQuantity,
    long AmountBefore, long AmountAfter, long BalanceAfter, string? DisabledReason, string LimitReason,
    long ExecutableQuantity = 0)
{
    internal const string RequestIdConflict = "request_id_conflict";
    private const string ValueOverflowCode = "value_overflow";

    internal static string CommandType(TradeMode mode) => mode switch
    {
        TradeMode.Sell => TradeCommandTypes.Sell,
        TradeMode.Refuel => TradeCommandTypes.Refuel,
        _ => TradeCommandTypes.Buy
    };

    /// <summary>
    /// Locale key (<c>TradeUX.*</c>) for an Engine quote/limit reason code, or null when the code has no
    /// dedicated key. Existing trade result codes keep their earlier keys.
    /// </summary>
    internal static string? ReasonKey(string? code) => code switch
    {
        CommandReasonCodes.StationBudgetExceeded => "StationBudgetLimit",
        CommandReasonCodes.StationCapacityExceeded => "StationCapacityLimit",
        CommandReasonCodes.InsufficientPlayerCredits => "MoneyLimit",
        CommandReasonCodes.InsufficientStationStock => "StockLimit",
        CommandReasonCodes.CargoCapacityExceeded => "CapacityLimit",
        CommandReasonCodes.FuelCapacityExceeded => "TankLimit",
        CommandReasonCodes.InsufficientCargoQuantity => "CargoLimit",
        ValueOverflowCode => "ValueOverflow",
        CommandReasonCodes.StaleQuote => "QuoteStale",
        CommandReasonCodes.InvalidQuote => "InvalidQuote",
        CommandReasonCodes.QuoteRequired => "QuoteRequired",
        CommandReasonCodes.FuelTradeForbidden => "FuelServiceOnly",
        CommandReasonCodes.ModuleUnavailable => "ModuleUnavailable",
        CommandReasonCodes.NotDocked => "NotDocked",
        CommandReasonCodes.InvalidQuantity => "EnterQuantity",
        _ => null
    };

    /// <summary>Locale key for a disabled quote or limiter; <c>request_id_conflict</c> and unknown codes are QuoteUnavailable.</summary>
    internal static string QuoteReasonKey(string? code) => ReasonKey(code) ?? "QuoteUnavailable";

    /// <summary>
    /// Project <paramref name="quote"/> onto the current selection. A quote bound to another item, module, command,
    /// quantity or station is not shown: the preview stays <paramref name="pendingReason"/> (QuoteLoading by default)
    /// with <paramref name="knownMaximum"/> — the last valid server maximum for this selection — for Max/slider/presets.
    /// </summary>
    internal static TradeQuote Calculate(StationInventoryItemSnapshot? item, InstalledModuleSnapshot? module,
        TradeMode mode, long quantity, long credits, TradeQuoteSnapshot? quote, long knownMaximum = 0,
        string? pendingReason = null, string? stationId = null)
    {
        TradeQuote Disabled(string reason) => new(0, 0, 0, 0, 0, credits, reason, reason);
        if (item is null) return Disabled("SelectItem");
        if (module is null) return Disabled(mode == TradeMode.Refuel ? "NoTank" : "NoContainer");
        if (module.PowerState != "On" || module.OperationalState != "Ready" || module.StructurePoints <= 0)
            return Disabled("ModuleUnavailable");
        if (item.UnitMassKg < 0 || credits < 0) return Disabled("InvalidData");
        // A malformed authoritative capacity must never be traded against.
        if (mode == TradeMode.Sell && item.FreeStockCapacity is < 0) return Disabled("InvalidData");
        long cargo = module.Cargo.IsDefaultOrEmpty ? 0 : module.Cargo.FirstOrDefault(c => c.ItemTypeId == item.ItemTypeId)?.Quantity ?? 0;
        long available = Math.Max(0, module.AvailableCapacityKg ?? 0);
        long before = mode == TradeMode.Refuel ? module.FuelAmountKg ?? 0 : available;
        TradeQuote Blocked(long maximum, string reason, string limit) => new(Math.Max(0, maximum), 0, cargo, before, before, credits, reason, limit);

        if (quantity <= 0) return Blocked(knownMaximum, "EnterQuantity", "");
        if (quote is null || !IsBoundTo(quote, item, module, mode, quantity, stationId))
            return Blocked(knownMaximum, pendingReason ?? "QuoteLoading", "");
        if (quote.DisabledReason is { } disabled)
        {
            string key = QuoteReasonKey(disabled);
            return Blocked(disabled == RequestIdConflict ? knownMaximum : quote.MaximumQuantity, key, key);
        }

        long executable = quote.ExecutableQuantity;
        string limitKey = quote.LimitReasons.IsDefaultOrEmpty ? "" : QuoteReasonKey(quote.LimitReasons[0]);
        if (string.IsNullOrEmpty(quote.QuoteId) || quote.MarketRevision < 1 || executable < 0 || executable > quantity ||
            quote.MaximumQuantity < executable || quote.TotalCredits < 0)
            return Blocked(quote.MaximumQuantity, "InvalidData", limitKey);
        if (executable == 0) return Blocked(quote.MaximumQuantity, limitKey.Length > 0 ? limitKey : "QuoteUnavailable", limitKey);
        // Only Sell may fill partially; Buy/Refuel are all-or-nothing.
        if (mode != TradeMode.Sell && executable != quantity) return Blocked(quote.MaximumQuantity, "InvalidQuote", limitKey);
        try
        {
            if (!CurveMatches(quote)) return Blocked(quote.MaximumQuantity, "InvalidData", limitKey);
            long total = quote.TotalCredits;
            long delta = checked(executable * (mode == TradeMode.Refuel ? 1 : item.UnitMassKg));
            long after = mode == TradeMode.Buy ? checked(before - delta) : checked(before + delta);
            long balance = mode == TradeMode.Sell ? checked(credits + total) : checked(credits - total);
            return new(quote.MaximumQuantity, total, cargo, before, after, balance, null, limitKey, executable);
        }
        catch (OverflowException) { return Blocked(quote.MaximumQuantity, "BalanceLimit", limitKey); }
    }

    private static bool IsBoundTo(TradeQuoteSnapshot quote, StationInventoryItemSnapshot item, InstalledModuleSnapshot module,
        TradeMode mode, long quantity, string? stationId) =>
        quote.ItemTypeId == item.ItemTypeId && quote.ModuleId == module.ModuleId && quote.CommandType == CommandType(mode) &&
        quote.RequestedQuantity == quantity &&
        (string.IsNullOrEmpty(stationId) || string.IsNullOrEmpty(quote.StationObjectId) || quote.StationObjectId == stationId);

    /// <summary>The curve must add up to exactly the executable quantity and total (checked; never re-priced).</summary>
    private static bool CurveMatches(TradeQuoteSnapshot quote)
    {
        long units = 0, credits = 0;
        if (!quote.Curve.IsDefault)
            foreach (var step in quote.Curve)
            {
                if (step.Quantity <= 0 || step.UnitPriceCredits < 0) return false;
                units = checked(units + step.Quantity);
                credits = checked(credits + checked(step.Quantity * step.UnitPriceCredits));
            }
        return units == quote.ExecutableQuantity && credits == quote.TotalCredits;
    }
}

/// <summary>Per-session bounded receipts survive closing and reopening the trade window.</summary>
internal sealed class TradeJournal
{
    /// <summary>
    /// One sent trade. <see cref="QuoteId"/>/<see cref="MarketRevision"/>/<see cref="QuotedTotalCredits"/> record the
    /// server quote that was shown and sent (null for unquoted legacy sends).
    /// </summary>
    internal sealed record Entry(string CommandId, string ItemId, string ModuleId, TradeMode Mode,
        long RequestedQuantity, long UnitPrice, CommandResult? Result = null, string ModuleLabel = "",
        string? QuoteId = null, long? MarketRevision = null, long? QuotedTotalCredits = null);
    private readonly List<Entry> _entries = new();
    internal IReadOnlyList<Entry> Entries => _entries;
    internal Entry? Latest => _entries.LastOrDefault();
    internal bool IsPending => _entries.Any(e => e.Result is null);
    internal void Track(Entry entry)
    {
        _entries.Add(entry);
        if (_entries.Count > 50) _entries.RemoveAt(0);
    }
    /// <summary>Attach final results; returns true when a pending trade has just been refused as <c>stale_quote</c>.</summary>
    internal bool Refresh(SnapshotBuffer? buffer)
    {
        if (buffer is null) return false;
        bool stale = false;
        for (int i = 0; i < _entries.Count; i++)
        {
            var entry = _entries[i];
            if (entry.Result is not null) continue;
            if (buffer.FindCommandResult(entry.CommandId) is { Status: not CommandResultStatus.Deferred } result)
            {
                _entries[i] = entry with { Result = result };
                stale |= result.ReasonCode == CommandReasonCodes.StaleQuote;
            }
        }
        return stale;
    }
}

/// <summary>Selection and cached market projection. No rendering or engine dependencies.</summary>
internal sealed class TradeModel
{
    internal const string FuelId = "item.fuel";
    private AuthoritativeSnapshot? _snapshot;
    private bool _dirty = true;
    private string _query = "";
    internal string Query { get => _query; set { _query = value; _dirty = true; } }
    internal TradeFilter Filter { get; private set; }
    internal TradeSort Sort { get; private set; }
    internal bool Descending { get; private set; }
    internal bool CargoOnly { get; private set; }
    internal TradeMode Mode { get; private set; }
    internal bool FuelMode => Mode == TradeMode.Refuel;
    internal string? SelectedItemId { get; private set; }
    internal string? SelectedModuleId { get; private set; }
    internal long Quantity { get; set; } = 1;
    internal StationInventoryItemSnapshot[] Rows { get; private set; } = [];
    internal InstalledModuleSnapshot[] Modules { get; private set; } = [];
    internal InstalledModuleSnapshot? Module => Modules.FirstOrDefault(m => m.ModuleId == SelectedModuleId);
    internal StationInventoryItemSnapshot? Item => Rows.FirstOrDefault(i => i.ItemTypeId == SelectedItemId);
    internal string CommandType => TradeQuote.CommandType(Mode);
    internal string? StationId => _snapshot?.DockedStationTrade?.StationObjectId;
    internal TradeQuote Quote => TradeQuote.Calculate(Item, Module, Mode, Quantity, _snapshot?.PlayerCredits ?? 0,
        AuthoritativeQuote, KnownMaximum, _quoteReason, StationId);

    /// <summary>Latest server quote accepted for the current selection key (see TradeScreen); null while none is valid.</summary>
    internal TradeQuoteSnapshot? AuthoritativeQuote { get; private set; }
    private string? _quoteReason;
    private (string Station, string Item, string Module, string Command, long Maximum)? _lastMaximum;

    /// <summary>Last valid server MaximumQuantity for this station/item/module/command; 0 when none is known.</summary>
    internal long KnownMaximum => _lastMaximum is { } known && known.Station == StationId && known.Item == SelectedItemId &&
        known.Module == SelectedModuleId && known.Command == CommandType ? known.Maximum : 0;

    /// <summary>Show <paramref name="quote"/>; its maximum becomes the known maximum for its selection.</summary>
    internal void ApplyQuote(TradeQuoteSnapshot quote)
    {
        AuthoritativeQuote = quote;
        _quoteReason = null;
        if (quote.DisabledReason is not (TradeQuote.RequestIdConflict or CommandReasonCodes.InvalidQuantity) &&
            quote.MaximumQuantity >= 0 && !string.IsNullOrEmpty(quote.StationObjectId))
            _lastMaximum = (quote.StationObjectId, quote.ItemTypeId, quote.ModuleId, quote.CommandType, quote.MaximumQuantity);
    }

    /// <summary>Drop the shown quote; the preview stays disabled with <paramref name="reasonKey"/> until a new quote.</summary>
    internal void InvalidateQuote(string reasonKey)
    {
        AuthoritativeQuote = null;
        _quoteReason = reasonKey;
    }

    internal void Refresh(AuthoritativeSnapshot? snapshot)
    {
        if (!ReferenceEquals(snapshot, _snapshot)) { _snapshot = snapshot; _dirty = true; }
        if (!_dirty) return;
        _dirty = false;
        string command = CommandType;
        Modules = snapshot is null || snapshot.InstalledModules.IsDefaultOrEmpty ? [] : snapshot.InstalledModules
            .Where(m => !m.CommandTypeIds.IsDefaultOrEmpty && m.CommandTypeIds.Contains(command)).OrderBy(m => m.Position).ToArray();
        if (!Modules.Any(m => m.ModuleId == SelectedModuleId)) SelectedModuleId = Modules.FirstOrDefault()?.ModuleId;
        var items = snapshot?.DockedStationTrade?.Items;
        IEnumerable<StationInventoryItemSnapshot> rows = items is null || items.Value.IsDefaultOrEmpty ? [] : items.Value;
        rows = rows.Where(i => (i.ItemTypeId == FuelId) == FuelMode);
        if (!FuelMode)
        {
            rows = rows.Where(i => Filter == TradeFilter.All || i.Category == (Filter == TradeFilter.Resources ? TradeItemCategories.Resource : TradeItemCategories.Good));
            if (CargoOnly) rows = rows.Where(i => Cargo(i.ItemTypeId) > 0);
            if (!string.IsNullOrWhiteSpace(Query)) rows = rows.Where(i => TradeItemPresentation.ItemDisplayName(i.ItemTypeId).Contains(Query, StringComparison.CurrentCultureIgnoreCase)
                || i.ItemTypeId.Contains(Query, StringComparison.OrdinalIgnoreCase));
        }
        Rows = rows.ToArray();
        Array.Sort(Rows, (a, b) =>
        {
            int comparison = Sort switch
            {
                TradeSort.Price => a.UnitPriceCredits.CompareTo(b.UnitPriceCredits),
                TradeSort.StationStock => a.StockQuantity.CompareTo(b.StockQuantity),
                TradeSort.Cargo => Cargo(a.ItemTypeId).CompareTo(Cargo(b.ItemTypeId)),
                _ => StringComparer.CurrentCultureIgnoreCase.Compare(TradeItemPresentation.ItemDisplayName(a.ItemTypeId), TradeItemPresentation.ItemDisplayName(b.ItemTypeId))
            };
            if (comparison == 0) comparison = StringComparer.Ordinal.Compare(a.ItemTypeId, b.ItemTypeId);
            return Descending ? -comparison : comparison;
        });
        if (FuelMode) SelectedItemId = Rows.FirstOrDefault()?.ItemTypeId;
        else if (SelectedItemId is not null && !Rows.Any(i => i.ItemTypeId == SelectedItemId)) SelectedItemId = null;
    }
    internal long Cargo(string itemId) => Module?.Cargo.IsDefaultOrEmpty == false
        ? Module.Cargo.FirstOrDefault(c => c.ItemTypeId == itemId)?.Quantity ?? 0 : 0;
    internal void Select(string itemId) { SelectedItemId = itemId; Quantity = 1; }
    internal void SelectModule(string id) { SelectedModuleId = id; _dirty = true; Refresh(_snapshot); }
    internal void SetMode(TradeMode mode)
    {
        bool changedTab = (mode == TradeMode.Refuel) != FuelMode;
        Mode = mode; Quantity = 1;
        if (changedTab) { SelectedItemId = null; SelectedModuleId = null; }
        _dirty = true; Refresh(_snapshot);
    }
    internal void SetFilter(TradeFilter filter) { Filter = filter; _dirty = true; Refresh(_snapshot); }
    internal void ToggleCargoOnly() { CargoOnly = !CargoOnly; _dirty = true; Refresh(_snapshot); }
    internal void SetSort(TradeSort sort) { Descending = Sort == sort && !Descending; Sort = sort; _dirty = true; Refresh(_snapshot); }
    internal void FillTank(int percent)
    {
        if (!FuelMode || Module is not { FuelCapacityKg: { } capacity } module) return;
        long target = (long)((decimal)capacity * percent / 100);
        Quantity = Math.Min(Quote.Maximum, Math.Max(0, target - (module.FuelAmountKg ?? 0)));
    }
}
