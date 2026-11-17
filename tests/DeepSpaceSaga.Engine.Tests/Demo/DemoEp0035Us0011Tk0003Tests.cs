using DeepSpaceSaga.Engine.Content;

namespace DeepSpaceSaga.Engine.Tests.Demo;

public sealed class DemoEp0035Us0011Tk0003Tests
{
    [Theory]
    [InlineData("light.sword.shield", "light", "gun.sword.t1", 1)]
    [InlineData("light.spear", "light", "gun.spear.t1", 1)]
    [InlineData("heavy.sword.shield", "heavy", "gun.sword.t1", 2)]
    [InlineData("heavy.spear.shield", "heavy", "gun.spear.t1", 2)]
    public void Layouts_LoadWithApprovedEquipment(string id, string frame, string weapon, int shields)
    {
        var registry = EngineContentLoader.LoadRegistryFromSettingsFile(DemoEp0033Us0001Tk0003Tests.Root() + "/Settings.json", out _, out _);
        var catalog = registry.TacticalBattle.Catalog!;
        var layout = catalog.Layouts[id];
        Assert.Equal(frame, layout.Class);
        Assert.Equal(weapon, layout.Weapon);
        Assert.Equal("plate.medium", layout.Armor);
        Assert.Equal("shield.t1", layout.Shield);
        Assert.Equal(shields, layout.ShieldCount ?? 1);
        Assert.Equal(new[] { "drive.standard", "drive.standard" }, layout.Engines);
        Assert.Equal("decoy.t1", layout.Module);
        Assert.True(catalog.Frames.Contains(frame));
        Assert.True(catalog.Scenarios.Contains("initial"));
        Assert.True(catalog.Layouts.Contains("thug.scrap"));
    }
}
