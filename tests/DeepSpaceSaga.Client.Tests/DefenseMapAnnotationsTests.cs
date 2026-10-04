using System.Diagnostics;
using DeepSpaceSaga.Client.UI.Screens.GameSession;
using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Motion;
using SkiaSharp;
namespace DeepSpaceSaga.Client.Tests;

public class DefenseMapAnnotationsTests
{
    [Fact]
    public void Chance_shown_at_projectile_and_encounter_with_frozen_tooltip()
    {
        var op = new WeaponOperatorSnapshot("crew", "Crew", WeaponSkillType.CountermeasureDefense, 50, 30, 30);
        var route = new TorpedoRoute(0, 1, TorpedoRoutePhase.Straight, true, [new(0, 0, 90, 12, 0, 1000)]);
        var pr = new ObjectMotionSnapshot("pr", 0, 0, 12, 90, RenderObjectType: SpaceObjectType.Countermeasure,
            Countermeasure: new("player", "launcher", "target", CountermeasurePhase.Guiding, route, 0, 500, new(op, 30), []));
        var buffer = new SnapshotBuffer(() => 0);
        buffer.Update(new(1, 0, SimulationSpeed.Speed0, [new("player", 0, 0, 0, 0), pr, new("target", 120, 0, 0, 0)], "player"));
        var screen = new GameSessionScreen(buffer, new LinearMotionPredictor(), timestampProvider: () => 0);
        using var bitmap = new SKBitmap(1280, 720);
        using var canvas = new SKCanvas(bitmap);
        screen.Render(canvas, 1280, 720);
        Assert.True(GameSessionScreen.HasCombatMarker(pr));
        var geometry = screen.DefenseGeometry["pr"];
        Assert.Equal(120, geometry.Intercept!.Value.X, 5);
        Assert.Equal(0, geometry.Intercept!.Value.Y, 5);
        Assert.Equal("50.0%", screen.DefenseChanceLabels["pr"].Chance);
        var marker = screen.DefenseChanceLabels["pr"].Encounter!.Value;
        screen.OnMouseMove(marker.X, marker.Y);
        screen.Render(canvas, 1280, 720);
        Assert.Contains("Crew", screen.DefenseTooltip);
        Assert.Contains("50.0%", screen.DefenseTooltip);
        buffer.Update(buffer.Latest!.Snapshot with { SnapshotSequence = 2, Objects = [new("player", 0, 0, 0, 0)] });
        screen.Render(canvas, 1280, 720);
        Assert.Null(screen.DefenseTooltip);
        screen.OnDeactivated();
    }
}
