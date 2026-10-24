using System.Text.Json;
using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Engine.Content;
using DeepSpaceSaga.Engine.Scenario;

namespace DeepSpaceSaga.Engine.Tests;

public sealed class ModulePanelProjectionTests
{
    [Theory]
    [InlineData(19, false)]
    [InlineData(19, true)]
    [InlineData(18, false)]
    public void Real_save_file_preserves_both_flights_deadlines_rng_tuning_and_atomic_rejection(int format, bool tuning)
    {
        using var engine = PointCounterFixture.Create();
        if (tuning)
        {
            engine.ReceiveCommand(new("panel-tuning", 1, "", "", DebugTuningCommandTypes.Apply,
                DebugTuning: new([new("module.engine.tetrarch", "maxSpeedKmS", 6.5)])));
            PointCounterFixture.At(engine);
        }
        var torpedo = new PlayerCommand("panel-torpedo", 1, PointCounterFixture.Player, PointCounterFixture.Torpedo,
            CombatCommandTypes.Launch, TargetWorldX: 700, TargetWorldY: 0, ProfileId: "medium");
        engine.ReceiveCommand(torpedo); PointCounterFixture.At(engine);
        engine.ReceiveCommand(PointCounterFixture.Shot("panel-counter", x: 250, y: 0)); PointCounterFixture.At(engine);
        var mid = PointCounterFixture.At(engine, 500);
        Assert.Equal(2, mid.Objects.Count(o => o.OwnProjectile is not null));
        var save = PointCounterFixture.Save(engine, 500);
        if (format == 18)
            save = save with
            {
                SaveFormatVersion = 18,
                GameState = save.GameState with
                {
                    PointCombatState = save.GameState.PointCombatState! with
                    { Projectiles = save.GameState.PointCombatState.Projectiles.Select(p => p with { LauncherModuleTypeId = null, OriginalFlight = null }).ToArray() }
                }
            };
        string directory = Path.Combine("D:/DeepSpaceSaga/artifacts/validation/ep-0014-module-panels/20261022-execution", "continuation-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        string path = Path.Combine(directory, "midflight.json");
        try
        {
            File.WriteAllText(path, ScenarioLoader.Serialize(save));
            var loaded = ScenarioLoader.LoadFromFile(path, true, true);
            Assert.Equal(19, loaded.SaveFormatVersion);
            using var restored = new SimulationEngine(PointCounterFixture.Registry());
            restored.LoadScenario(loaded, true);
            var admitted = PointCounterFixture.At(restored, 500);
            Assert.Equal(3, admitted.DestroyedModuleLimit);
            Assert.Equal(JsonSerializer.Serialize(mid.Objects.Where(o => o.OwnProjectile is not null).Select(o => o.OwnProjectile)),
                JsonSerializer.Serialize(admitted.Objects.Where(o => o.OwnProjectile is not null).Select(o => o.OwnProjectile)));
            Assert.Equal(engine.CaptureLaunchScatterState(), restored.CaptureLaunchScatterState());
            Assert.Equal(tuning, restored.CaptureSaveState().GameState.DebugTuning is not null);
            restored.ReceiveCommand(torpedo); PointCounterFixture.At(restored, 500);
            Assert.Equal(engine.CaptureLaunchScatterState(), restored.CaptureLaunchScatterState());
            Assert.Equal(2, PointCounterFixture.At(restored, 500).Objects.Count(o => o.OwnProjectile is not null));
            string beforeFailure = ScenarioLoader.Serialize(PointCounterFixture.Save(restored, 500));
            var corrupt = loaded with
            {
                GameState = loaded.GameState with
                {
                    CombatState = loaded.GameState.CombatState! with
                    { Launchers = loaded.GameState.CombatState.Launchers.Select(l => l with { State = l.State with { ModeId = "retired" } }).ToArray() }
                }
            };
            Assert.Throws<ScenarioException>(() => restored.LoadScenario(corrupt, true));
            Assert.Equal(beforeFailure, ScenarioLoader.Serialize(PointCounterFixture.Save(restored, 500)));
            PointCounterFixture.At(engine, 15000); PointCounterFixture.At(restored, 15000);
            var expected = PointCounterFixture.Save(engine, 15000).GameState;
            var actual = PointCounterFixture.Save(restored, 15000).GameState;
            Assert.Equal(JsonSerializer.Serialize(expected.CombatState), JsonSerializer.Serialize(actual.CombatState));
            Assert.Equal(JsonSerializer.Serialize(expected.DefenseState), JsonSerializer.Serialize(actual.DefenseState));
            Assert.Equal(JsonSerializer.Serialize(expected.PointCombatState), JsonSerializer.Serialize(actual.PointCombatState));
            Assert.Equal(JsonSerializer.Serialize(expected.DebugTuning), JsonSerializer.Serialize(actual.DebugTuning));
            Assert.Equal(engine.CaptureLaunchScatterState(), restored.CaptureLaunchScatterState());
        }
        finally { File.Delete(path); Directory.Delete(directory); }
    }

    [Theory]
    [InlineData(9, 3)]
    [InlineData(6, 2)]
    public void Own_threshold_uses_actual_module_count(int count, int expected)
    {
        using var engine = PointCounterFixture.Create(configure: o => o.ObjectId != PointCounterFixture.Player ? o : o with
        {
            Modules = o.Modules!.Where(m => count == 9 || m.ModuleId is not
                ("MOD-PLAYER-SCANNER-01" or "MOD-PLAYER-GENERATOR-01" or "MOD-PLAYER-LIVING-QUARTERS-01")).ToArray()
        });
        var snapshot = PointCounterFixture.At(engine);
        Assert.Equal(count, snapshot.InstalledModules.Length);
        Assert.Equal(expected, snapshot.DestroyedModuleLimit);
        Assert.All(snapshot.Objects, o => Assert.DoesNotContain("DestroyedModuleLimit", JsonSerializer.Serialize(o)));
        Assert.All(snapshot.InstalledModules.Where(m => m.LauncherCombat is not null),
            m => Assert.Equal(20m, m.LauncherCombat!.ArmorPiercingPercent));
    }

    [Fact]
    public void Projection_preserves_fractional_catalog_penetration()
    {
        var source = PointCounterFixture.Registry();
        static IEnumerable<T> All<T>(TypeRegistry<T> registry) where T : ITypeDefinition =>
            Enumerable.Range(0, registry.Count).Select(registry.GetDefinition);
        var registry = GameDataRegistry.Create(All(source.ModuleCategories),
            All(source.ModuleTypes).Select(m => m.TorpedoDamage is null ? m : m with { ArmorPiercingPercent = 20.125m }),
            All(source.ItemTypes), All(source.CommandDefinitions), All(source.FactoryTypes), All(source.Recipes),
            All(source.Dialogues), All(source.Quests), stationMarketProfiles: All(source.StationMarketProfiles),
            shipClasses: All(source.ShipClasses), stationMarketEvents: All(source.StationMarketEvents), combatRules: source.CombatRules);
        using var engine = new SimulationEngine(registry);
        engine.LoadScenario(ScenarioLoader.LoadFromFile(Path.Combine(PointCounterFixture.Root, "Scenarios/PlayerShipOnly/scenario.json")));
        Assert.All(PointCounterFixture.At(engine).InstalledModules.Where(m => m.LauncherCombat is not null),
            m => Assert.Equal(20.125m, m.LauncherCombat!.ArmorPiercingPercent));
    }
}
