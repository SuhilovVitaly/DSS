using System.Text.Json.Nodes;
using DeepSpaceSaga.Engine.Content;

namespace DeepSpaceSaga.Engine.Tests;

public sealed class CombatRulesContentTests : IDisposable
{
    private readonly string _directory = Path.Combine(Path.GetTempPath(), $"dss-combat-rules-{Guid.NewGuid():N}");
    private static string ClientRoot => SeededWorldBootstrapTests.ClientRoot;
    private string RulesPath => Path.Combine(_directory, "combat-rules.json");
    private string ModesPath => Path.Combine(_directory, "fire-modes.json");

    public CombatRulesContentTests()
    {
        Directory.CreateDirectory(_directory);
        File.Copy(Path.Combine(ClientRoot, "Data", "Combat", "combat-rules.json"), RulesPath);
    }

    [Fact]
    public void Rules_load()
    {
        var registry = EngineContentLoader.LoadRegistryFromSettingsFile(Path.Combine(ClientRoot, "Settings.json"), out _, out _);
        Assert.Null(registry.CombatRules!.Chance); Assert.Equal(.3, registry.CombatRules.Blast!.CoreFraction);
        Assert.Equal(8000, registry.CombatRules.Damage.HitDisableMs);
        Assert.Empty(registry.FireModes);
    }

    [Theory]
    [InlineData("scatter", "skillPivot", -1, "skillPivot")]
    [InlineData("npc", "reactionBaseMs", 0, "reactionBaseMs")]
    [InlineData("damage", "hitDisableMs", -1, "hitDisableMs")]
    [InlineData("shipDestruction", "destroyedModuleFraction", 0, "destroyedModuleFraction")]
    public void Invalid_chance_bounds_rejected(string section, string field, int value, string expected)
    {
        var root = JsonNode.Parse(File.ReadAllText(RulesPath))!;
        root[section]![field] = value;
        File.WriteAllText(RulesPath, root.ToJsonString());
        var error = Assert.Throws<ContentException>(() => EngineContentLoader.LoadCombatRules(RulesPath));
        Assert.Contains(Path.GetFileName(RulesPath), error.Message);
        Assert.Contains(expected, error.Message);
    }

    [Fact]
    public void Unknown_role_rejected()
    {
        var root = JsonNode.Parse(File.ReadAllText(RulesPath))!;
        root["shipDestruction"]!["criticalRoles"] = new JsonArray("bridge", "cargo");
        File.WriteAllText(RulesPath, root.ToJsonString());
        Assert.Contains("criticalRoles", Assert.Throws<ContentException>(
            () => EngineContentLoader.LoadCombatRules(RulesPath)).Message);
    }

    [Fact]
    public void Armor_none_must_remain_zero()
    {
        var root = JsonNode.Parse(File.ReadAllText(RulesPath))!;
        root["armorClasses"]!["none"] = 1;
        File.WriteAllText(RulesPath, root.ToJsonString());
        Assert.Contains("armorClasses", Assert.Throws<ContentException>(
            () => EngineContentLoader.LoadCombatRules(RulesPath)).Message);
    }

    [Fact]
    public void Mode_for_unknown_category_rejected()
    {
        File.WriteAllText(ModesPath, "{\"fireModes\":[]}");
        Assert.Contains("retired", Assert.Throws<ContentException>(() => EngineContentLoader.LoadFireModes(ModesPath)).Message);
    }

    [Fact]
    public void Missing_settings_key_names_file()
    {
        string settings = Path.Combine(_directory, "settings.json");
        File.WriteAllText(settings, """{"typeData":{"combatRules":"missing.json"},"defaultScenario":"unused.json"}""");
        var missingPair = Assert.Throws<ContentException>(
            () => EngineContentLoader.LoadCombatRulesAndModes(settings));
        Assert.Contains("missing.json", missingPair.Message);
        File.WriteAllText(settings, """{"typeData":{"combatRules":"missing.json","fireModes":"fire-modes.json"},"defaultScenario":"unused.json"}""");
        var missingFile = Assert.Throws<ContentException>(
            () => EngineContentLoader.LoadCombatRulesAndModes(settings));
        Assert.Contains("missing.json", missingFile.Message);
        File.WriteAllText(settings, "{\"typeData\":{\"combatRules\":\"combat-rules.json\"},\"defaultScenario\":\"unused.json\"}");
        var current = EngineContentLoader.LoadCombatRulesAndModes(settings);
        Assert.NotNull(current.Rules); Assert.Empty(current.Modes);
    }

    public void Dispose() => Directory.Delete(_directory, recursive: true);
}
