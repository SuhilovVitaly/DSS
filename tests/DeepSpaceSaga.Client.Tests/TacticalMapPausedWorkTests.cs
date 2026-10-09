using System.Diagnostics;
using DeepSpaceSaga.Client.UI.Screens.GameSession;
using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Motion;
using SkiaSharp;

namespace DeepSpaceSaga.Client.Tests;

public class TacticalMapPausedWorkTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Paused_frames_reuse_geometry(bool navigation)
    {
        long clock = 0;
        var obj = new ObjectMotionSnapshot("player", 0, 0, 1, 90);
        if (navigation) obj = obj with
        {
            NavigationTargetX = 1000,
            NavigationTargetY = 0,
            ActiveEngineCommandType = ShipEngineCommandTypes.Orbit,
            NavigationAngularInertiaDegPerSec = 1
        };
        var buffer = new SnapshotBuffer(() => clock);
        buffer.Update(new(1, 0, SimulationSpeed.Speed0, [obj], PlayerShipObjectId: "player"));
        var screen = new GameSessionScreen(buffer, new LinearMotionPredictor(), timestampProvider: () => clock);
        using var surface = SKSurface.Create(new SKImageInfo(1920, 1080));
        void Frame()
        {
            clock += Stopwatch.Frequency / 80;
            screen.Render(surface.Canvas, 1920, 1080);
        }
        for (int i = 0; i < 4; i++) Frame(); // Complete initial panel layout.
        var before = (screen.TrailGeometryBuilds, screen.FutureGeometryBuilds,
            screen.NavigationGeometryBuilds, screen.LabelGeometryBuilds);
        Assert.True(before.TrailGeometryBuilds > 0);
        Assert.True(navigation ? before.NavigationGeometryBuilds > 0 : before.FutureGeometryBuilds > 0);
        for (int i = 0; i < 40; i++) Frame(); // UI status/reticle time keeps advancing.
        Assert.Equal(before, (screen.TrailGeometryBuilds, screen.FutureGeometryBuilds,
            screen.NavigationGeometryBuilds, screen.LabelGeometryBuilds));

        screen.OnMouseWheel(1100, 400, -1);
        for (int i = 0; i < 12; i++) Frame();
        Assert.True(screen.TrailGeometryBuilds > before.TrailGeometryBuilds);
        Assert.True(screen.LabelGeometryBuilds > before.LabelGeometryBuilds);

        buffer.Update(new(2, 10000, SimulationSpeed.Speed0,
            [obj with { X = 100 }], PlayerShipObjectId: "player"));
        Frame();
        Assert.Equal(100, screen.RenderStates[0].Pose.X);
        Assert.True(navigation ? screen.NavigationGeometryBuilds > before.NavigationGeometryBuilds :
            screen.FutureGeometryBuilds > before.FutureGeometryBuilds);
    }
}
