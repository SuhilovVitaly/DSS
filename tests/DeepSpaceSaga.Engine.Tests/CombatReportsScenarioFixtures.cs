using System.Text.Json;
using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Engine.Content;
using DeepSpaceSaga.Engine.Scenario;
using static DeepSpaceSaga.Engine.Tests.TorpedoImpactTests;

namespace DeepSpaceSaga.Engine.Tests;

internal static class CombatReportsScenarioFixtures
{
    internal static GameDataRegistry ProductionRegistry() => EngineContentLoader.LoadRegistryFromSettingsFile(
        Path.Combine(ClientRoot, "Settings.json"), out _, out _);

    internal static (AuthoritativeSnapshot Final, BattleReportSnapshot[] Reports) ProductionRun(bool scanner = true)
    {
        var scenario = ScenarioLoader.LoadFromFile(Path.Combine(ClientRoot, "Scenarios", "CombatMukha", "scenario.json"));
        scenario = scenario with { GameState = scenario.GameState with { MasterSeed = 1 } };
        if (!scanner) scenario = scenario with
        {
            GameState = scenario.GameState with
            {
                SpaceObjects = scenario.GameState.SpaceObjects.Select(o =>
            o.ObjectId != Player ? o : o with { Modules = o.Modules!.Select(m => m.ModuleTypeId == "module.scanner.mk1" ? m with { PowerState = "Off" } : m).ToArray() }).ToArray()
            }
        };
        using var engine = Create(scenario, ProductionRegistry());
        var reports = new List<BattleReportSnapshot>();
        AuthoritativeSnapshot Capture(long time)
        {
            var snapshot = At(engine, time); reports.AddRange(snapshot.CombatReports!.Reports); return snapshot;
        }
        var current = Capture(0); ulong sequence = 0;
        for (long time = 0; time <= 600000; time += 1000)
        {
            current = Capture(time);
            if (current.EffectiveOutcome.State == SessionOutcomeState.Defeated) break;
            var enemy = current.Objects.FirstOrDefault(o => o.RelationToPlayer == PlayerRelation.Enemy && o.RenderObjectType == SpaceObjectType.NpcShip);
            if (enemy is null) break;
            foreach (var module in current.InstalledModules.Where(m => m.LauncherCombat is not null))
            {
                if (module.IsDestroyed || module.LauncherCombat!.ReadyAtMotionTimeMs > time) continue;
                var owner = current.Objects.Single(o => o.ObjectId == Player);
                var profile = Assert.Single(module.LauncherCombat.Profiles!.Value, p => p.ProfileId == "fast");
                var aim = ModuleCombatEndToEndTests.LinearAim(owner, enemy, profile.SpeedKmS);
                double range = module.LauncherCombat.RangeKm!.Value;
                if (Math.Pow((aim.X - owner.X) / 10, 2) + Math.Pow((aim.Y - owner.Y) / 10, 2) > range * range) continue;
                engine.ReceiveCommand(new($"proof-{++sequence}", sequence, Player, module.ModuleId, CombatCommandTypes.Launch,
                    TargetWorldX: aim.X, TargetWorldY: aim.Y, ProfileId: profile.ProfileId));
                current = Capture(time);
            }
        }
        return (current, reports.ToArray());
    }

}
