using System.Collections.Immutable;
using DeepSpaceSaga.Engine.Scenario;

namespace DeepSpaceSaga.Engine;

public sealed partial class SimulationEngine
{
    private static void ValidateCargoCost(CargoStackRuntime stack)
    {
        if (stack.Quantity < 0 || stack.CostBasisCredits is < 0 || stack.Quantity == 0 && stack.CostBasisCredits is > 0)
            throw new ArgumentException("Cargo quantity and basis must be consistent and nonnegative.");
        if (stack.CostBasisCredits is null)
        {
            if (!stack.AcquisitionSources.IsDefaultOrEmpty && (stack.AcquisitionSources.Length != 1 || stack.AcquisitionSources[0] != CargoAcquisitionSources.LegacyUnknown))
                throw new ArgumentException("Unknown cargo requires legacy-unknown provenance.");
        }
        else if (stack.AcquisitionSources.IsDefaultOrEmpty || stack.AcquisitionSources.Any(s => !CargoAcquisitionSources.IsKnown(s)) ||
            stack.AcquisitionSources.Distinct(StringComparer.Ordinal).Count() != stack.AcquisitionSources.Length)
            throw new ArgumentException("Known cargo requires valid acquisition provenance.");
    }

    internal static CargoStackRuntime AddCargoCost(CargoStackRuntime? existing, int itemTypeIndex,
        long acquiredQuantity, long acquisitionCostCredits, string acquisitionSource)
    {
        if (acquiredQuantity <= 0 || acquisitionCostCredits < 0 || !CargoAcquisitionSources.IsKnown(acquisitionSource))
            throw new ArgumentOutOfRangeException(nameof(acquiredQuantity));
        if (existing is not null)
        {
            ValidateCargoCost(existing);
            if (existing.ItemTypeIndex != itemTypeIndex) throw new ArgumentException("Cannot merge different cargo types.");
        }
        if (existing is null || existing.Quantity == 0)
            return new(itemTypeIndex, acquiredQuantity, acquisitionCostCredits, [acquisitionSource]);
        long quantity = checked(existing.Quantity + acquiredQuantity);
        if (existing.CostBasisCredits is null)
            return new(itemTypeIndex, quantity, null, [CargoAcquisitionSources.LegacyUnknown]);
        long basis = checked(existing.CostBasisCredits.Value + acquisitionCostCredits);
        var sources = existing.AcquisitionSources.Append(acquisitionSource).Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal).ToImmutableArray();
        return new(itemTypeIndex, quantity, basis, sources);
    }

    internal static (CargoStackRuntime? Remaining, long? RealizedCostCredits) RemoveCargoCost(CargoStackRuntime existing, long removedQuantity)
    {
        ValidateCargoCost(existing);
        if (removedQuantity <= 0 || removedQuantity > existing.Quantity) throw new ArgumentOutOfRangeException(nameof(removedQuantity));
        if (removedQuantity == existing.Quantity) return (null, existing.CostBasisCredits);
        long? realized = existing.CostBasisCredits is { } basis ? AllocateFuelCostBasis(existing.Quantity, basis, removedQuantity) : null;
        return (existing with
        {
            Quantity = existing.Quantity - removedQuantity,
            CostBasisCredits = realized is null ? null : existing.CostBasisCredits!.Value - realized.Value,
            AcquisitionSources = realized is null ? [CargoAcquisitionSources.LegacyUnknown] : existing.AcquisitionSources
        }, realized);
    }
}
