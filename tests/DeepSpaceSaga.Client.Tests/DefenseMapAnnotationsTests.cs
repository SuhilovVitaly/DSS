using System.Diagnostics;
using DeepSpaceSaga.Client.UI.Screens.GameSession;
using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Motion;
using SkiaSharp;
namespace DeepSpaceSaga.Client.Tests;

public class DefenseMapAnnotationsTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Only_intercept_point_shows_chance_without_flight_labels_or_tooltip(bool capturedContact)
    {
        var op = new WeaponOperatorSnapshot("crew", "Crew", WeaponSkillType.CountermeasureDefense, 50, 55, 55);
        var route = new TorpedoRoute(0, 1, TorpedoRoutePhase.Straight, true, [new(0, 0, 90, 12, 0, 1000)]);
        var pr = new ObjectMotionSnapshot("pr", 0, 0, 12, 90, RenderObjectType: SpaceObjectType.Countermeasure,
            Countermeasure: new("player", "launcher", "target", CountermeasurePhase.Guiding, route, 0, 500, new(op, 5), []));
        var target = new ObjectMotionSnapshot("target", 120, 0, 0, 0);
        if (capturedContact)
        {
            pr = pr with { Countermeasure = pr.Countermeasure! with { ExpiresAtMotionTimeMs = 10000, SpeedKmS = 12, TurnRateDegPerSec = 90, PredictedEncounterMotionTimeMs = 115d / 120 * 1000 } };
            target = target with
            {
                Torpedo = new("enemy", "launcher", "player", 0, 1, 90, 0, 0,
                new(0, 1, TorpedoRoutePhase.Straight, true, [new(120, 0, 270, 1, 0, 10000)]), [], ExpiresAtMotionTimeMs: 10_000)
            };
        }
        var buffer = new SnapshotBuffer(() => 0);
        buffer.Update(new(1, 0, SimulationSpeed.Speed0, [new("player", 0, 0, 0, 0), pr, target], "player"));
        var screen = new GameSessionScreen(buffer, new LinearMotionPredictor(), timestampProvider: () => 0);
        using var bitmap = new SKBitmap(1280, 720);
        using var canvas = new SKCanvas(bitmap);
        screen.ForecastCache.UseWorker = false;
        screen.Render(canvas, 1280, 720);
        Assert.True(GameSessionScreen.HasCombatMarker(pr));
        var geometry = screen.DefenseGeometry["pr"];
        if (capturedContact)
        {
            Assert.NotNull(geometry.Intercept);
            var chance = Assert.Single(screen.DefenseChanceLabels).Value;
            Assert.Equal("50.0%", chance.Chance);
            Assert.NotNull(chance.Encounter);
            screen.OnMouseMove((float)geometry.Intercept.Value.X, (float)geometry.Intercept.Value.Y);
        }
        else
        {
            Assert.Null(geometry.Intercept);
            Assert.Empty(screen.DefenseChanceLabels);
        }
        Assert.Empty(screen.DefenseFlightLabels);
        screen.ForecastCache.UseWorker = false;
        screen.Render(canvas, 1280, 720);
        Assert.Null(screen.DefenseTooltip);
        buffer.Update(buffer.Latest!.Snapshot with { SnapshotSequence = 2, Objects = [new("player", 0, 0, 0, 0)] });
        screen.ForecastCache.UseWorker = false;
        screen.Render(canvas, 1280, 720);
        Assert.Null(screen.DefenseTooltip);
        screen.OnDeactivated();
    }
}
