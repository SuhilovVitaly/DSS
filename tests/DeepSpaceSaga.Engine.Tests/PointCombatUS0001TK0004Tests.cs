using System.Text.Json.Nodes;
using DeepSpaceSaga.Engine.Content;

namespace DeepSpaceSaga.Engine.Tests;

public sealed class PointCombatUS0001TK0004Tests : IDisposable
{
    private const string RulesJson = """
        {
          "damage":{"hitDisableMs":8000,"hitLocationWeighting":"mass"},
          "shipDestruction":{"criticalRoles":["bridge","engine"],"destroyedModuleFraction":0.3333},
          "armorClasses":{"none":0,"light":60},
          "blast":{"coreFraction":0.3},
          "scatter":{"skillPivot":15,"skillSlope":0.0333,"minMultiplier":0.3},
          "npc":{"reactionBaseMs":1400,"reactionPerSkillMs":30,"reactionMinMs":200},
          "combatStartRangeKm":100
        }
        """;
    private readonly string _directory = Path.Combine(Path.GetTempPath(), $"dss-point-rules-{Guid.NewGuid():N}");
    private string RulesPath => Path.Combine(_directory, "point-combat-rules.json");
    private string SettingsPath => Path.Combine(_directory, "settings.json");

    public PointCombatUS0001TK0004Tests()
    {
        Directory.CreateDirectory(_directory);
        File.WriteAllText(RulesPath, RulesJson);
        File.WriteAllText(SettingsPath, """{"typeData":{"combatRules":"point-combat-rules.json"},"defaultScenario":"unused.json"}""");
    }

    [Theory]
    [InlineData("blast", "coreFraction", 0)]
    [InlineData("blast", "coreFraction", 1)]
    [InlineData("damage", "hitDisableMs", -1)]
    [InlineData("scatter", "skillPivot", -1)]
    [InlineData("scatter", "skillPivot", 31)]
    [InlineData("scatter", "skillSlope", -0.01)]
    [InlineData("scatter", "minMultiplier", 0)]
    [InlineData("scatter", "minMultiplier", 1.01)]
    [InlineData("npc", "reactionBaseMs", 0)]
    [InlineData("npc", "reactionPerSkillMs", -1)]
    [InlineData("npc", "reactionMinMs", -1)]
    [InlineData("npc", "reactionMinMs", 1401)]
    [InlineData("", "combatStartRangeKm", 0)]
    public void Rules_reject_invalid_blast_scatter_reaction_and_disable_fields(string section, string field, double value)
    {
        var json = JsonNode.Parse(RulesJson)!;
        (section.Length == 0 ? json : json[section]!)[field] = value;
        File.WriteAllText(RulesPath, json.ToJsonString());

        var error = Assert.Throws<ContentException>(() => EngineContentLoader.LoadCombatRules(RulesPath));

        Assert.Contains(Path.GetFileName(RulesPath), error.Message);
        Assert.Contains(field, error.Message);
    }

    [Fact]
    public void Rules_load_without_fire_modes()
    {
        var (rules, modes) = EngineContentLoader.LoadCombatRulesAndModes(SettingsPath);
        Assert.NotNull(rules);
        Assert.True(rules.IsPointAim);
        Assert.Null(rules.Chance);
        Assert.Empty(modes);
        Assert.Equal(0.3, rules.Blast!.CoreFraction);
        Assert.Equal(15, rules.Scatter!.SkillPivot);
        Assert.Equal(0.0333, rules.Scatter.SkillSlope);
        Assert.Equal(0.3, rules.Scatter.MinMultiplier);
        Assert.Equal(1400, rules.Npc!.ReactionBaseMs);
        Assert.Equal(30, rules.Npc.ReactionPerSkillMs);
        Assert.Equal(200, rules.Npc.ReactionMinMs);
        Assert.Equal(100, rules.CombatStartRangeKm);
        Assert.Equal(8000, rules.Damage.HitDisableMs);
        Assert.Equal("mass", rules.Damage.HitLocationWeighting);
        Assert.Equal(0, rules.ArmorClasses["none"]);
        Assert.Equal(60, rules.ArmorClasses["light"]);
        Assert.Equal(0.3333m, rules.ShipDestruction.DestroyedModuleFraction);
        Assert.Equal(["bridge", "engine"], rules.ShipDestruction.CriticalRoles.ToArray());
        Assert.True(GameDataRegistry.Create([], [], [], [], combatRules: rules).CombatRules!.IsPointAim);
    }

    [Theory]
    [InlineData("blast")]
    [InlineData("scatter")]
    [InlineData("npc")]
    [InlineData("combatStartRangeKm")]
    public void Point_rules_require_the_complete_new_schema(string field)
    {
        var json = JsonNode.Parse(RulesJson)!.AsObject();
        json.Remove(field);
        File.WriteAllText(RulesPath, json.ToJsonString());
        var error = Assert.Throws<ContentException>(() => EngineContentLoader.LoadCombatRules(RulesPath));
        Assert.Contains(RulesPath, error.Message);
        Assert.Contains(field, error.Message);
    }

    [Theory]
    [InlineData("NaN")]
    [InlineData("Infinity")]
    [InlineData("\"NaN\"")]
    [InlineData("1e309")]
    public void Rules_reject_nonfinite_numbers_with_file_and_field(string number)
    {
        File.WriteAllText(RulesPath, RulesJson.Replace("0.0333", number, StringComparison.Ordinal));
        var error = Assert.Throws<ContentException>(() => EngineContentLoader.LoadCombatRules(RulesPath));
        Assert.Contains(RulesPath, error.Message);
        Assert.Contains("skillSlope", error.Message);
    }

    [Fact]
    public void Point_rules_reject_mixed_legacy_chance_and_modes()
    {
        var json = JsonNode.Parse(RulesJson)!.AsObject();
        json["chance"] = JsonNode.Parse("""{"base":50,"min":0,"max":100}""");
        File.WriteAllText(RulesPath, json.ToJsonString());
        Assert.Contains("chance", Assert.Throws<ContentException>(() => EngineContentLoader.LoadCombatRules(RulesPath)).Message);

        File.WriteAllText(RulesPath, RulesJson);
        File.WriteAllText(SettingsPath, """{"typeData":{"combatRules":"point-combat-rules.json","fireModes":"stale.json"},"defaultScenario":"unused.json"}""");
        var error = Assert.Throws<ContentException>(() => EngineContentLoader.LoadCombatRulesAndModes(SettingsPath));
        Assert.Contains(SettingsPath, error.Message);
        Assert.Contains("typeData.fireModes", error.Message);

        var rules = EngineContentLoader.LoadCombatRules(RulesPath);
        var mode = new FireModeDefinition("standard", "Standard", 0, 0, 0, 1,
            ["module.torpedo.launcher", "module.countermeasure.launcher"]);
        Assert.Contains("fireModes", Assert.Throws<ContentException>(() =>
            GameDataRegistry.Create([], [], [], [], combatRules: rules, fireModes: [mode])).Message);
    }

    [Fact]
    public void Rules_reject_unknown_fields()
    {
        var json = JsonNode.Parse(RulesJson)!.AsObject();
        json["unexpected"] = 1;
        File.WriteAllText(RulesPath, json.ToJsonString());
        var error = Assert.Throws<ContentException>(() => EngineContentLoader.LoadCombatRules(RulesPath));
        Assert.Contains(RulesPath, error.Message);
        Assert.Contains("unexpected", error.Message);
    }

    public void Dispose() => Directory.Delete(_directory, recursive: true);
}
