using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Engine;
using DeepSpaceSaga.Engine.Scenario;
using DeepSpaceSaga.Engine.Content;
using Xunit.Abstractions;
using static DeepSpaceSaga.Engine.Tests.TorpedoImpactTests;

namespace DeepSpaceSaga.Engine.Tests;

public sealed class ModuleCombatEndToEndTests
{
    private readonly ITestOutputHelper _output;

    public ModuleCombatEndToEndTests(ITestOutputHelper output) => _output = output;

    [Theory]
    [InlineData("CombatMukha")]
    public void Real_scenario_runs_hostile_ai_player_fire_and_module_damage(string scenarioName)
    {
        var result = Run(scenarioName, 1);
        _output.WriteLine(result.ToString());
        Assert.True(result.HostileLaunches > 0, "Hostile AI must launch from the shipped scenario.");
        Assert.True(result.PlayerLaunches > 0, "Player fire must execute through the command path.");
        Assert.Equal(0, result.HitRolls);
        Assert.Equal(0, result.LegacyChanceFacts);
        Assert.True(result.Detonations > 0, "Real point flights must resolve geometric detonations.");
        Assert.True(result.ModuleHits > 0, "Contacts must reach module armor/structure.");
        Assert.True(result.Finished, "Combat must reach a terminal outcome within the documented horizon.");
    }

    internal static CombatResult Run(string scenarioName, ulong seed)
    {
        string path = Path.Combine(ClientRoot, "Scenarios", scenarioName, "scenario.json");
        var source = ScenarioLoader.LoadFromFile(path);
        string playerId = source.GameState.PlayerShipObjectId!;
        string[] hostileIds = source.GameState.SpaceObjects.Where(o => o.ObjectType == SpaceObjectType.NpcShip)
            .Select(o => o.ObjectId).ToArray();
        var registry = EngineContentLoader.LoadRegistryFromSettingsFile(Path.Combine(ClientRoot, "Settings.json"), out _, out _);
        using var engine = new SimulationEngine(registry);
        engine.LoadScenario(source with { GameState = source.GameState with { MasterSeed = seed } });
        ulong commandSequence = 0;
        AuthoritativeSnapshot snapshot = At(engine, 0);
        for (long time = 0; time <= 600_000; time += 5_000)
        {
            snapshot = At(engine, time);
            if (snapshot.EffectiveOutcome.State == SessionOutcomeState.Defeated ||
                hostileIds.All(id => snapshot.Objects.All(o => o.ObjectId != id)))
                break;
            var target = snapshot.Objects.Where(o => hostileIds.Contains(o.ObjectId)).OrderBy(o => o.ObjectId, StringComparer.Ordinal).FirstOrDefault();
            if (target is null) break;
            foreach (var module in snapshot.InstalledModules.Where(m => m.LauncherCombat is not null))
            {
                var launcher = module.LauncherCombat!;
                if (launcher.IsBrokenUntilDock || (launcher.ReadyAtMotionTimeMs ?? 0) > snapshot.MotionTimeMs)
                    continue;
                var owner = snapshot.Objects.Single(o => o.ObjectId == playerId);
                var profile = Assert.Single(launcher.Profiles!.Value, p => p.ProfileId == "fast");
                var aim = LinearAim(owner, target, profile.SpeedKmS);
                double dx = (aim.X - owner.X) / 10, dy = (aim.Y - owner.Y) / 10;
                double range = launcher.RangeKm!.Value;
                if (dx * dx + dy * dy > range * range) continue;
                engine.ReceiveCommand(new PlayerCommand($"integration-{seed}-{++commandSequence}",
                    commandSequence, playerId, module.ModuleId, CombatCommandTypes.Launch,
                    TargetWorldX: aim.X, TargetWorldY: aim.Y, ProfileId: profile.ProfileId));
                snapshot = At(engine, time);
            }
        }
        var journal = snapshot.CombatJournal;
        bool defeated = snapshot.EffectiveOutcome.State == SessionOutcomeState.Defeated;
        bool victory = !defeated && hostileIds.All(id => snapshot.Objects.All(o => o.ObjectId != id));
        return new CombatResult(scenarioName, seed, victory, defeated, snapshot.MotionTimeMs,
            journal.Count(e => e.Type == CombatEventType.Launch && e.ActorObjectId == playerId),
            journal.Count(e => e.Type == CombatEventType.Launch && e.ActorObjectId != playerId),
            journal.Count(e => e.Type.ToString() == "TorpedoHitRoll"),
            journal.Count(e => e.Type == CombatEventType.ModuleHit),
            journal.Count(e => e.Type == CombatEventType.Hit && e.TargetObjectId == playerId),
            journal.Count(e => e.Type == CombatEventType.Launch && e.ProjectileKind == ProjectileKind.Torpedo && e.ActorObjectId != playerId),
            journal.Count(e => e.Type == CombatEventType.Launch && e.ActorObjectId == playerId && e.ProjectileKind == ProjectileKind.Countermissile),
            journal.Count(e => e.Type is CombatEventType.TorpedoDetonated or CombatEventType.CountermissileDetonated or CombatEventType.ManuallyDetonated),
            journal.Count(e => e.ChanceTenths is not null || e.Roll is not null));
    }

    // Input policy uses only current public position, speed and bearing; no AI segment, private endpoint or future snapshot.
    internal static (double X, double Y) LinearAim(ObjectMotionSnapshot owner, ObjectMotionSnapshot target, double speedKmS)
    {
        double angle = target.Direction * Math.PI / 180;
        double vx = target.SpeedKmS * Math.Sin(angle) * 10, vy = -target.SpeedKmS * Math.Cos(angle) * 10;
        double time = 0;
        for (int i = 0; i < 24; i++)
        {
            double dx = target.X + vx * time - owner.X, dy = target.Y + vy * time - owner.Y;
            time = Math.Sqrt(dx * dx + dy * dy) / (speedKmS * 10);
        }
        return (target.X + vx * time, target.Y + vy * time);
    }

    internal sealed record CombatResult(string Scenario, ulong Seed, bool Victory, bool Defeat, long DurationMs,
        int PlayerLaunches, int HostileLaunches, int HitRolls, int ModuleHits,
        int PlayerHits, int EnemyTorpedoes, int PlayerCountermeasureLaunches, int Detonations, int LegacyChanceFacts)
    {
        public bool Finished => Victory || Defeat;
    }
}
