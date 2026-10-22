using System.Collections.Immutable;
using DeepSpaceSaga.Client.UI.Screens.GameSession;
using DeepSpaceSaga.Client.UI.Screens.GameSession.Controls;
using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Motion;
using SkiaSharp;

namespace DeepSpaceSaga.Client.Tests;

public sealed class PointCombatUS0010TK0003Tests
{
    [Theory]
    [InlineData(1.0)]
    [InlineData(.1)]
    public async Task Range_geometry_reads_captured_capabilities_instead_of_UI_weapon_constants(double ppu)
    {
        await using var fixture = new PointCombatSessionFixture();
        var state = fixture.Engine.CaptureSnapshot();
        state = state with
        {
            InstalledModules = state.InstalledModules.Select(m => m with
            {
                LauncherCombat = m.LauncherCombat is { } torpedo ? torpedo with { RangeKm = 80 } : null,
                Defense = m.Defense is { } counter ? counter with { RangeKm = 17 } : null
            }).ToImmutableArray()
        };
        var buffer = new SnapshotBuffer(() => 0); buffer.Update(state);
        var map = new DeepSpaceSaga.Client.UI.TacticalMapSettings
        {
            MetersPerPixel = [100 / ppu, 1000 / ppu, 10000 / ppu, 100000 / ppu, 1000000 / ppu],
            LabelDetailPpu = ppu,
            CompactMarkerPpu = ppu,
            ClusterPpu = ppu,
            TrailDetailPpu = ppu
        };
        var screen = new GameSessionScreen(buffer, new LinearMotionPredictor(), timestampProvider: () => 0, mapSettings: map);
        using var bitmap = new SKBitmap(1920, 1200); using var canvas = new SKCanvas(bitmap);
        screen.Render(canvas, 1920, 1200);
        Assert.Equal(new[] { 17d, 17d, 80d }, screen.WeaponRangeRings.Select(r => r.RangeKm).Order());
        Assert.All(screen.WeaponRangeRings, r => Assert.Equal(r.RangeKm * 10 * ppu, r.RadiusPx, 3));
        screen.OnDeactivated();
    }

    [Theory]
    [InlineData(1f)]
    [InlineData(1.5f)]
    public async Task Range_rings_default_visible_and_toggle_independently(float scale)
    {
        await using var fixture = new PointCombatSessionFixture();
        var state = fixture.Engine.CaptureSnapshot();
        var buffer = new SnapshotBuffer(() => 0);
        var session = Guid.NewGuid();
        buffer.UpdatePresentation(new(session, 1, state.MotionTimeMs, [], state, false, 0));
        var screen = new GameSessionScreen(buffer, new LinearMotionPredictor(), timestampProvider: () => 0, uiScale: scale);
        using var bitmap = new SKBitmap(1920, 1200); using var canvas = new SKCanvas(bitmap);
        void Render() => screen.Render(canvas, 1920, 1200);
        Render();
        Assert.Equal(3, screen.WeaponRangeRings.Count);
        Assert.Equal(new[] { 30d, 30d, 70d }, screen.WeaponRangeRings.Select(r => r.RangeKm).Order());
        Assert.All(screen.WeaponRangeRings, r => Assert.Equal(r.RangeKm * 10 * screen.CameraPixelsPerWorldUnit, r.RadiusPx, 3));
        // Close unrelated bodies so both module headers remain reachable at high DPI.
        foreach (string name in new[] { "Navigation", "Maneuver", "Engine", "Space Control" })
        {
            var row = screen.CommandsPanel.CommandPanelRows.FirstOrDefault(r => r.Name == name && r.Opened);
            if (row.Name is null) continue;
            screen.OnMouseDown(row.CaptionRect.MidX * scale, row.CaptionRect.MidY * scale); Render();
        }
        var ids = screen.WeaponRangeRings.Select(r => r.ModuleId).ToArray();
        var hovered = screen.CommandsPanel.CommandPanelRows.Single(r => r.RangeModuleId == ids[0]);
        double radius = screen.WeaponRangeRings.Single(r => r.ModuleId == ids[0]).RadiusPx;
        screen.OnMouseMove(hovered.CaptionRect.MidX * scale, hovered.CaptionRect.MidY * scale); Render();
        Assert.True(screen.WeaponRangeRings.Single(r => r.ModuleId == ids[0]).Highlighted);
        Assert.Equal(radius, screen.WeaponRangeRings.Single(r => r.ModuleId == ids[0]).RadiusPx);
        foreach (string id in ids)
        {
            var row = Assert.Single(screen.CommandsPanel.CommandPanelRows, r => r.RangeModuleId == id);
            Assert.True(row.RangeVisible);
            Assert.InRange(row.RangeToggleRect.Bottom * scale, 0, 1200);
            screen.OnMouseDown(row.RangeToggleRect.MidX * scale, row.RangeToggleRect.MidY * scale);
            screen.OnMouseMove(row.CaptionRect.MidX * scale, row.CaptionRect.MidY * scale); Render();
            Assert.False(screen.CommandsPanel.IsWeaponRangeVisible(id));
            Assert.DoesNotContain(screen.WeaponRangeRings, r => r.ModuleId == id); // hover cannot override off
        }
        Assert.Empty(screen.WeaponRangeRings);
        var selected = screen.CommandsPanel.CommandPanelRows.Single(r => r.RangeModuleId == ids[1]);
        screen.OnMouseDown(selected.RangeToggleRect.MidX * scale, selected.RangeToggleRect.MidY * scale); Render();
        Assert.Equal(ids[1], Assert.Single(screen.WeaponRangeRings).ModuleId);
        var hide = screen.CommandsPanel.HideShowButtonRect;
        screen.OnMouseDown(hide.MidX * scale, hide.MidY * scale); Render();
        Assert.Equal(CommandsPanelState.Closed, screen.CommandsPanel.State);
        Assert.Equal(ids[1], Assert.Single(screen.WeaponRangeRings).ModuleId);
        // Same-session coarse revisions do not reset off choices.
        buffer.UpdatePresentation(new(session, 2, state.MotionTimeMs, [new(state.MotionTimeMs, state)], state, false, 0));
        Render(); Assert.Equal(ids[1], Assert.Single(screen.WeaponRangeRings).ModuleId);
        buffer.UpdatePresentation(new(Guid.NewGuid(), 1, state.MotionTimeMs, [], state, false, 0));
        Render(); Assert.Equal(3, screen.WeaponRangeRings.Count);
        screen.OnDeactivated();
    }
}
