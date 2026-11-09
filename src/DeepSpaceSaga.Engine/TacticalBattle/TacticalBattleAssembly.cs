using System.Collections.Immutable;
using DeepSpaceSaga.Engine.TacticalBattle.Content;
using DeepSpaceSaga.Motion.TacticalBattle;

namespace DeepSpaceSaga.Engine.TacticalBattle;

// Null means omitted; empty Shield explicitly removes it. No slot revalidation here
// (Generation §2). Public player commands validate their own narrower loadout contract.
internal sealed record TacticalLoadoutOverride(string? Weapon = null, string? Armor = null, string? Shield = null,
    int? ShieldCount = null, int? Hull = null, ImmutableArray<string>? Engines = null);

internal sealed partial class TacticalBattle
{
    public static TacticalBattle Create(TacticalBattleCatalog catalog, string? scenarioId, long seed,
        IReadOnlyDictionary<string, string>? tactics = null, IReadOnlyDictionary<string, TacticalLoadoutOverride>? loadouts = null)
    {
        scenarioId ??= catalog.Scenarios.Entries[0].Id;
        var scenario = catalog.Scenarios[scenarioId];
        var random = new Mulberry32(seed);
        var drones = ImmutableArray.CreateBuilder<TacticalDrone>();
        var carriers = ImmutableArray.CreateBuilder<TacticalCarrier>();
        var selectedTactics = ImmutableArray.CreateBuilder<string>();
        foreach (var side in scenario.Sides)
        {
            string tactic = tactics is not null && tactics.TryGetValue(side.Id, out var selected) ? selected : side.Tactic;
            _ = catalog.Ai.Tactics[tactic];
            selectedTactics.Add(tactic);
            var carrierStart = side.Carrier;
            var carrierLayout = catalog.Layouts[carrierStart.Layout];
            var carrierFrame = catalog.Frames[TacticalBattleConstants.CarrierFrame];
            carriers.Add(new(carrierStart.Id, side.Id, new(carrierStart.X, carrierStart.Y, carrierStart.Course),
                carrierFrame.Hull, catalog.Armor[carrierLayout.Armor].Armor, carrierFrame.Maneuver!.Value));
            foreach (var start in side.Drones)
            {
                var layout = catalog.Layouts[start.Layout];
                if (loadouts is not null && loadouts.TryGetValue(start.Id, out var overlay))
                    layout = layout with
                    {
                        Weapon = overlay.Weapon ?? layout.Weapon,
                        Armor = overlay.Armor ?? layout.Armor,
                        Shield = overlay.Shield is null ? layout.Shield : overlay.Shield.Length == 0 ? null : overlay.Shield,
                        ShieldCount = overlay.ShieldCount ?? layout.ShieldCount,
                        Hull = overlay.Hull ?? layout.Hull,
                        Engines = overlay.Engines ?? layout.Engines
                    };
                var frame = catalog.Frames[layout.Class];
                var weapon = catalog.Weapons[layout.Weapon!];
                var armor = catalog.Armor[layout.Armor];
                var shield = layout.Shield is { } shieldId ? catalog.Shields[shieldId] : null;
                var decoy = layout.Module is { } module ? catalog.Decoys[module] : null;
                int shieldCount = shield is null ? 0 : layout.ShieldCount ?? TacticalBattleConstants.DefaultShieldCount;
                var engines = layout.Engines.Select(id => new TacticalEngineState(catalog.Engines[id])).ToImmutableArray();
                var limits = TacticalLimits.Of(new(frame.FrameMassKg, frame.ReferenceMassKg, frame.MinSpeed, frame.MaxSpeedCap, frame.TurnCapDeg),
                    armor.MassKg, weapon.MassKg, shield?.MassKg ?? 0, shieldCount, engines.Select(e => e.Motion).ToArray(), moduleMass: decoy?.MassKg ?? 0);
                double speed = PyMath.Clamp(start.Speed, limits.MinSpeed, limits.MaxSpeed);
                var pilot = catalog.Pilots[start.Pilot];
                int accuracy = Skill(pilot.Accuracy), maneuver = Skill(pilot.Maneuver);
                drones.Add(new(start.Id, side.Id, new(start.X, start.Y, start.Course), layout.Hull ?? frame.Hull,
                    armor.Armor, maneuver, accuracy, start.Pilot, layout, frame, weapon, armor, shield, shieldCount, engines, speed, decoy));
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
        var battle = new TacticalBattle(catalog, scenarioId, seed, assembledDrones, assembledCarriers, selectedTactics.ToImmutable(), random)
        { Anomalies = TacticalAnomalyGenerator.Generate(catalog, seed, assembledDrones, assembledCarriers) };
        for (int i = 0; i < scenario.Sides.Length; i++)
        {
            var tactic = catalog.Ai.Tactics[battle.Tactics[i]];
            if (tactic.Adaptive) tactic = catalog.Ai.Tactics[catalog.Ai.Commander.StartTactic];
            var roles = Enumerable.Repeat("striker", tactic.Striker).Concat(Enumerable.Repeat("interceptor", tactic.Interceptor))
                .Concat(Enumerable.Repeat("fighter", tactic.Fighter)).ToArray();
            int slot = 0;
            foreach (var drone in battle.Drones.Where(d => d.Side == scenario.Sides[i].Id).OrderByDescending(d => d.MaxHull))
            {
                drone.Role = slot < roles.Length ? roles[slot] : TacticalBattleConstants.InitialRole;
                slot++;
            }
        }
        foreach (var drone in battle.Drones)
        {
            var initiative = catalog.Pilots[drone.PilotId].Initiative;
            drone.Initiative = initiative.IsRange
                ? battle.InitiativeRandom.RandInt(initiative.Minimum, initiative.Maximum) : initiative.Minimum;
            drone.Consciousness = catalog.Rules.Consciousness.Start;
        }
        battle.InitializeFireOrder();
        return battle;

        int Skill(PilotSkill skill) => skill.IsRange ? random.RandInt(skill.Minimum, skill.Maximum) : skill.Minimum;
    }
}
