using System.Text.Json.Nodes;
using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Engine.Content;

namespace DeepSpaceSaga.Client.Tests;

public sealed class PointCombatUS0002TK0004Tests
{
    private static IEnumerable<T> All<T>(TypeRegistry<T> source) where T : ITypeDefinition => Enumerable.Range(0, source.Count).Select(source.GetDefinition);
    private static bool Weapon(string category) => category is "module.torpedo.launcher" or "module.countermeasure.launcher";

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Command_catalog_exposes_launch_and_detonate_only_with_matching_categories(bool deployed)
    {
        string source = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "src", "DeepSpaceSaga.Client"));
        string root = deployed ? AppContext.BaseDirectory : source;
        bool staged = File.Exists(Path.Combine(source, "Data/CombatV3/module-types.json"));
        string prefix = staged ? "Data/CombatV3/" : "Data/";
        foreach (string relative in new[] { "module-types.json", "Commands/TorpedoLauncher/commands.json", "Commands/CountermeasureLauncher/commands.json" })
            Assert.True(JsonNode.DeepEquals(JsonNode.Parse(File.ReadAllText(Path.Combine(source, prefix + relative))),
                JsonNode.Parse(File.ReadAllText(Path.Combine(root, prefix + relative)))));
        var active = EngineContentLoader.LoadRegistryFromSettingsFile(Path.Combine(root, "Settings.json"), out _, out _);
        var categories = EngineContentLoader.LoadModuleCategories(Path.Combine(root, prefix + "module-types.json"));
        var definitions = EngineContentLoader.LoadCommandDefinitions(Path.Combine(root, prefix + "Commands")).Where(c => Weapon(c.Type)).ToArray();
        Assert.Equal(new[] { DefenseCommandTypes.Launch, CombatCommandTypes.Detonate, CombatCommandTypes.Launch },
            definitions.Select(c => c.TypeId).Order(StringComparer.Ordinal));
        foreach (var definition in definitions)
        {
            Assert.Equal(definition.TypeId == CombatCommandTypes.Detonate ? "object" : "point", definition.Target);
            Assert.Equal(0, definition.ActivationEnergyCellsCost);
            Assert.Equal((CommandDefinition.Neutral, CommandDefinition.Neutral, CommandDefinition.Neutral),
                (definition.TimeFactor, definition.ComplexityFactor, definition.ConsumptionFactor));
            Assert.Null(definition.RangeKm); Assert.Null(definition.TrailDistanceKm); Assert.False(string.IsNullOrWhiteSpace(definition.DisplayName));
        }
        Assert.Equal(new[] { CombatCommandTypes.Launch, CombatCommandTypes.Detonate },
            categories.Single(c => c.TypeId == "module.torpedo.launcher").CommandTypeIds);
        Assert.Equal(new[] { DefenseCommandTypes.Launch }, categories.Single(c => c.TypeId == "module.countermeasure.launcher").CommandTypeIds);
        foreach (var category in categories.Where(c => !Weapon(c.TypeId)))
        {
            var original = active.ModuleCategories.GetDefinition(active.ModuleCategories.GetIndex(category.TypeId));
            Assert.Equal((original.TypeId, original.DisplayName, original.SlotSize), (category.TypeId, category.DisplayName, category.SlotSize));
            Assert.Equal<string>(original.CommandTypeIds, category.CommandTypeIds);
        }
        // Validate the committed components together through the real registry, without activating default Settings yet.
        var replacements = EngineContentLoader.LoadModuleImplementations(Path.Combine(root, prefix + "Modules"), categories);
        var replacementIds = replacements.Select(m => m.TypeId).ToHashSet(StringComparer.Ordinal);
        var registry = GameDataRegistry.Create(categories, All(active.ModuleTypes).Where(m => !replacementIds.Contains(m.TypeId)).Concat(replacements),
            All(active.ItemTypes), All(active.CommandDefinitions).Where(c => !Weapon(c.Type)).Concat(definitions),
            All(active.FactoryTypes), All(active.Recipes), All(active.Dialogues), All(active.Quests),
            stationMarketProfiles: All(active.StationMarketProfiles), shipClasses: All(active.ShipClasses), stationMarketEvents: All(active.StationMarketEvents),
            combatRules: active.CombatRules, fireModes: active.FireModes);
        foreach (string id in new[] { "torpedo.fire", "torpedo.selfDestruct", "torpedo.setMode", "defense.fire", "defense.enable", "defense.disable" })
            Assert.False(registry.CommandDefinitions.Contains(id));
        foreach (var module in All(registry.ModuleTypes).Where(m => Weapon(m.CategoryTypeId!)))
            Assert.All(module.CommandTypeIds, id => Assert.True(registry.CommandDefinitions.Contains(id)));
        Assert.Equal(!staged, active.CommandDefinitions.Contains(CombatCommandTypes.Launch));
        using var engine = EngineContentLoader.CreateEngineFromSettingsFile(Path.Combine(root, "Settings.json")); Assert.NotNull(engine.PlayerShipObjectId);
    }
}
