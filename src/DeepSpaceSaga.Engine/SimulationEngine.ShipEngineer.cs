using DeepSpaceSaga.Engine.Rng;
using DeepSpaceSaga.Engine.Scenario;

namespace DeepSpaceSaga.Engine;

public sealed partial class SimulationEngine
{
    private static bool IsEngineer(string? role) => string.Equals(role, "Engineer", StringComparison.OrdinalIgnoreCase);

    internal static void ValidateEngineerCrew(SpaceObjectData ship)
    {
        int engineers = 0;
        foreach (var member in ship.Crew ?? [])
        {
            if (member is null) throw new ScenarioException("Null crew member.");
            bool engineer = IsEngineer(member.Role);
            if (engineer && ++engineers > 1) throw new ScenarioException("Only one assigned engineer is supported per ship.");
            if (engineer ? member.SalvageBonusPercentagePoints is < 10 or > 15
                : member.SalvageBonusPercentagePoints is not (null or 0))
                throw new ScenarioException("Invalid engineer salvage skill.");
        }
    }

    internal static int ResolveEngineerBonus(ulong seed, string shipId, ShipCrewMemberData member)
    {
        if (!IsEngineer(member.Role)) return 0;
        return member.SalvageBonusPercentagePoints ?? 10 + RngStreamNames.CreateDeterministicRandom(
            RngStreamSeedDerivation.DeriveStreamSeed(seed, "engineer-salvage/v1/" + shipId + "/" + member.CrewId)).Next(6);
    }

    internal double GetAssignedEngineerBonusFraction()
    {
        var ship = _objects.FirstOrDefault(o => o.InitialMotion.ObjectId == PlayerShipObjectId);
        if (ship is null || ship.IsDestroyed) return 0;
        return (ship.Crew.SingleOrDefault(c => IsEngineer(c.Role))?.SalvageBonusPercentagePoints ?? 0) / 100.0;
    }
}
