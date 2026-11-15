using System.Collections.Immutable;
using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Engine.TacticalBattle.Content;
using DeepSpaceSaga.Engine.Scenario;
using DeepSpaceSaga.Motion.TacticalBattle;

namespace DeepSpaceSaga.Engine.TacticalBattle;

/// <summary>
/// Assembles a tactical battle whose player side is the world carrier and its persistent wing,
/// and whose enemy side is the encounter scenario. Deployment slots are matched by stable
/// DroneId order, never by display name; owned drones restore hull/armor/charges/ammo/jam,
/// disabled state and engine damage. Pilot identity/skills come from the persistent crew record.
/// </summary>
internal static class OwnedBattleAssembly
{
    public static TacticalBattle Create(TacticalBattleCatalog catalog, string shipId, int hullCurrent, int maneuver,
        ImmutableArray<OwnedDroneData> wing, ImmutableArray<CrewMemberRuntime> crew, BattleScenario encounter,
        string scenarioId, long seed, string playerSide = "A")
    {
        var random = new Mulberry32(seed);
        var drones = ImmutableArray.CreateBuilder<TacticalDrone>();
        var carriers = ImmutableArray.CreateBuilder<TacticalCarrier>();
        var selectedTactics = ImmutableArray.CreateBuilder<string>();
        var crewById = crew.ToDictionary(c => c.Id, StringComparer.Ordinal);

        foreach (var side in encounter.Sides)
        {
            selectedTactics.Add(side.Tactic);
            var carrierStart = side.Carrier;
            var carrierLayout = catalog.Layouts[carrierStart.Layout];
            var carrierFrame = catalog.Frames[TacticalBattleConstants.CarrierFrame];
            if (side.Id == playerSide)
            {
                carriers.Add(new(shipId, playerSide, new(carrierStart.X, carrierStart.Y, carrierStart.Course),
                    hullCurrent, catalog.Armor[carrierLayout.Armor].Armor, maneuver));

                // The encounter slot is pinned to the wing's stable DroneId index: a destroyed or
                // disabled drone consumes its slot (it keeps its pilot and place) and never shifts
                // survivors onto another alias. Destroyed tombstones are not built; a disabled
                // drone is built at its slot with the persisted disabled/engine-damage state (the
                // world sortie filter already refuses to launch one, this keeps direct assembly
                // honest).
                var ordered = wing.OrderBy(d => d.DroneId, StringComparer.Ordinal).ToArray();
                for (int i = 0; i < ordered.Length && i < side.Drones.Length; i++)
                {
                    var owned = ordered[i];
                    if (owned.Destroyed) continue;
                    drones.Add(BuildOwnedDrone(catalog, owned, side.Drones[i], playerSide, crewById));
                }
            }
            else
            {
                carriers.Add(new(carrierStart.Id, side.Id, new(carrierStart.X, carrierStart.Y, carrierStart.Course),
                    carrierFrame.Hull, catalog.Armor[carrierLayout.Armor].Armor, carrierFrame.Maneuver!.Value));
                foreach (var start in side.Drones)
                    drones.Add(BuildScenarioDrone(catalog, start, side.Id, random));
            }
        }

        foreach (var drone in drones)
        {
            var scatter = catalog.Rules.DeployScatter;
            double x = drone.Pose.X + random.Uniform(-scatter.Xy, scatter.Xy);
            double y = drone.Pose.Y + random.Uniform(-scatter.Xy, scatter.Xy);
            double course = TacticalGeometry.Norm(drone.Pose.Course + random.Uniform(-scatter.CourseDeg, scatter.CourseDeg));
            drone.Pose = new(x, y, course);
        }

        var assembledDrones = drones.ToImmutable();
        var assembledCarriers = carriers.ToImmutable();
        var battle = new TacticalBattle(catalog, scenarioId, seed, assembledDrones, assembledCarriers,
            selectedTactics.ToImmutable(), random)
        { Anomalies = TacticalAnomalyGenerator.Generate(catalog, seed, assembledDrones, assembledCarriers) };

        for (int i = 0; i < encounter.Sides.Length; i++)
        {
            var tactic = catalog.Ai.Tactics[battle.Tactics[i]];
            if (tactic.Adaptive) tactic = catalog.Ai.Tactics[catalog.Ai.Commander.StartTactic];
            var roles = Enumerable.Repeat("striker", tactic.Striker).Concat(Enumerable.Repeat("interceptor", tactic.Interceptor))
                .Concat(Enumerable.Repeat("fighter", tactic.Fighter)).ToArray();
            int slot = 0;
            foreach (var drone in battle.Drones.Where(d => d.Side == encounter.Sides[i].Id).OrderByDescending(d => d.MaxHull))
            {
                drone.Role = slot < roles.Length ? roles[slot] : TacticalBattleConstants.InitialRole;
                slot++;
            }
        }

        foreach (var drone in battle.Drones)
        {
            if (drone.Side != playerSide)
            {
                // Enemy pilots consume the initiative RNG stream exactly as the diagnostic assembly does.
                var initiative = catalog.Pilots[drone.PilotId].Initiative;
                drone.Initiative = initiative.IsRange
                    ? battle.InitiativeRandom.RandInt(initiative.Minimum, initiative.Maximum) : initiative.Minimum;
            }
            drone.Consciousness = catalog.Rules.Consciousness.Start;
        }

        battle.InitializeFireOrder();
        return battle;
    }

    private static TacticalDrone BuildOwnedDrone(TacticalBattleCatalog catalog, OwnedDroneData owned,
        BattleDroneStart start, string side, IReadOnlyDictionary<string, CrewMemberRuntime> crewById)
    {
        if (!crewById.TryGetValue(owned.CrewId, out var member))
            throw new ScenarioException($"Drone '{owned.DroneId}' pilot '{owned.CrewId}' is not a crew member.");

        var layout = catalog.Layouts[owned.FrameId];
        var frame = catalog.Frames[layout.Class];
        var weapon = catalog.Weapons[owned.WeaponId];
        var armor = catalog.Armor[owned.ArmorId];
        var shield = owned.ShieldId is { } shieldId ? catalog.Shields[shieldId] : null;
        var decoy = owned.DecoyId is { } decoyId ? catalog.Decoys[decoyId] : null;
        var engines = (owned.EngineIds ?? []).Select(id => new TacticalEngineState(catalog.Engines[id])).ToImmutableArray();
        int shieldCount = owned.ShieldCount;
        var limits = TacticalLimits.Of(new(frame.FrameMassKg, frame.ReferenceMassKg, frame.MinSpeed, frame.MaxSpeedCap, frame.TurnCapDeg),
            armor.MassKg, weapon.MassKg, shield?.MassKg ?? 0, shieldCount, engines.Select(e => e.Motion).ToArray(),
            moduleMass: (decoy?.MassKg ?? 0) + owned.AmmoCurrent * 1.0);
        double speed = PyMath.Clamp(start.Speed, limits.MinSpeed, limits.MaxSpeed);

        // Skills/initiative come from the persistent crew record once it carries the tactical scale
        // (the starter wing does); a legacy wing whose world skills are both 0 keeps the encounter
        // alias's catalog values. Identity is always the persistent CrewId, never the alias.
        var catalogPilot = catalog.Pilots[start.Pilot];
        bool crewSkills = member.AccuracySkill != 0 || member.ManeuverabilitySkill != 0;
        int accuracy = crewSkills ? member.AccuracySkill : catalogPilot.Accuracy.Minimum;
        int maneuver = crewSkills ? member.ManeuverabilitySkill : catalogPilot.Maneuver.Minimum;
        int initiative = crewSkills ? member.Initiative : catalogPilot.Initiative.Minimum;

        var drone = new TacticalDrone(owned.DroneId, side, new(start.X, start.Y, start.Course),
            owned.MaxHull, owned.MaxArmor, maneuver, accuracy,
            owned.CrewId, layout, frame, weapon, armor, shield, shieldCount, engines, speed, decoy)
        {
            PilotName = member.DisplayName,
            PilotGender = ParseCrewGender(member.Gender),
            PilotPortraitAssetId = member.PortraitAssetId,
            PilotRole = ParseCrewRole(member.Role),
            Hull = owned.CurrentHull,
            Armor = owned.Armor,
            AmmoCurrent = owned.AmmoCurrent,
            AmmoCapacity = owned.AmmoCapacity,
            AmmoUnitMassKg = 1.0,
            DecoyCharges = owned.DecoyCharges,
            Jammed = owned.Jammed,
            Initiative = initiative,
        };

        // Restore the persisted damage from the previous sortie (US-0006): a disabled drone stays
        // grounded (not destroyed, keeps pilot/slot/magazine) and engine factors apply per slot.
        if (owned.Disabled)
        {
            drone.Status = TacticalDroneStatus.Disabled;
            drone.Speed = 0;
        }
        if (owned.EngineFactors is { Count: > 0 } factors)
            for (int i = 0; i < factors.Count && i < drone.Engines.Length; i++)
                drone.Engines[i].Factor = factors[i];
        return drone;
    }

    private static CrewGender ParseCrewGender(string? value) =>
        Enum.TryParse<CrewGender>(value, ignoreCase: true, out var gender) ? gender : CrewGender.Female;

    private static CrewRole? ParseCrewRole(string? value) =>
        string.IsNullOrWhiteSpace(value)
            ? null
            : Enum.TryParse<CrewRole>(value, ignoreCase: true, out var role) ? role : null;

    private static TacticalDrone BuildScenarioDrone(TacticalBattleCatalog catalog, BattleDroneStart start, string side, Mulberry32 random)
    {
        var layout = catalog.Layouts[start.Layout];
        var frame = catalog.Frames[layout.Class];
        var weapon = catalog.Weapons[layout.Weapon!];
        var armor = catalog.Armor[layout.Armor];
        var shield = layout.Shield is { } shieldId ? catalog.Shields[shieldId] : null;
        var decoy = layout.Module is { } module ? catalog.Decoys[module] : null;
        int shieldCount = shield is null ? 0 : layout.ShieldCount ?? TacticalBattleConstants.DefaultShieldCount;
        var engines = layout.Engines.Select(id => new TacticalEngineState(catalog.Engines[id])).ToImmutableArray();
        var limits = TacticalLimits.Of(new(frame.FrameMassKg, frame.ReferenceMassKg, frame.MinSpeed, frame.MaxSpeedCap, frame.TurnCapDeg),
            armor.MassKg, weapon.MassKg, shield?.MassKg ?? 0, shieldCount, engines.Select(e => e.Motion).ToArray(),
            moduleMass: (decoy?.MassKg ?? 0) + frame.AmmoCapacity * 1.0);
        double speed = PyMath.Clamp(start.Speed, limits.MinSpeed, limits.MaxSpeed);
        var pilot = catalog.Pilots[start.Pilot];
        int accuracy = Skill(pilot.Accuracy), maneuver = Skill(pilot.Maneuver);
        return new(start.Id, side, new(start.X, start.Y, start.Course), layout.Hull ?? frame.Hull,
            armor.Armor, maneuver, accuracy, start.Pilot, layout, frame, weapon, armor, shield, shieldCount, engines, speed, decoy)
        {
            AmmoCurrent = frame.AmmoCapacity,
            AmmoCapacity = frame.AmmoCapacity,
            AmmoUnitMassKg = 1.0,
        };

        int Skill(PilotSkill skill) => skill.IsRange ? random.RandInt(skill.Minimum, skill.Maximum) : skill.Minimum;
    }
}
