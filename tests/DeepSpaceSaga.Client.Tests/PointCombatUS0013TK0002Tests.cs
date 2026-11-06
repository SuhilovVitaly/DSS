using DeepSpaceSaga.Client.UI.Screens.GameSession;
using DeepSpaceSaga.Client.UI.Screens.GameSession.Controls;
using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Engine.Content;
using DeepSpaceSaga.Motion;
using System.Diagnostics;
using Silk.NET.Input;
using SkiaSharp;

namespace DeepSpaceSaga.Client.Tests;

public sealed class PointCombatUS0013TK0002Tests
{

    [Fact]
    public async Task Counter_effective_scatter_is_authoritative_and_foreign_projection_stays_hidden()
    {
        var source = PointCombatSessionFixture.Registry();
        var rules = source.CombatRules! with { Scatter = new(30, .01, .3) };
        static IEnumerable<T> All<T>(TypeRegistry<T> registry) where T : ITypeDefinition => Enumerable.Range(0, registry.Count).Select(registry.GetDefinition);
        var registry = GameDataRegistry.Create(All(source.ModuleCategories), All(source.ModuleTypes).Select(m =>
                m.CategoryTypeId == "module.countermeasure.launcher" ? m with { ScatterPercent = .08 } : m),
            All(source.ItemTypes), All(source.CommandDefinitions), All(source.FactoryTypes), All(source.Recipes), All(source.Dialogues), All(source.Quests),
            stationMarketProfiles: All(source.StationMarketProfiles), shipClasses: All(source.ShipClasses), stationMarketEvents: All(source.StationMarketEvents), combatRules: rules);
        await using var world = new PointCombatSessionFixture(registry);
        var snapshot = world.Engine.CaptureSnapshot();
        var counters = snapshot.InstalledModules.Where(m => m.Defense is not null).ToArray();
        Assert.NotEmpty(counters);
        foreach (var module in counters)
        {
            var counter = module.Defense!;
            Assert.Equal(0, counter.Operator!.Skill);
            Assert.Equal(.08, counter.ScatterPercent);
            Assert.Equal(.104, counter.EffectiveScatterPercent!.Value, 9);
        }
        foreach (var foreign in snapshot.Objects.Where(o => o.ObjectId != snapshot.PlayerShipObjectId))
            Assert.Null(foreign.Defense);
    }
}
