using DeepSpaceSaga.Client.UI;
using DeepSpaceSaga.Client.UI.Screens.GameSession;
using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Motion;
using SkiaSharp;

namespace DeepSpaceSaga.Client.Tests;

public class TacticalPaletteRoutingTests
{
    [Fact]
    public void PaletteRenderRoleRouting_uses_custom_settings_and_distinct_range_roles()
    {
        var settings = CombatVisualSettings.Default with
        {
            Torpedo = new(31, 61, 91),
            TorpedoRange = new(120, 90, 60, 46),
            DefenseRange = new(60, 120, 90, 46),
            ManualDefenseRange = new(60, 120, 90, 71)
        };
        Assert.Equal(settings.TorpedoRange, settings.RangeColor("torpedo"));
        Assert.Equal(settings.DefenseRange, settings.RangeColor("auto"));
        Assert.Equal(settings.ManualDefenseRange, settings.RangeColor("manual"));
    }
}
