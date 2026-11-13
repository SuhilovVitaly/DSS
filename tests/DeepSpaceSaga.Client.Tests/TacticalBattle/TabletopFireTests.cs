using DeepSpaceSaga.Client.UI.Screens.TacticalBattle;
using DeepSpaceSaga.Client.Tests.TacticalBattle.View;
using Silk.NET.Input;
using SkiaSharp;

namespace DeepSpaceSaga.Client.Tests.TacticalBattle;

[Collection("InterfaceLog")]
public sealed class TabletopFireTests
{
    [Fact]
    public async Task Clicking_a_reticle_assigns_the_same_candidate_as_the_pilot_card()
    {
        await using var scene = await Ep0019Scenes.Create("2-fire-hover-own");
        using var catalog = TacticalBattleSkinCatalog.Load(Path.Combine(AppContext.BaseDirectory, "Skins"));
        var screen = new TacticalBattleScreen(scene.Screen.Flow, 1, catalog.GetOrClassic("tabletop"));
        using var bitmap = new SKBitmap(1280, 720); using var canvas = new SKCanvas(bitmap);
        try
        {
            screen.Render(canvas, 1280, 720);
            var frame = screen.CaptureFrame(1280, 720)!;
            var selected = frame.FirePlan!.Selected!;
            var candidate = selected.Group.List.First(t => t.TargetId != selected.TargetId);
            var rect = screen.Layout.Rects["candidate:" + candidate.TargetId];
            screen.OnMouseDown(rect.MidX, rect.MidY, MouseButton.Left);
            Assert.Equal(candidate.TargetId, screen.BuildFireRequest().Targets.Single(t => t.DroneId == selected.Drone.Id).TargetId);
            screen.Render(canvas, 1280, 720);
            Assert.Contains(screen.Layout.Texts, t => t.Text == BattleDesign.Pct(candidate.Chance) && t.Color == SKColor.Parse("#FFE27A"));
        }
        finally { screen.OnDeactivated(); }
    }

    [Theory]
    [InlineData("2-fire-hover-own")]
    [InlineData("2-anomaly-absorption")]
    [InlineData("2-no-target")]
    [InlineData("2-evasion-plan")]
    public async Task Fire_templates_use_authoritative_targets_and_preserve_absorption(string name)
    {
        await using var scene = await Ep0019Scenes.Create(name);
        using var catalog = TacticalBattleSkinCatalog.Load(Path.Combine(AppContext.BaseDirectory, "Skins"));
        using var scope = TacticalBattleSkinRenderScope.Enter(catalog.GetOrClassic("tabletop"));
        using var bitmap = new SKBitmap(1920, 1080); using var canvas = new SKCanvas(bitmap); canvas.Scale(1.5f);
        var frame = scene.Frame;
        var camera = new TacticalBattleCamera();
        camera.Update(frame.Battle, 1280, 720, tabletop: true);
        frame = frame with { View = camera.View };
        var layout = new BattleLayout(); BattleDesign.Recording = layout;
        try { TacticalBattleMapRenderer.Draw(canvas, frame); }
        finally { BattleDesign.Recording = null; }
        var selected = frame.FirePlan!.Selected!;
        Assert.Equal(selected.Group.List.Length, layout.Rects.Keys.Count(k => k.StartsWith("candidate:", StringComparison.Ordinal)));
        foreach (var candidate in selected.Group.List)
        {
            var rect = layout.Rects["candidate:" + candidate.TargetId];
            var point = TacticalBattleMapRenderer.MarkerPoint(frame.View, frame.Battle.Units.Single(u => u.Id == candidate.TargetId));
            Assert.Equal(44, rect.Width, .001f); Assert.Equal(44, rect.Height, .001f);
            Assert.Equal(point.X, rect.MidX, .001f); Assert.Equal(point.Y, rect.MidY, .001f);
        }
        if (selected.TargetId is not null) Assert.Contains(SKColor.Parse("#FF4628"), bitmap.Pixels);
        if (selected.Group.List.Any(t => t.TargetId != selected.TargetId)) Assert.Contains(SKColor.Parse("#FF9A6A"), bitmap.Pixels);
        if (frame.FirePlan.Rows.Any(r => r.TargetId is not null)) Assert.DoesNotContain(SKColor.Parse("#D6CBA6"), bitmap.Pixels);
        if (name == "2-anomaly-absorption")
        {
            Assert.True(layout.Rects.ContainsKey("absorption:" + selected.Drone.Id));
            Assert.Contains(BattleDesign.Heat, bitmap.Pixels);
        }
        if (selected.Target is { } shot)
        {
            Assert.True(layout.Rects.ContainsKey("chance"));
            Assert.Contains(layout.Texts, t => t.Text == BattleDesign.Pct(shot.Chance) && t.Color == SKColor.Parse("#FFE27A"));
        }
        if (name == "2-fire-hover-own")
        {
            var chance = layout.Rects["chance"];
            Assert.All(layout.Rects.Where(p => p.Key.StartsWith("label:", StringComparison.Ordinal)), p => Assert.False(p.Value.IntersectsWith(chance), p.Key));
            var pills = layout.Rects.Where(p => p.Key == "chance" || p.Key.StartsWith("badge:own:", StringComparison.Ordinal) || p.Key.StartsWith("badge:threat:", StringComparison.Ordinal)).ToArray();
            for (int i = 0; i < pills.Length; i++)
                for (int j = i + 1; j < pills.Length; j++) Assert.False(pills[i].Value.IntersectsWith(pills[j].Value), pills[i].Key + "/" + pills[j].Key);
        }
    }
}
