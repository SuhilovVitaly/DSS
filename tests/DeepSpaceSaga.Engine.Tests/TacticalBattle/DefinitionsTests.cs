using System.Text.Json;
using DeepSpaceSaga.Engine.TacticalBattle.Content;

namespace DeepSpaceSaga.Engine.Tests.TacticalBattle;

public sealed class DefinitionsTests
{
    [Fact]
    public void PilotInitiativeIsIndependentFromAccuracyAndManeuver()
    {
        var pilot = new BattlePilot("Pilot", PilotSkill.Fixed(70), PilotSkill.Fixed(65));
        Assert.Equal(PilotSkill.Fixed(50), pilot.Initiative);
        var changed = pilot with { Initiative = PilotSkill.Range(50, 50) };
        Assert.Equal(pilot.Accuracy, changed.Accuracy);
        Assert.Equal(pilot.Maneuver, changed.Maneuver);
        Assert.True(changed.Initiative.IsRange);
        Assert.NotEqual(pilot.Initiative, changed.Initiative);
    }

    [Fact]
    public void CatalogCopiesInputAndKeepsJsonOrderWithOrdinalIdentity()
    {
        (string Id, string Value)[] input = [("second", "two"), ("First", "one"), ("first", "lowercase")];
        var catalog = new OrderedBattleCatalog<string>(input);
        input[0] = ("changed", "changed");
        Assert.Equal(new[] { "second", "First", "first" }, catalog.Entries.Select(e => e.Id));
        Assert.Equal("two", catalog["second"]);
        Assert.Equal("one", catalog["First"]);
        Assert.Equal("lowercase", catalog["first"]);
        Assert.False(catalog.Contains("FIRST"));
        Assert.Throws<ArgumentException>(() => new OrderedBattleCatalog<string>([("x", "a"), ("x", "b")]));
    }

    [Fact]
    public void FixedAndDegenerateRangeAreDistinctForRandomDrawOwnership()
    {
        var fixedSkill = PilotSkill.Fixed(50);
        var rangeSkill = PilotSkill.Range(50, 50);
        Assert.Equal(fixedSkill.Minimum, rangeSkill.Minimum);
        Assert.Equal(fixedSkill.Maximum, rangeSkill.Maximum);
        Assert.False(fixedSkill.IsRange);
        Assert.True(rangeSkill.IsRange);
        Assert.NotEqual(fixedSkill, rangeSkill);
    }

    [Fact]
    public void WeaponDefinitionReadsProductionFixtureAndPreservesOptionalRangeBonus()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "DeepSpaceSaga.sln"))) directory = directory.Parent;
        Assert.NotNull(directory);
        string json = File.ReadAllText(Path.Combine(directory.FullName,
            "src/DeepSpaceSaga.Client/Data/TacticalBattle/Modules/Weapons/gun.spear.t1.json"));
        var options = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
        var weapon = JsonSerializer.Deserialize<BattleWeapon>(json, options)!;
        Assert.Equal(new BattleWeapon("gun.spear.t1", "Копьё I", 1, 45, 135, 40, 60, 0, 10, .6, 65), weapon);
        Assert.Null(weapon.RangeBonus);
        Assert.Equal(12.5, (weapon with { RangeBonus = 12.5 }).RangeBonus);
        Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<BattleWeapon>(json.Replace("\"damageMin\": 40", "\"damageMin\": 40.0"), options));
    }
}
