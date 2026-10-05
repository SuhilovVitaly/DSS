using System.Collections.Immutable;
using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Engine.Scenario;

namespace DeepSpaceSaga.Engine;

public sealed partial class SimulationEngine
{
    internal sealed record VoyageLedgerEntry(VoyageFinanceSnapshot Finance, ImmutableHashSet<string> PostingIds);
    private ImmutableArray<VoyageLedgerEntry> _voyageLedgers = [];
    internal ImmutableArray<VoyageFinanceSnapshot> VoyageFinancesForTests => _voyageLedgers.Select(v => v.Finance).ToImmutableArray();

    private static long? VoyageNet(VoyageFinanceSnapshot f) => f.HasUnknownCostOfGoodsSold ? null : checked(
        f.GrossSalesCredits - f.CostOfGoodsSoldCredits!.Value - f.RouteFuelCostCredits - f.PortFeesAssessedCredits
        - f.EventCostsCredits + f.PassengerPayoutCredits - f.PassengerPenaltyCredits);

    private void BeginVoyageLedger(VoyageStateData voyage, long gameTimeMs)
    {
        if (_voyageLedgers.Any(v => v.Finance.VoyageId == voyage.VoyageId)) return;
        var ship = _objects.Single(o => o.InitialMotion.ObjectId == PlayerShipObjectId);
        var cargo = ImmutableArray.CreateBuilder<VoyageCargoRemainderSnapshot>();
        foreach (var group in ship.Modules.SelectMany(m => m.Cargo).Where(c => c.Quantity > 0)
            .GroupBy(c => c.ItemTypeIndex).OrderBy(g => _registry.ItemTypes.GetDefinition(g.Key).TypeId, StringComparer.Ordinal))
        {
            long quantity = 0, basis = 0; bool unknown = false;
            foreach (var stack in group)
            {
                quantity = checked(quantity + stack.Quantity);
                if (stack.CostBasisCredits is { } cost) basis = checked(basis + cost); else unknown = true;
            }
            cargo.Add(new(_registry.ItemTypes.GetDefinition(group.Key).TypeId, quantity, unknown ? null : basis));
        }
        var finance = new VoyageFinanceSnapshot(voyage.VoyageId!, voyage.OriginStationObjectId!,
            voyage.DestinationStationObjectId, gameTimeMs, null, VoyageFinanceStates.InTransit,
            0, 0, false, 0, 0, 0, 0, 0, 0, 0, 0, cargo.ToImmutable());
        var previous = _voyageLedgers.Select(v => v.Finance.State == VoyageFinanceStates.AwaitingRealization
            ? v with { Finance = v.Finance with { State = VoyageFinanceStates.Finalized } } : v).ToImmutableArray();
        _voyageLedgers = TrimVoyageLedgers(previous.Add(new(finance, ImmutableHashSet.Create<string>(StringComparer.Ordinal))));
    }

    private static ImmutableArray<VoyageLedgerEntry> TrimVoyageLedgers(ImmutableArray<VoyageLedgerEntry> entries)
    {
        while (entries.Length > 50)
        {
            int index = -1;
            for (int i = 0; i < entries.Length; i++)
                if (entries[i].Finance.State is VoyageFinanceStates.Finalized or VoyageFinanceStates.Interrupted) { index = i; break; }
            if (index < 0) throw new InvalidOperationException("Too many unfinished voyage ledgers.");
            entries = entries.RemoveAt(index);
        }
        return entries;
    }

    private ImmutableArray<VoyageLedgerEntry> PrepareVoyageTransport(string voyageId, string? destination,
        long time, bool interrupted, VoyageFuelSettlementSnapshot? settlement, ImmutableArray<VoyageLedgerEntry>? source = null)
    {
        var entries = source ?? _voyageLedgers;
        for (int i = 0; i < entries.Length; i++)
        {
            var entry = entries[i]; var f = entry.Finance;
            if (f.VoyageId != voyageId || f.State is VoyageFinanceStates.Finalized or VoyageFinanceStates.Interrupted) continue;
            var ids = entry.PostingIds;
            if (settlement is not null && settlement.VoyageId == voyageId && !ids.Contains("fuel:" + voyageId))
            {
                if (settlement.RouteFuelCostCredits < 0) throw new ArgumentOutOfRangeException(nameof(settlement));
                f = f with { RouteFuelCostCredits = checked(f.RouteFuelCostCredits + settlement.RouteFuelCostCredits) };
                ids = ids.Add("fuel:" + voyageId);
            }
            f = f with
            {
                DestinationStationObjectId = destination,
                CompletedGameTimeMs = f.CompletedGameTimeMs ?? time,
                State = interrupted ? VoyageFinanceStates.Interrupted : VoyageFinanceStates.AwaitingRealization
            };
            f = f with { NetProfitCredits = VoyageNet(f) };
            return entries.SetItem(i, new(f, ids));
        }
        return entries;
    }

    private ImmutableArray<VoyageLedgerEntry> PrepareVoyageTerminal(VoyageStateData voyage, bool arrived)
    {
        var fuel = ProjectVoyageFuel(voyage, arrived);
        var settlement = voyage.FuelReservationParts is null ? null : new VoyageFuelSettlementSnapshot(
            voyage.VoyageId!, fuel.Reserved, fuel.Consumed, fuel.Reserved - fuel.Consumed, fuel.Cost);
        return PrepareVoyageTransport(voyage.VoyageId!, voyage.DestinationStationObjectId, _processedWorldTimeMs, !arrived, settlement);
    }

    private static ImmutableArray<VoyageLedgerEntry> PrepareVoyagePortFee(ImmutableArray<VoyageLedgerEntry> entries,
        string? stationId, string postingId, long assessed, long paid, long debtAdded)
    {
        if (string.IsNullOrWhiteSpace(postingId) || assessed < 0 || paid < 0 || debtAdded < 0 || (Int128)paid + debtAdded != assessed)
            throw new ArgumentException("Invalid voyage fee posting.");
        if (assessed == 0) return entries;
        for (int i = entries.Length - 1; i >= 0; i--)
        {
            var entry = entries[i]; var f = entry.Finance;
            if (f.State != VoyageFinanceStates.AwaitingRealization || f.DestinationStationObjectId != stationId || entry.PostingIds.Contains(postingId)) continue;
            f = f with
            {
                PortFeesAssessedCredits = checked(f.PortFeesAssessedCredits + assessed),
                PortFeesPaidCredits = checked(f.PortFeesPaidCredits + paid),
                OutstandingPortFeeDebtCredits = checked(f.OutstandingPortFeeDebtCredits + debtAdded)
            };
            f = f with { NetProfitCredits = VoyageNet(f) };
            return entries.SetItem(i, new(f, entry.PostingIds.Add(postingId)));
        }
        return entries;
    }

    private ImmutableArray<VoyageLedgerEntry> PrepareVoyageDockingFinance(IReadOnlyList<SpaceObjectRuntime> candidate, long credits)
    {
        var before = _objects.FirstOrDefault(o => o.InitialMotion.ObjectId == PlayerShipObjectId);
        var after = candidate.FirstOrDefault(o => o.InitialMotion.ObjectId == PlayerShipObjectId);
        if (before is null || after is not { IsDocked: true } || before.IsDocked ||
            _voyageState is not { Phase: VoyagePhases.Docking } voyage || after.DockedStationObjectId != voyage.DestinationStationObjectId)
            return _voyageLedgers;
        // Stage both the transport transition and fee before committing the dialogue candidate.
        var entries = PrepareVoyageTransport(voyage.VoyageId!, after.DockedStationObjectId, _processedWorldTimeMs, false, null);
        long paid = checked(PlayerCredits - credits);
        long debtAdded = checked(after.PortFeeDebt - before.PortFeeDebt);
        return PrepareVoyagePortFee(entries, after.DockedStationObjectId,
            $"port:{voyage.VoyageId}:{after.DockedStationObjectId}:{after.FirstPortFeeGameTimeMs}", checked(paid + debtAdded), paid, debtAdded);
    }

    internal enum VoyageAmountKind { EventCost, PassengerPayout, PassengerPenalty }
    internal void RecordVoyageAmount(string postingId, VoyageAmountKind kind, long credits)
    {
        if (string.IsNullOrWhiteSpace(postingId) || credits < 0 || !Enum.IsDefined(kind)) throw new ArgumentException("Invalid voyage amount posting.");
        if (credits == 0 || _voyageLedgers.IsEmpty) return;
        int index = _voyageLedgers.Length - 1; var entry = _voyageLedgers[index]; var f = entry.Finance;
        if (f.State is not (VoyageFinanceStates.InTransit or VoyageFinanceStates.AwaitingRealization) || entry.PostingIds.Contains(postingId)) return;
        f = kind switch
        {
            VoyageAmountKind.EventCost => f with { EventCostsCredits = checked(f.EventCostsCredits + credits) },
            VoyageAmountKind.PassengerPayout => f with { PassengerPayoutCredits = checked(f.PassengerPayoutCredits + credits) },
            _ => f with { PassengerPenaltyCredits = checked(f.PassengerPenaltyCredits + credits) }
        };
        f = f with { NetProfitCredits = VoyageNet(f) };
        _voyageLedgers = _voyageLedgers.SetItem(index, new(f, entry.PostingIds.Add(postingId)));
    }
    private ImmutableArray<VoyageLedgerEntry> PrepareVoyageSale(string postingId, TradeExecutionReceipt receipt)
    {
        if (string.IsNullOrWhiteSpace(postingId)) throw new ArgumentException("Sale posting requires identity.");
        if (receipt.ExecutedQuantity <= 0) return _voyageLedgers;
        for (int index = _voyageLedgers.Length - 1; index >= 0; index--)
        {
            var entry = _voyageLedgers[index]; var f = entry.Finance;
            if (f.State != VoyageFinanceStates.AwaitingRealization || f.DestinationStationObjectId != receipt.StationObjectId ||
                entry.PostingIds.Contains(postingId)) continue;
            int item = -1;
            for (int c = 0; c < f.UnsoldCargo.Length; c++)
                if (f.UnsoldCargo[c].ItemTypeId == receipt.ItemTypeId) { item = c; break; }
            if (item < 0) return _voyageLedgers;
            var carried = f.UnsoldCargo[item];
            long quantity = Math.Min(carried.Quantity, receipt.ExecutedQuantity);
            if (quantity <= 0) return _voyageLedgers;
            long proceeds = AllocateFuelCostBasis(receipt.ExecutedQuantity, receipt.TotalCredits, quantity);
            long? cost = receipt.RealizedCargoCostCredits is { } basis
                ? AllocateFuelCostBasis(receipt.ExecutedQuantity, basis, quantity) : null;
            var remainder = carried with
            {
                Quantity = carried.Quantity - quantity,
                // A local purchase can change the pooled receipt cost. If it exhausts more than
                // the captured carried basis, its residual cannot be proven; never publish a negative or invented basis.
                CostBasisCredits = carried.CostBasisCredits is { } original && cost is { } used && used <= original ? original - used : null
            };
            bool unknown = f.HasUnknownCostOfGoodsSold || cost is null;
            f = f with
            {
                GrossSalesCredits = checked(f.GrossSalesCredits + proceeds),
                CostOfGoodsSoldCredits = unknown ? null : checked(f.CostOfGoodsSoldCredits!.Value + cost!.Value),
                HasUnknownCostOfGoodsSold = unknown,
                UnsoldCargo = remainder.Quantity == 0 ? f.UnsoldCargo.RemoveAt(item) : f.UnsoldCargo.SetItem(item, remainder)
            };
            f = f with { NetProfitCredits = VoyageNet(f) };
            return _voyageLedgers.SetItem(index, new(f, entry.PostingIds.Add(postingId)));
        }
        return _voyageLedgers;
    }

    private ImmutableArray<VoyageFinanceSnapshot> BuildVoyageFinanceProjection() =>
        _voyageLedgers.Select(v => v.Finance).ToImmutableArray();

}
