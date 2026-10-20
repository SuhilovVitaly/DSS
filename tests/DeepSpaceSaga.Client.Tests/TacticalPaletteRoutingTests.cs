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
        var screen = new GameSessionScreen(new SnapshotBuffer(), new LinearMotionPredictor(), combatSettings: settings);
        Assert.Equal(new SKColor(31, 61, 91, 77), screen.CombatTargetPathColor(new("torpedo", 0, 0, 0, 0,
            RenderObjectType: SpaceObjectType.Missile)));
        Assert.Equal(new SKColor(240, 128, 136, 77), screen.CombatTargetPathColor(new("enemy", 0, 0, 0, 0,
            RenderObjectType: SpaceObjectType.NpcShip, RelationToPlayer: PlayerRelation.Enemy)));
        Assert.Equal(new SKColor(165, 175, 196, 77), screen.CombatTargetPathColor(new("unknown", 0, 0, 0, 0,
            RenderObjectType: SpaceObjectType.UnknownSpaceObject, RelationToPlayer: PlayerRelation.Enemy)));
        Assert.Equal(new SKColor(118, 213, 220, 26), screen.CountermeasureTrailColor(0));
        Assert.Equal(new SKColor(118, 213, 220, 102), screen.CountermeasureTrailColor(1));
        screen.OnDeactivated();
    }
}
