using DeepSpaceSaga.Client.UI.Screens.GameSession.Controls;
using DeepSpaceSaga.Contracts;
using SkiaSharp;
using static DeepSpaceSaga.Client.Tests.TorpedoCommandPanelTests;

namespace DeepSpaceSaga.Client.Tests;

public class HostileSelectedInfoTests
{
    private static HostileCombatSnapshot State(HostileCombatPhase phase) => new("player", "engine", "weapon", 37.25,
        false, phase, new(0, 100, 0, 10000, 10060, 0, 0, 0, 4));

    [Theory]
    [InlineData(HostileCombatPhase.Pursuit, "Сближение")]
    [InlineData(HostileCombatPhase.OrbitEntry, "Выход на орбиту")]
    [InlineData(HostileCombatPhase.OrbitHold, "Удержание орбиты")]
    public async Task Selected_known_enemy_shows_confirmed_phase_and_configured_radius(HostileCombatPhase phase, string label)
    {
        await using var fixture = new Fixture();
        SelectTarget(fixture);
        fixture.Update(s => s with
        {
            Objects = s.Objects.SetItem(1, s.Objects[1] with
            { ObjectType = SpaceObjectType.NpcShip, RenderObjectType = SpaceObjectType.NpcShip, RelationToPlayer = PlayerRelation.Enemy, HostileCombat = State(phase) })
        });
        Render(fixture.Screen);
        var data = fixture.Screen.SelectedOrActiveObjectInfo;
        var lines = ObjectInfoPanel.BuildLines(data);
        Assert.Contains(("Манёвр", label), lines);
        Assert.Contains(("Радиус орбиты", $"{37.25:0.##} км"), lines);
        Assert.DoesNotContain(ObjectInfoPanel.BuildLines(fixture.Screen.PlayerShipInfo), line => line.Label == "Манёвр");
        Render(fixture.Screen);
        Assert.Equal(data, fixture.Screen.SelectedOrActiveObjectInfo);
        fixture.Update(s => s with
        {
            Objects = s.Objects.SetItem(1, s.Objects[1] with
            { HostileCombat = State(HostileCombatPhase.Pursuit) with { DesiredRadiusKm = 80 } })
        });
        Render(fixture.Screen);
        Assert.Contains(("Манёвр", "Сближение"), ObjectInfoPanel.BuildLines(fixture.Screen.SelectedOrActiveObjectInfo));
        Assert.Contains(("Радиус орбиты", "80 км"), ObjectInfoPanel.BuildLines(fixture.Screen.SelectedOrActiveObjectInfo));
        fixture.Screen.OnMouseDown(740, 420);
        Render(fixture.Screen);
        Assert.Equal("hover", fixture.Screen.SelectedObjectId);
        Assert.DoesNotContain(ObjectInfoPanel.BuildLines(fixture.Screen.SelectedOrActiveObjectInfo), line => line.Label == "Манёвр");
    }

    [Theory]
    [InlineData(PlayerRelation.Neutral, SpaceObjectType.NpcShip)]
    [InlineData(PlayerRelation.Friend, SpaceObjectType.NpcShip)]
    [InlineData(null, SpaceObjectType.UnknownSpaceObject)]
    [InlineData(PlayerRelation.Enemy, SpaceObjectType.PlayerShip)]
    public async Task Non_enemy_or_unknown_rows_do_not_reveal_hostile_state(string? relation, string renderType)
    {
        await using var fixture = new Fixture();
        SelectTarget(fixture);
        fixture.Update(s => s with
        {
            Objects = s.Objects.SetItem(1, s.Objects[1] with
            { ObjectType = renderType, RenderObjectType = renderType, RelationToPlayer = relation, HostileCombat = State(HostileCombatPhase.OrbitHold) })
        });
        Render(fixture.Screen);
        Assert.Null(fixture.Screen.SelectedOrActiveObjectInfo!.Value.HostileCombat);
        Assert.DoesNotContain(ObjectInfoPanel.BuildLines(fixture.Screen.SelectedOrActiveObjectInfo), line => line.Label == "Манёвр");
    }

    [Fact]
    public async Task Hover_without_selection_does_not_show_combat_phase()
    {
        await using var fixture = new Fixture();
        fixture.Update(s => s with
        {
            Objects = s.Objects.SetItem(1, s.Objects[1] with
            {
                ObjectType = SpaceObjectType.NpcShip,
                RenderObjectType = SpaceObjectType.NpcShip,
                RelationToPlayer = PlayerRelation.Enemy,
                HostileCombat = State(HostileCombatPhase.OrbitHold)
            })
        });
        Render(fixture.Screen);
        fixture.Screen.OnMouseMove(640, 420);
        Render(fixture.Screen);
        Assert.Equal("target", fixture.Screen.ActiveObjectId);
        Assert.Null(fixture.Screen.SelectedObjectId);
        Assert.Null(fixture.Screen.SelectedOrActiveObjectInfo!.Value.HostileCombat);
    }

    [Theory]
    [InlineData(600, 320)]
    [InlineData(800, 600)]
    public void Panel_wraps_long_phase_and_retains_radius_in_scrollable_content(int width, int height)
    {
        var panel = new ObjectInfoPanel();
        var data = new ObjectInfoPanelData("enemy", "Enemy", 1, 0, SpaceObjectType.NpcShip,
            RelationToPlayer: PlayerRelation.Enemy, HostileCombat: State(HostileCombatPhase.OrbitHold));
        using var bitmap = new SKBitmap(width, height);
        using var canvas = new SKCanvas(bitmap);
        panel.Render(canvas, width, 0, null, data, height, height);
        var lines = panel.BuildRenderLines(data);
        Assert.Contains(lines, line => line.Label == "Радиус орбиты");
        Assert.Contains("Удержание", string.Join(" ", lines.Select(line => line.Value)));
        Assert.Contains("орбиты", string.Join(" ", lines.Select(line => line.Value)));
        Assert.Contains("37", string.Join(" ", lines.Select(line => line.Value)));
    }
}
