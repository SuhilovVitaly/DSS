using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Engine.Scenario;
using DeepSpaceSaga.Engine.Content;
using static DeepSpaceSaga.Engine.Tests.TorpedoImpactTests;

namespace DeepSpaceSaga.Engine.Tests;

public class CountermeasureBootstrapTests
{
    internal static ScenarioFile RealScenario() => ScenarioLoader.LoadFromFile(global::DeepSpaceSaga.TestSupport.LegacyCombatCatalog.Scenario(Path.Combine(ClientRoot, "Scenarios", "PlayerShipOnly", "scenario.json")));

    [Fact]
    public void Both_launchers_bootstrap_independently()
    {
        using var engine = EngineContentLoader.CreateEngineFromScenarioFile(global::DeepSpaceSaga.TestSupport.LegacyCombatCatalog.Settings(Path.Combine(ClientRoot, "Settings.json")), global::DeepSpaceSaga.TestSupport.LegacyCombatCatalog.Scenario(
            Path.Combine(ClientRoot, "Scenarios", "PlayerShipOnly", "scenario.json")));
        var defenses = At(engine, 0).InstalledModules.Where(m => m.Defense is not null).ToArray();
        Assert.Equal(2, defenses.Length);
        Assert.All(defenses, m => { Assert.True(m.Defense!.AutoEnabled); Assert.Equal(DefenseState.Ready, m.Defense.State); });
        Assert.NotEqual(defenses[0].Operator!.CrewId, defenses[1].Operator!.CrewId);
        engine.ReceiveCommand(new("disable-first", 1, Player, defenses[0].ModuleId, DefenseCommandTypes.Disable));
        var after = At(engine, 0);
        Assert.Equal(CommandResultStatus.Executed, Assert.Single(after.CommandResults).Status);
        Assert.False(after.InstalledModules.Single(m => m.ModuleId == defenses[0].ModuleId).Defense!.AutoEnabled);
        Assert.Equal(defenses[1].Defense, after.InstalledModules.Single(m => m.ModuleId == defenses[1].ModuleId).Defense);

        // Restore two valid independently reloading modules and cross each physical deadline.
        var save = engine.CaptureSaveStateForTests(0, SimulationSpeed.Speed0);
        save = save with
        {
            GameState = save.GameState with
            {
                DefenseState = save.GameState.DefenseState! with
                {
                    Launchers = save.GameState.DefenseState!.Launchers.Select(l => l.OwnerObjectId == Player
                        ? l with
                        {
                            State = l.State with
                            {
                                State = DefenseState.Reloading,
                                ReloadDueMotionTimeMs = l.ModuleId == defenses[0].ModuleId ? 1000 : 2000
                            }
                        } : l).ToArray()
                }
            }
        };
        string path = Path.Combine(Path.GetTempPath(), $"dual-defense-{Guid.NewGuid():N}.json");
        try
        {
            File.WriteAllText(path, ScenarioLoader.Serialize(save));
            using var restored = EngineContentLoader.CreateEngineFromSaveFile(global::DeepSpaceSaga.TestSupport.LegacyCombatCatalog.Settings(Path.Combine(ClientRoot, "Settings.json")), path);
            var firstDeadline = At(restored, 1000);
            var first = firstDeadline.InstalledModules.Single(m => m.ModuleId == defenses[0].ModuleId).Defense!;
            var second = firstDeadline.InstalledModules.Single(m => m.ModuleId == defenses[1].ModuleId).Defense!;
            Assert.Equal(DefenseState.Ready, first.State);
            Assert.False(first.AutoEnabled);
            Assert.Null(first.ReloadDueMotionTimeMs);
            Assert.Equal(DefenseState.Reloading, second.State);
            Assert.True(second.AutoEnabled);
            Assert.Equal(2000, second.ReloadDueMotionTimeMs);
            Assert.All(At(restored, 2000).InstalledModules.Where(m => m.Defense is not null),
                m => Assert.Equal(DefenseState.Ready, m.Defense!.State));
        }
        finally { File.Delete(path); }
    }

    [Fact]
    public void Generated_world_keeps_loadout()
    {
        using var engine = EngineContentLoader.CreateEngineFromScenarioFile(global::DeepSpaceSaga.TestSupport.LegacyCombatCatalog.Settings(Path.Combine(ClientRoot, "Settings.json")), global::DeepSpaceSaga.TestSupport.LegacyCombatCatalog.Scenario(
            Path.Combine(ClientRoot, "Scenarios", "PlayerShipOnly", "scenario.json")));
        var snapshot = At(engine, 0);
        Assert.Equal(3, snapshot.PlayerCrewCount);
        Assert.Equal(3, snapshot.InstalledModules.Sum(m => m.CabinesCount ?? 0));
        Assert.Equal(new[] { "MOD-PLAYER-COUNTERMEASURE-01", "MOD-PLAYER-COUNTERMEASURE-02" },
            snapshot.InstalledModules.Where(m => m.Defense is not null).Select(m => m.ModuleId));
        var player = snapshot.Objects.Single(o => o.ObjectId == Player);
        var pirate = snapshot.Objects.Single(o => o.ObjectId == Target);
        Assert.Equal(-10000, pirate.X - player.X, 6);
        Assert.Equal(-5000, pirate.Y - player.Y, 6);
        var save = engine.CaptureSaveState();
        Assert.Equal(3, save.GameState.DefenseState!.Launchers.Count);
        Assert.Equal(3, save.GameState.SpaceObjects.Single(o => o.ObjectId == Player).Crew!.Count);
        Assert.Equal(2, save.GameState.SpaceObjects.Single(o => o.ObjectId == Target).Crew!.Count);
    }

    [Fact]
    public void Both_ships_start_auto_defense_enabled()
    {
        using var engine = Create(RealScenario());
        var snapshot = At(engine, 0);
        Assert.All(snapshot.Objects, o =>
        {
            Assert.True(o.Defense!.AutoEnabled);
            Assert.Equal(DefenseState.Ready, o.Defense.State);
            Assert.Equal(55m, o.Defense.Operator!.EffectiveRating);
            Assert.Equal(100, o.Defense.RangeKm);
        });
        Assert.Equal(snapshot.Objects.Single(o => o.ObjectId == Player).Defense,
            Assert.Single(snapshot.InstalledModules, m => m.ModuleId == "MOD-PLAYER-COUNTERMEASURE-01").Defense);
        Assert.Equal(2, snapshot.InstalledModules.Count(m => m.Defense is not null));
    }

    [Fact]
    public void Missing_operator_gives_no_operator_state()
    {
        var scenario = RealScenario();
        scenario = scenario with
        {
            GameState = scenario.GameState with
            {
                SpaceObjects = scenario.GameState.SpaceObjects.Select(o => o with
                { Modules = o.Modules!.Select(m => m with { OperatorCrewId = null }).ToArray() }).ToArray()
            }
        };
        using var engine = Create(scenario);
        Assert.All(At(engine, 0).Objects, o => { Assert.Equal(DefenseState.NoOperator, o.Defense!.State); Assert.Null(o.Defense.Operator); });
    }

    [Fact]
    public void Npc_defense_does_not_enable_npc_torpedo_fire()
    {
        using var engine = Create(RealScenario());
        engine.ReceiveCommand(new("npc-fire", 1, Target, "MOD-PIRATE-TORPEDO-01", CombatCommandTypes.Fire, Player));
        var snapshot = At(engine, 1000);
        Assert.DoesNotContain(snapshot.Objects, o => o.Torpedo is not null);
        Assert.Equal(CommandResultStatus.Rejected, Assert.Single(snapshot.CommandResults).Status);
    }
}
