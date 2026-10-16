using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Engine.Scenario;
using static DeepSpaceSaga.Engine.Tests.TorpedoImpactTests;

namespace DeepSpaceSaga.Engine.Tests;

public sealed class FireModeRuntimeTests
{
    private static SimulationEngine CreateInMode(string modeId, ulong seed)
    {
        var scenario = TorpedoImpactTests.Scenario();
        scenario = scenario with { GameState = scenario.GameState with { MasterSeed = seed } };
        var engine = Create(scenario);
        var save = engine.CaptureSaveStateForTests(0, SimulationSpeed.Speed0);
        var combat = save.GameState.CombatState!;
        engine.LoadScenario(save with
        {
            GameState = save.GameState with
            {
                CombatState = combat with
                {
                    Launchers = combat.Launchers.Select(row => row.ModuleId == Launcher
                        ? row with { State = row.State with { ModeId = modeId } } : row).ToArray()
                }
            }
        }, isSave: true);
        return engine;
    }

    [Fact]
    public void Maneuverability_mode_is_captured_and_reloads_at_18750()
    {
        using var engine = CreateInMode("maneuverability", 42);
        engine.ReceiveCommand(Fire("mode-flight"));
        var launched = At(engine, 0);
        var flight = Assert.Single(launched.Objects, o => o.Torpedo is not null).Torpedo!;
        Assert.Equal("maneuverability", flight.ModeId);
        Assert.Equal(flight.RatingBreakdown!.EffectiveRating + 10, flight.CapturedManeuverability);
        Assert.Equal(10, flight.ModeManeuverabilityBonus);
        Assert.Equal(18750, launched.InstalledModules.Single(m => m.ModuleId == Launcher)
            .LauncherCombat!.ReadyAtMotionTimeMs);
        Assert.Equal(18750, launched.InstalledModules.Single(m => m.ModuleId == Launcher)
            .LauncherCombat!.ReloadDurationMs);
        ScenarioLoader.LoadFromJson(ScenarioLoader.Serialize(
            engine.CaptureSaveStateForTests(0, SimulationSpeed.Speed0)), true);
    }

    [Fact]
    public void Maneuverability_mode_lowers_production_intercept_chance_to_48_percent()
    {
        var registry = DeepSpaceSaga.Engine.Content.EngineContentLoader.LoadRegistryFromSettingsFile(
            Path.Combine(ClientRoot, "Settings.json"), out _, out _);
        using var engine = Create(AutomaticDefenseTests.DefenseScenario(), registry);
        var save = engine.CaptureSaveStateForTests(0, SimulationSpeed.Speed0);
        var combat = save.GameState.CombatState!;
        engine.LoadScenario(save with
        {
            GameState = save.GameState with
            {
                CombatState = combat with
                {
                    Launchers = combat.Launchers.Select(row => row.ModuleId == Launcher
                        ? row with { State = row.State with { ModeId = "maneuverability" } } : row).ToArray()
                }
            }
        }, isSave: true);
        engine.ReceiveCommand(Fire("mode-defense"));
        At(engine, 0);
        var snapshot = At(engine, 100);
        var flight = Assert.Single(snapshot.Objects, o => o.Torpedo is not null).Torpedo!;
        var defense = Assert.Single(snapshot.Objects, o => o.Countermeasure is not null).Countermeasure!;
        Assert.Equal(60, flight.CapturedManeuverability);
        Assert.Equal(480, defense.FrozenChanceTenths);
    }

    [Fact]
    public void Overheat_breakdown_is_deterministic_and_persists_until_dock()
    {
        bool found = false;
        for (ulong seed = 0; seed < 100 && !found; seed++)
        {
            using var engine = CreateInMode("overheat", seed);
            engine.ReceiveCommand(Fire("overheat"));
            var launched = At(engine, 0);
            Assert.Single(launched.Objects, o => o.Torpedo is not null);
            Assert.Equal(18750, launched.InstalledModules.Single(m => m.ModuleId == Launcher)
                .LauncherCombat!.ReadyAtMotionTimeMs);
            var combat = engine.CaptureSaveState().GameState.CombatState!;
            Assert.Equal(1UL, combat.LauncherBreakdownRngCounter);
            if (launched.InstalledModules.Single(m => m.ModuleId == Launcher).LauncherCombat!.IsBrokenUntilDock)
            {
                found = true;
                var breakdown = Assert.Single(launched.CombatJournal, e => e.Type == CombatEventType.LauncherBreakdown);
                Assert.Equal(100, breakdown.ChanceTenths);
                Assert.InRange(breakdown.Roll!.Value, 1, 100);
                var save = ScenarioLoader.LoadFromJson(ScenarioLoader.Serialize(
                    engine.CaptureSaveStateForTests(0, SimulationSpeed.Speed0)), true);
                using var restored = new SimulationEngine(Registry.Value);
                restored.LoadScenario(save, isSave: true);
                Assert.True(restored.CaptureSnapshot().InstalledModules.Single(m => m.ModuleId == Launcher)
                    .LauncherCombat!.IsBrokenUntilDock);
                Assert.Equal(combat.LauncherBreakdownRngState,
                    restored.CaptureSaveState().GameState.CombatState!.LauncherBreakdownRngState);
                At(restored, 18750);
                restored.ReceiveCommand(Fire("broken"));
                Assert.Equal("launcher_broken_until_dock", At(restored, 18750).CommandResults
                    .Single(r => r.CommandId == "broken").ReasonCode);
            }
        }
        Assert.True(found, "At least one of the first 100 deterministic seeds must break the overheat launcher.");
    }
}
