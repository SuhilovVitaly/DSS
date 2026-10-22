using System.Text.Json;
using DeepSpaceSaga.Contracts;

namespace DeepSpaceSaga.Contracts.Tests;

public sealed class ModulePanelContractTests
{
    [Fact]
    public void Panel_values_roundtrip_without_losing_fractional_penetration()
    {
        var launcher = new LauncherCombatSnapshot(6, 0, 200, ArmorPiercingPercent: 20.125m);
        var copy = JsonSerializer.Deserialize<LauncherCombatSnapshot>(JsonSerializer.Serialize(launcher))!;
        Assert.Equal(20.125m, copy.ArmorPiercingPercent);
        var snapshot = new AuthoritativeSnapshot(1, 0, SimulationSpeed.Speed0, [],
            PlayerShipObjectId: "own", DestroyedModuleLimit: 3);
        var restored = JsonSerializer.Deserialize<AuthoritativeSnapshot>(JsonSerializer.Serialize(snapshot))!;
        Assert.Equal("own", restored.PlayerShipObjectId);
        Assert.Equal(3, restored.DestroyedModuleLimit);
    }

    [Fact]
    public void Legacy_producers_do_not_invent_panel_values()
    {
        var launcher = JsonSerializer.Deserialize<LauncherCombatSnapshot>("""{"SpeedKmS":6,"TurnRateDegPerSec":0,"Damage":200}""")!;
        Assert.Null(launcher.ArmorPiercingPercent);
        Assert.DoesNotContain("ArmorPiercingPercent", JsonSerializer.Serialize(launcher));
        var snapshot = new AuthoritativeSnapshot(1, 0, SimulationSpeed.Speed0, []);
        Assert.Null(JsonSerializer.Deserialize<AuthoritativeSnapshot>(JsonSerializer.Serialize(snapshot))!.DestroyedModuleLimit);
        Assert.DoesNotContain("DestroyedModuleLimit", JsonSerializer.Serialize(snapshot));
    }
}
