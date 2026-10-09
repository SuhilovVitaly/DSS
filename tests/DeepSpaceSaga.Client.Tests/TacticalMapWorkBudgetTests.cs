using System.Collections.Immutable;
using DeepSpaceSaga.Client.UI.Screens.GameSession;
using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Motion;
using SkiaSharp;

namespace DeepSpaceSaga.Client.Tests;

public class TacticalMapWorkBudgetTests
{
    [Theory]
    [InlineData(500)]
    [InlineData(5000)]
    public void Offscreen_contacts_do_not_bootstrap_full_trails(int count)
    {
        var buffer = new SnapshotBuffer(() => 0);
        buffer.Update(new(1, 0, SimulationSpeed.Speed0,
            Enumerable.Range(0, count).Select(i => new ObjectMotionSnapshot("off-" + i, 1e9 + i, 1e9, 1, 90)).ToImmutableArray()));
        var screen = new GameSessionScreen(buffer, new LinearMotionPredictor(), timestampProvider: () => 0);
        using var surface = SKSurface.Create(new SKImageInfo(1920, 1080));
        screen.Render(surface.Canvas, 1920, 1080);
        Assert.Equal(count, screen.RenderStates.Count);
        Assert.Equal(count, screen.RenderPoseObjectsProcessed);
        Assert.Equal(0, screen.PoseDtoMaterializations);
        Assert.Equal(0, screen.TrailStatistics.Points);
        Assert.Equal(0, screen.TrailStatistics.Capacity);
        Assert.Equal(0, screen.DetailedTrailObjectsProcessed);
        screen.Render(surface.Canvas, 1920, 1080);
        Assert.Equal(1, screen.ContactMembershipBuilds);
    }

    [Fact]
    public void Pan_back_does_not_invent_history()
    {
        var buffer = new SnapshotBuffer(() => 0);
        buffer.Update(new(1, 0, SimulationSpeed.Speed0, [new("contact", 20000, 10000, 1, 90)]));
        var screen = new GameSessionScreen(buffer, new LinearMotionPredictor(), timestampProvider: () => 0);
        using var surface = SKSurface.Create(new SKImageInfo(1920, 1080));
        void Frame() => screen.Render(surface.Canvas, 1920, 1080);
        void Pan(float delta)
        {
            screen.OnMouseDown(900, 400);
            screen.OnMouseMove(900 - delta, 400);
            screen.OnMouseUp(900 - delta, 400);
            Frame();
        }
        Frame();
        Assert.Empty(screen.GetObjectTrail("contact"));
        Pan(10000);
        Assert.Equal(20000, Assert.Single(screen.GetObjectTrail("contact")).X);
        Pan(10000);
        Assert.Empty(screen.GetObjectTrail("contact"));
        Pan(-10000);
        Assert.Equal(20000, Assert.Single(screen.GetObjectTrail("contact")).X);
        Assert.Equal("contact", Assert.Single(screen.RenderStates).Pose.ObjectId);
    }

    [Fact]
    public void Trail_budget_preserves_important_targets()
    {
        var buffer = new SnapshotBuffer(() => 0);
        buffer.Update(new(1, 0, SimulationSpeed.Speed0,
            [new("player", 0, 0, 1, 90, NavigationTargetObjectId: "navigation"),
             new("selected", 1e9, 0, 1, 90), new("navigation", -1e9, 0, 1, 90)],
            PlayerShipObjectId: "player", SelectedObjectId: "selected"));
        var screen = new GameSessionScreen(buffer, new LinearMotionPredictor(), timestampProvider: () => 0);
        using var surface = SKSurface.Create(new SKImageInfo(1920, 1080));
        screen.Render(surface.Canvas, 1920, 1080);
        Assert.Equal(3, screen.TrailStatistics.Trails);
        Assert.All(new[] { "player", "selected", "navigation" }, id => Assert.NotEmpty(screen.GetObjectTrail(id)));
        var history = new ObjectTrailBuffer();
        for (int i = 0; i < 20000; i++) history.Add(new(i, 0, i));
        Assert.Equal(ObjectTrailBuffer.MaximumPoints, history.Count);
        Assert.Equal(ObjectTrailBuffer.MaximumPoints, history.Capacity);
        Assert.Equal(19999, history[^1].X);
        Assert.Equal(20000 - ObjectTrailBuffer.MaximumPoints, history[0].X);
    }
}
