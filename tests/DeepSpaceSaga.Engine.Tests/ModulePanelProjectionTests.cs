using System.Text.Json;
using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Engine.Content;
using DeepSpaceSaga.Engine.Scenario;

namespace DeepSpaceSaga.Engine.Tests;

public sealed class ModulePanelProjectionTests
{
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
