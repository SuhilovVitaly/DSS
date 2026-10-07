using System.Collections.Immutable;
using System.Globalization;
using System.Numerics;
using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Engine.Scenario;

namespace DeepSpaceSaga.Engine;

public sealed partial class SimulationEngine
{
    private VoyageFuelSettlementSnapshot? _lastVoyageFuelSettlement;

    private long AvailableFuelTankCapacity(string moduleId, long capacity)
    {
        long reserved = _voyageState?.FuelReservationParts?.Where(p => p.ModuleId == moduleId).Sum(p => p.ReservedFuelKg) ?? 0;
        return checked(capacity - reserved);
    }

    // Map geometry is double in the established map contract. Convert its round-trip
    // decimal representation once, then perform fuel accounting entirely with integers.
    internal static decimal CaptureFuelDistance(double distanceKm) =>
        decimal.Parse(distanceKm.ToString("R", CultureInfo.InvariantCulture), NumberStyles.Float, CultureInfo.InvariantCulture);

    internal static long CalculateVoyageFuel(decimal distanceKm, int multiplier, long efficiency)
    {
        if (distanceKm <= 0 || multiplier <= 0 || efficiency <= 0) throw new ArgumentOutOfRangeException(nameof(distanceKm));
        int[] bits = decimal.GetBits(distanceKm);
        BigInteger units = (uint)bits[0] + ((BigInteger)(uint)bits[1] << 32) + ((BigInteger)(uint)bits[2] << 64);
        BigInteger denominator = (BigInteger)efficiency * 1000 * BigInteger.Pow(10, (bits[3] >> 16) & 255);
        BigInteger numerator = units * multiplier;
        return checked((long)((numerator + denominator - 1) / denominator));
    }

    private string? PrepareVoyageFuel(SpaceObjectRuntime ship, double distanceKm, int multiplier,
        out ImmutableArray<InstalledModuleRuntime> modules,
        out ImmutableArray<VoyageFuelReservationPartData> parts, out decimal distance, out long efficiency)
    {
        modules = ship.Modules;
        parts = [];
        distance = 0;
        efficiency = 0;
        var tanks = ship.Modules.Select((m, i) => (Module: m, Index: i,
            Type: _registry.ModuleTypes.GetDefinition(m.ModuleTypeIndex))).Where(m => m.Type.FuelCapacityKg is > 0).ToArray();
        if (tanks.Length == 0 || tanks.Any(t => t.Type.FuelEfficiencyKmPerKg is not > 0) ||
            tanks.Select(t => t.Type.FuelEfficiencyKmPerKg).Distinct().Count() != 1)
            return CommandReasonCodes.FuelEfficiencyUnavailable;
        efficiency = tanks[0].Type.FuelEfficiencyKmPerKg!.Value;
        try
        {
            distance = CaptureFuelDistance(distanceKm);
            long remaining = CalculateVoyageFuel(distance, multiplier, efficiency);
            Int128 available = 0;
            foreach (var tank in tanks) available += tank.Module.FuelAmountKg;
            if (available < remaining) return CommandReasonCodes.InsufficientVoyageFuel;
            var staged = ship.Modules.ToBuilder();
            var reservations = ImmutableArray.CreateBuilder<VoyageFuelReservationPartData>();
            long aggregateBasis = 0;
            foreach (var tank in tanks)
            {
                long taken = Math.Min(remaining, tank.Module.FuelAmountKg);
                if (taken == 0) continue;
                long basis = AllocateFuelCostBasis(tank.Module.FuelAmountKg, tank.Module.FuelCostBasisCredits, taken);
                aggregateBasis = checked(aggregateBasis + basis);
                reservations.Add(new(tank.Module.ModuleId, taken, basis));
                staged[tank.Index] = tank.Module with
                {
                    FuelAmountKg = tank.Module.FuelAmountKg - taken,
                    FuelCostBasisCredits = tank.Module.FuelCostBasisCredits - basis
                };
                remaining -= taken;
                if (remaining == 0) break;
            }
            modules = staged.ToImmutable();
            parts = reservations.ToImmutable();
            return null;
        }
        catch (Exception error) when (error is OverflowException or FormatException or ArgumentOutOfRangeException)
        { return "value_overflow"; }
    }

    private static (long Reserved, long Consumed, long Cost) ProjectVoyageFuel(VoyageStateData voyage, bool arrived = false)
    {
        var parts = voyage.FuelReservationParts;
        if (parts is null) return (0, 0, 0); // Legacy active voyage: no retroactive debit.
        long reserved = 0;
        foreach (var part in parts) reserved = checked(reserved + part.ReservedFuelKg);
        long consumed = arrived ? reserved : checked((long)(((Int128)reserved * voyage.ProgressPermille + 999) / 1000));
        long remaining = consumed;
        long cost = 0;
        foreach (var part in parts)
        {
            long taken = Math.Min(remaining, part.ReservedFuelKg);
            cost = checked(cost + AllocateFuelCostBasis(part.ReservedFuelKg, part.ReservedFuelCostBasisCredits, taken));
            remaining -= taken;
        }
        return (reserved, consumed, cost);
    }

    // Called under the world lock, before the terminal lifecycle transition. Refunds
    // are staged in original tanks; any invalid capacity or identity leaves state intact.
    private VoyageFuelSettlementSnapshot? SettleVoyageFuel(VoyageStateData voyage, bool arrived)
    {
        if (_durableVoyageTerminalIds.Contains(voyage.VoyageId!))
            return _voyageFuelSettlements.GetValueOrDefault(voyage.VoyageId!);
        if (voyage.FuelReservationParts is null)
        {
            RememberVoyageTerminal(voyage.VoyageId!);
            return null;
        }
        var projected = ProjectVoyageFuel(voyage, arrived);
        int shipIndex = _objects.FindIndex(o => o.InitialMotion.ObjectId == PlayerShipObjectId);
        if (shipIndex < 0) throw new InvalidOperationException("Reserved fuel requires its player ship.");
        var ship = _objects[shipIndex];
        var modules = ship.Modules.ToBuilder();
        long remainingConsumed = projected.Consumed;
        foreach (var part in voyage.FuelReservationParts)
        {
            int index = -1;
            for (int i = 0; i < modules.Count; i++) if (modules[i].ModuleId == part.ModuleId) { index = i; break; }
            if (index < 0) throw new InvalidOperationException("Reserved fuel source module is missing.");
            var module = modules[index];
            var type = _registry.ModuleTypes.GetDefinition(module.ModuleTypeIndex);
            long consumed = Math.Min(remainingConsumed, part.ReservedFuelKg);
            long consumedBasis = AllocateFuelCostBasis(part.ReservedFuelKg, part.ReservedFuelCostBasisCredits, consumed);
            long amount = checked(module.FuelAmountKg + part.ReservedFuelKg - consumed);
            long basis = checked(module.FuelCostBasisCredits + part.ReservedFuelCostBasisCredits - consumedBasis);
            if (type.FuelCapacityKg is not > 0 || amount > type.FuelCapacityKg) throw new InvalidOperationException("Reserved fuel refund exceeds source capacity.");
            modules[index] = module with { FuelAmountKg = amount, FuelCostBasisCredits = basis };
            remainingConsumed -= consumed;
        }
        var result = new VoyageFuelSettlementSnapshot(voyage.VoyageId!, projected.Reserved, projected.Consumed,
            projected.Reserved - projected.Consumed, projected.Cost);
        _objects[shipIndex] = ship with { Modules = modules.ToImmutable() };
        _lastVoyageFuelSettlement = result;
        RememberVoyageTerminal(result.VoyageId);
        _voyageFuelSettlements[result.VoyageId] = result;
        foreach (string oldId in _voyageFuelSettlements.Keys.Where(id => id != result.VoyageId &&
            !_voyageLedgers.Any(l => l.Finance.VoyageId == id)).ToArray()) _voyageFuelSettlements.Remove(oldId);
        return result;
    }

    private void ValidateVoyageFuelSave(GameStateData state, VoyageStateData? voyage, IReadOnlyList<SpaceObjectRuntime> objects, StationClusterMapSnapshot? clusterMap = null)
    {
        var receipt = state.LastVoyageFuelSettlement;
        if (receipt is not null && (string.IsNullOrWhiteSpace(receipt.VoyageId) || receipt.ReservedFuelKg <= 0 ||
            receipt.ConsumedFuelKg < 0 || receipt.ReturnedFuelKg < 0 || receipt.RouteFuelCostCredits < 0 ||
            (Int128)receipt.ConsumedFuelKg + receipt.ReturnedFuelKg != receipt.ReservedFuelKg ||
            receipt.ConsumedFuelKg == 0 && receipt.RouteFuelCostCredits != 0 || receipt.VoyageId == voyage?.VoyageId))
            throw new ScenarioException("lastVoyageFuelSettlement is invalid.");
        if (voyage is null) return;
        bool hasFuel = voyage.FuelReservationParts is not null || voyage.FuelDistanceKm is not null || voyage.FuelEfficiencyKmPerKg is not null;
        if (!hasFuel) return;
        if (voyage.Phase == VoyagePhases.Docked || voyage.FuelReservationParts is not { Count: > 0 } parts ||
            voyage.FuelDistanceKm is not > 0 || voyage.FuelEfficiencyKmPerKg is not > 0 || voyage.FuelMultiplierPermille is not > 0)
            throw new ScenarioException("voyageState fuel reservation metadata is incomplete.");
        var departureMap = BuildClusterVoyageMap(state.TradingMap, clusterMap ?? state.ClusterMap, objects, voyage.StartedMotionTimeMs)!;
        var edge = departureMap.Edges.Single(e => Connects(e, voyage.OriginStationObjectId!, voyage.DestinationStationObjectId!));
        try
        {
            if (voyage.FuelDistanceKm != CaptureFuelDistance(edge.DistanceKm))
                throw new ScenarioException("voyageState fuel distance differs from its materialized edge.");
        }
        catch (Exception error) when (error is OverflowException or FormatException)
        { throw new ScenarioException("voyageState materialized fuel distance is invalid.", error); }
        var ship = objects.Single(o => o.InitialMotion.ObjectId == state.PlayerShipObjectId);
        var seen = new HashSet<string>(StringComparer.Ordinal);
        long reserved = 0, totalBasis = 0;
        try
        {
            foreach (var part in parts)
            {
                var module = ship.Modules.FirstOrDefault(m => m.ModuleId == part.ModuleId);
                if (string.IsNullOrWhiteSpace(part.ModuleId) || !seen.Add(part.ModuleId) || part.ReservedFuelKg <= 0 ||
                    part.ReservedFuelCostBasisCredits < 0 || module is null)
                    throw new ScenarioException("voyageState fuel reservation part is invalid.");
                var type = _registry.ModuleTypes.GetDefinition(module.ModuleTypeIndex);
                if (type.FuelCapacityKg is not > 0 || checked(module.FuelAmountKg + part.ReservedFuelKg) > type.FuelCapacityKg)
                    throw new ScenarioException("voyageState fuel reservation source capacity is invalid.");
                _ = checked(module.FuelCostBasisCredits + part.ReservedFuelCostBasisCredits);
                reserved = checked(reserved + part.ReservedFuelKg);
                totalBasis = checked(totalBasis + part.ReservedFuelCostBasisCredits);
            }
            if (reserved != CalculateVoyageFuel(voyage.FuelDistanceKm.Value, voyage.FuelMultiplierPermille.Value, voyage.FuelEfficiencyKmPerKg.Value))
                throw new ScenarioException("voyageState fuel reservation disagrees with captured terms.");
        }
        catch (OverflowException error) { throw new ScenarioException("voyageState fuel reservation overflowed.", error); }
    }
}
