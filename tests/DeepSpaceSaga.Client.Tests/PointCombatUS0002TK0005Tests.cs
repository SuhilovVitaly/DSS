using System.Text.Json.Nodes;
using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Engine;
using DeepSpaceSaga.Engine.Content;
using DeepSpaceSaga.Engine.LocalClient;
using DeepSpaceSaga.Engine.Scenario;

namespace DeepSpaceSaga.Client.Tests;

public sealed class PointCombatUS0002TK0005Tests
{
    private static readonly string Source = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "src", "DeepSpaceSaga.Client"));

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Production_rules_match_decisions_and_obsolete_content_is_absent(bool deployed)
    {
        string root = deployed ? AppContext.BaseDirectory : Source;
        var settings = JsonNode.Parse(File.ReadAllText(Path.Combine(root, "Settings.json")))!;
        Assert.False(settings["typeData"]!.AsObject().ContainsKey("fireModes"));
        Assert.False(File.Exists(Path.Combine(root, "Data/Combat/fire-modes.json")));
        Assert.Empty(Directory.Exists(Path.Combine(root, "Data/CombatV3")) ? Directory.GetFiles(Path.Combine(root, "Data/CombatV3"), "*.json", SearchOption.AllDirectories) : []);
        var registry = EngineContentLoader.LoadRegistryFromSettingsFile(Path.Combine(root, "Settings.json"), out _, out _);
        var rules = Assert.IsType<CombatRulesDefinition>(registry.CombatRules);
        Assert.True(rules.IsPointAim); Assert.Null(rules.Chance); Assert.Empty(registry.FireModes);
        Assert.Equal(.3, rules.Blast!.CoreFraction);
        Assert.Equal(8000, rules.Damage.HitDisableMs); Assert.Equal("mass", rules.Damage.HitLocationWeighting);
        Assert.Equal((15, .0333, .3), (rules.Scatter!.SkillPivot, rules.Scatter.SkillSlope, rules.Scatter.MinMultiplier));
        Assert.Equal((1400L, 30L, 200L), (rules.Npc!.ReactionBaseMs, rules.Npc.ReactionPerSkillMs, rules.Npc.ReactionMinMs));
        Assert.Equal(100, rules.CombatStartRangeKm);
        Assert.Equal(.3333m, rules.ShipDestruction.DestroyedModuleFraction);
        Assert.Equal(new[] { "bridge", "engine" }, rules.ShipDestruction.CriticalRoles);
        Assert.Equal(0, rules.ArmorClasses["none"]); Assert.Equal(60, rules.ArmorClasses["light"]);
        foreach (string id in new[] { "torpedo.fire", "torpedo.selfDestruct", "torpedo.setMode", "defense.fire", "defense.enable", "defense.disable" })
            Assert.False(registry.CommandDefinitions.Contains(id));
        foreach (string id in new[] { CombatCommandTypes.Launch, CombatCommandTypes.Detonate, DefenseCommandTypes.Launch })
            Assert.True(registry.CommandDefinitions.Contains(id));
        Assert.True(JsonNode.DeepEquals(JsonNode.Parse(File.ReadAllText(Path.Combine(Source, "Data/Combat/combat-rules.json"))),
            JsonNode.Parse(File.ReadAllText(Path.Combine(root, "Data/Combat/combat-rules.json")))));
    }
}
