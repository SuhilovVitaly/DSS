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
                if (module.LauncherCombat!.IsBrokenUntilDock || module.LauncherCombat.ReadyAtMotionTimeMs > time) continue;
                engine.ReceiveCommand(new($"proof-{++sequence}", sequence, Player, module.ModuleId, CombatCommandTypes.Fire, enemy.ObjectId));
                current = Capture(time);
            }
        }
        return (current, reports.ToArray());
    }

    internal static BattleReportSnapshot[] AttackReports()
    {
        using var engine = Create(); var reports = new List<BattleReportSnapshot>();
        for (int i = 0; i < 3; i++)
        {
            long time = i * 15000; engine.ReceiveCommand(Fire($"hit-{i}"));
            reports.AddRange(At(engine, time).CombatReports!.Reports);
            reports.AddRange(At(engine, time + 5000).CombatReports!.Reports);
        }
        return reports.ToArray();
    }

    internal static BattleReportSnapshot[] InterceptReports()
    {
        using var engine = Create(HostileTorpedoFireTests.HostileScenario(), AutomaticDefenseTests.WeaponRegistry(100, 0));
        var reports = At(engine, 1).CombatReports!.Reports.ToList();
        string target = engine.RuntimeObjects.Single(o => o.InitialMotion.Torpedo is not null).InitialMotion.ObjectId;
        engine.ReceiveCommand(ManualCountermeasureFireTests.Manual("intercept", target));
        reports.AddRange(At(engine, 1).CombatReports!.Reports); reports.AddRange(At(engine, 12000).CombatReports!.Reports);
        return reports.ToArray();
    }
}
