using System.Collections.Immutable;
using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Engine.Scenario;

namespace DeepSpaceSaga.Engine;

public sealed partial class SimulationEngine
{
    // Only identities outlive the bounded 50-record finance history, never an unbounded audit log.
    private HashSet<string> _durableVoyageTerminalIds = new(StringComparer.Ordinal);
    private Dictionary<string, VoyageFuelSettlementSnapshot> _voyageFuelSettlements = new(StringComparer.Ordinal);
    private void RememberVoyageTerminal(string id)
    {
        _durableVoyageTerminalIds.Add(id);
        lock (_commandGate) _knownCommands.Add(id);
    }
    private sealed record StagedVoyageContinuation(ImmutableArray<VoyageLedgerEntry> Ledgers,
        HashSet<string> TerminalIds, Dictionary<string, VoyageFuelSettlementSnapshot> Settlements);
    private static ScenarioException InvalidVoyageContinuation(string detail) =>
        new($"Voyage continuation: {detail}. Save was not modified.");

    private StagedVoyageContinuation StageVoyageContinuation(ScenarioFile source,
        IReadOnlyList<SpaceObjectRuntime> objects, VoyageStateData? voyage)
    {
        var state = source.GameState;
        bool current = source.SaveFormatVersion >= TradingEconomySaveMigration.ManifestSaveVersion;
        var ledgers = state.VoyageLedgers;
        if (current && ledgers is null) throw InvalidVoyageContinuation("missing voyageLedgers");
        if (current && state.VoyageFuelSettlements is null) throw InvalidVoyageContinuation("missing voyageFuelSettlements");
        if (!current && ledgers is { Count: > 0 }) throw InvalidVoyageContinuation("ledger under a legacy schema");
        var terminal = new HashSet<string>(current ? state.TradingEconomyContinuation!.DurableTerminalReceiptIds ?? [] : [], StringComparer.Ordinal);
        var settlements = new Dictionary<string, VoyageFuelSettlementSnapshot>(StringComparer.Ordinal);
        if ((state.VoyageFuelSettlements?.Count ?? 0) > 51) throw InvalidVoyageContinuation("settlement history exceeds finance retention");
        foreach (var receipt in state.VoyageFuelSettlements ?? [])
        {
            if (receipt is null) throw InvalidVoyageContinuation("null fuel settlement");
            ValidateVoyageFuelSave(state with { LastVoyageFuelSettlement = receipt }, voyage, objects);
            if (!terminal.Contains(receipt.VoyageId) || !settlements.TryAdd(receipt.VoyageId, receipt))
                throw InvalidVoyageContinuation("invalid or duplicate durable fuel settlement");
        }
        var restored = ImmutableArray.CreateBuilder<VoyageLedgerEntry>();
        var stationIds = objects.Where(o => o.ObjectType == SpaceObjectType.Station).Select(o => o.InitialMotion.ObjectId).ToHashSet(StringComparer.Ordinal);
        bool active = voyage is { Phase: not VoyagePhases.Docked };
        if ((ledgers?.Count ?? 0) > 50) throw InvalidVoyageContinuation("finance history exceeds retention50");
        var seen = new HashSet<string>(StringComparer.Ordinal);
        long previousStarted = -1;
        foreach (var saved in ledgers ?? [])
        {
            if (saved is null) throw InvalidVoyageContinuation("null voyage ledger");
            var f = saved.Finance;
            if (f is null || string.IsNullOrWhiteSpace(f.VoyageId) || !seen.Add(f.VoyageId) ||
                !stationIds.Contains(f.OriginStationObjectId) || f.DestinationStationObjectId is null ||
                !stationIds.Contains(f.DestinationStationObjectId) || f.OriginStationObjectId == f.DestinationStationObjectId ||
                f.StartedGameTimeMs < 0 || f.StartedGameTimeMs < previousStarted || f.StartedGameTimeMs > state.GameTimeMs ||
                f.State is not (VoyageFinanceStates.InTransit or VoyageFinanceStates.AwaitingRealization or VoyageFinanceStates.Finalized or VoyageFinanceStates.Interrupted))
                throw InvalidVoyageContinuation("invalid ledger identity, stations, order or state");
            previousStarted = f.StartedGameTimeMs;
            bool transit = f.State == VoyageFinanceStates.InTransit;
            if (transit != (active && voyage!.VoyageId == f.VoyageId) ||
                transit && (terminal.Contains(f.VoyageId) || f.CompletedGameTimeMs is not null || settlements.ContainsKey(f.VoyageId)) ||
                !transit && (!terminal.Contains(f.VoyageId) || f.CompletedGameTimeMs is not { } end || end < f.StartedGameTimeMs || end > state.GameTimeMs) ||
                transit && (f.OriginStationObjectId != voyage!.OriginStationObjectId || f.DestinationStationObjectId != voyage.DestinationStationObjectId ||
                    voyage.StartedGameTimeMs is not null && f.StartedGameTimeMs != voyage.StartedGameTimeMs))
                throw InvalidVoyageContinuation("active/terminal ledger disagrees with voyage");
            if (f.UnsoldCargo.IsDefault || f.UnsoldCargo.Any(c => c is null || string.IsNullOrWhiteSpace(c.ItemTypeId) || c.Quantity <= 0 || c.CostBasisCredits is < 0 ||
                !_registry.ItemTypes.Contains(c.ItemTypeId)) ||
                !f.UnsoldCargo.Select(c => c.ItemTypeId).SequenceEqual(f.UnsoldCargo.Select(c => c.ItemTypeId).Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal)))
                throw InvalidVoyageContinuation("invalid carried cargo");
            if (transit || f.State == VoyageFinanceStates.AwaitingRealization)
            {
                var ship = objects.FirstOrDefault(o => o.InitialMotion.ObjectId == state.PlayerShipObjectId);
                if (ship is null || f.State == VoyageFinanceStates.AwaitingRealization &&
                    (!ship.IsDocked || ship.DockedStationObjectId != f.DestinationStationObjectId))
                    throw InvalidVoyageContinuation("unrealized ledger destination disagrees with docked ship");
                foreach (var carried in f.UnsoldCargo)
                {
                    int item = _registry.ItemTypes.GetIndex(carried.ItemTypeId);
                    Int128 actual = 0;
                    foreach (var stack in ship.Modules.SelectMany(m => m.Cargo).Where(c => c.ItemTypeIndex == item)) actual += stack.Quantity;
                    if (carried.Quantity > actual) throw InvalidVoyageContinuation("unrealized carried quantity exceeds ship cargo");
                }
            }
            var postings = saved.Postings ?? throw InvalidVoyageContinuation("missing posting entries");
            if (postings.Any(p => p is null)) throw InvalidVoyageContinuation("null monetary posting");
            var ids = postings.Select(p => p.PostingId).ToArray();
            if (ids.Any(string.IsNullOrWhiteSpace) || !ids.SequenceEqual(ids.Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal)))
                throw InvalidVoyageContinuation("duplicate or unordered ledger posting IDs");
            try
            {
                long sales = 0, cost = 0, fuel = 0, assessed = 0, paid = 0, debt = 0, events = 0, payout = 0, penalty = 0;
                bool unknown = false;
                foreach (var p in postings)
                {
                    if (p.GrossSalesCredits < 0 || p.CostOfGoodsSoldCredits < 0 || p.RouteFuelCostCredits < 0 ||
                        p.PortFeesAssessedCredits < 0 || p.PortFeesPaidCredits < 0 || p.OutstandingPortFeeDebtCredits < 0 ||
                        p.EventCostsCredits < 0 || p.PassengerPayoutCredits < 0 || p.PassengerPenaltyCredits < 0 ||
                        (Int128)p.PortFeesPaidCredits + p.OutstandingPortFeeDebtCredits != p.PortFeesAssessedCredits)
                        throw InvalidVoyageContinuation("invalid monetary posting");
                    sales = checked(sales + p.GrossSalesCredits); cost = checked(cost + p.CostOfGoodsSoldCredits);
                    fuel = checked(fuel + p.RouteFuelCostCredits); assessed = checked(assessed + p.PortFeesAssessedCredits);
                    paid = checked(paid + p.PortFeesPaidCredits); debt = checked(debt + p.OutstandingPortFeeDebtCredits);
                    events = checked(events + p.EventCostsCredits); payout = checked(payout + p.PassengerPayoutCredits);
                    penalty = checked(penalty + p.PassengerPenaltyCredits); unknown |= p.UnknownCostOfGoodsSold;
                }
                if (f.GrossSalesCredits != sales || f.CostOfGoodsSoldCredits != (unknown ? null : cost) || f.HasUnknownCostOfGoodsSold != unknown ||
                    f.RouteFuelCostCredits != fuel || f.PortFeesAssessedCredits != assessed || f.PortFeesPaidCredits != paid ||
                    f.OutstandingPortFeeDebtCredits != debt || f.EventCostsCredits != events || f.PassengerPayoutCredits != payout ||
                    f.PassengerPenaltyCredits != penalty || f.NetProfitCredits != VoyageNet(f))
                    throw InvalidVoyageContinuation("ledger totals disagree with persisted postings");
            }
            catch (OverflowException error) { throw new ScenarioException("Voyage continuation overflow. Save was not modified.", error); }
            if (settlements.TryGetValue(f.VoyageId, out var receipt))
            {
                ValidateVoyageFuelSave(state with { LastVoyageFuelSettlement = receipt }, voyage, objects);
                if (receipt.VoyageId != f.VoyageId || receipt.RouteFuelCostCredits != f.RouteFuelCostCredits ||
                    !ids.Contains("fuel:" + f.VoyageId, StringComparer.Ordinal))
                    throw InvalidVoyageContinuation("fuel settlement disagrees with ledger");
            }
            else if (f.RouteFuelCostCredits != 0 || ids.Contains("fuel:" + f.VoyageId, StringComparer.Ordinal))
                throw InvalidVoyageContinuation("missing fuel settlement");
            restored.Add(new(f with { UnsoldCargo = f.UnsoldCargo.ToImmutableArray() }, ids.ToImmutableHashSet(StringComparer.Ordinal), postings.ToImmutableArray()));
        }
        if (current && active && (!seen.Contains(voyage!.VoyageId!) || terminal.Contains(voyage.VoyageId!)))
            throw InvalidVoyageContinuation("active voyage requires exactly one open ledger");
        if (restored.Count(v => v.Finance.State is VoyageFinanceStates.InTransit or VoyageFinanceStates.AwaitingRealization) > 1)
            throw InvalidVoyageContinuation("multiple unfinished ledgers");
        if (current)
        {
            var latestFuelLedger = restored.LastOrDefault(l => settlements.ContainsKey(l.Finance.VoyageId));
            if (settlements.Count > 0 && state.LastVoyageFuelSettlement is null || latestFuelLedger is not null &&
                state.LastVoyageFuelSettlement?.VoyageId != latestFuelLedger.Finance.VoyageId ||
                settlements.Keys.Count(id => !seen.Contains(id)) > 1)
                throw InvalidVoyageContinuation("last fuel receipt disagrees with retained terminal chronology");
        }
        if (state.LastVoyageFuelSettlement is { } last)
        {
            if (current && (!terminal.Contains(last.VoyageId) || !settlements.TryGetValue(last.VoyageId, out var matching) || matching != last))
                throw InvalidVoyageContinuation("last settlement is not its persisted terminal receipt");
            terminal.Add(last.VoyageId); settlements[last.VoyageId] = last;
        }
        // Legacy history is unknowable. Keep one explicitly unknown active record without inventing sales/cost.
        if (!current && active)
        {
            var v = voyage!;
            var f = new VoyageFinanceSnapshot(v.VoyageId!, v.OriginStationObjectId!, v.DestinationStationObjectId,
                v.StartedGameTimeMs ?? state.GameTimeMs, null, VoyageFinanceStates.InTransit,
                0, null, true, 0, 0, 0, 0, 0, 0, 0, null, []);
            var posting = new VoyageLedgerPostingData("legacy-history:" + v.VoyageId, UnknownCostOfGoodsSold: true);
            restored.Add(new(f, ImmutableHashSet.Create(StringComparer.Ordinal, posting.PostingId), [posting]));
        }
        return new(restored.ToImmutable(), terminal, settlements);
    }

    private VoyageLedgerData[] CaptureVoyageLedgers() => _voyageLedgers.Select(l => new VoyageLedgerData(l.Finance,
        (l.Postings.IsDefault ? [] : l.Postings).OrderBy(p => p.PostingId, StringComparer.Ordinal).ToArray())).ToArray();

    private void CommitVoyageContinuation(StagedVoyageContinuation staged)
    {
        _voyageLedgers = staged.Ledgers;
        _durableVoyageTerminalIds = staged.TerminalIds;
        _voyageFuelSettlements = staged.Settlements;
    }

    internal VoyageFuelSettlementSnapshot? ReplayVoyageTerminalForTests(VoyageStateData voyage, bool arrived)
    {
        lock (_worldStateLock) return SettleVoyageFuel(voyage, arrived);
    }
}
