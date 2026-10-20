using System.Text.Json;
using DeepSpaceSaga.Contracts;

namespace DeepSpaceSaga.Contracts.Tests;

public class PointCombatUS0001TK0005ContractTests
{
    [Fact]
    public void Launcher_profiles_roundtrip_captured_catalogue_values()
    {
        var launcher = new LauncherCombatSnapshot(6, 0, 200, RangeKm: 70,
            Profiles: [new("slow", 6, 9), new("medium", 10, 5), new("fast", 16, 2.5)], ScatterPercent: 0.02);
        var restored = JsonSerializer.Deserialize<LauncherCombatSnapshot>(JsonSerializer.Serialize(launcher))!;
        Assert.Equal(launcher.Profiles!.Value.ToArray(), restored.Profiles!.Value.ToArray());
        Assert.Equal(0.02, restored.ScatterPercent);
        Assert.Equal(70, restored.RangeKm);
    }

    [Fact]
    public void Countermissile_profiles_have_no_ship_damage()
    {
        var defense = new DefenseSnapshot(false, null, DefenseState.Ready, RangeKm: 30,
            Profiles: [new("wide", 15, 1), new("fast", 25, 0.5)], ScatterPercent: 0.08);
        var restored = JsonSerializer.Deserialize<DefenseSnapshot>(JsonSerializer.Serialize(defense))!;
        Assert.Equal(defense.Profiles!.Value.ToArray(), restored.Profiles!.Value.ToArray());
        Assert.Equal(0.08, restored.ScatterPercent);
        Assert.DoesNotContain(typeof(DefenseSnapshot).GetProperties(), property => property.Name is "Damage" or "ArmorPiercingPercent");
    }

    [Fact]
    public void Legacy_launchers_do_not_invent_profiles_or_scatter()
    {
        var launcher = JsonSerializer.Deserialize<LauncherCombatSnapshot>("""{"SpeedKmS":3,"TurnRateDegPerSec":90,"Damage":150}""")!;
        var defense = JsonSerializer.Deserialize<DefenseSnapshot>("""{"AutoEnabled":true,"Operator":null,"State":0}""")!;
        Assert.Null(launcher.Profiles);
        Assert.Null(defense.Profiles);
        Assert.Null(launcher.ScatterPercent);
        Assert.Null(defense.ScatterPercent);
    }
}
