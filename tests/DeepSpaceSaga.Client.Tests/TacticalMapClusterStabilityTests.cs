using System.Collections.Immutable;
using System.Reflection;
using DeepSpaceSaga.Client.UI;
using DeepSpaceSaga.Client.UI.Screens.GameSession;
using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Motion;
using SkiaSharp;

namespace DeepSpaceSaga.Client.Tests;

public class TacticalMapClusterStabilityTests
{
    private sealed class Scene : IDisposable
    {
        internal readonly GameSessionScreen Screen;
        internal readonly CameraState Camera;
        private readonly SKSurface _surface = SKSurface.Create(new SKImageInfo(1920, 1080));
        internal Scene(bool reverse = false)
        {
            var contacts = new[] { 39990d, 39999d, 40001d, 40010d }
                .Select((x, i) => new ObjectMotionSnapshot($"c{i}", x, 10000, 0, 0, RenderObjectType: SpaceObjectType.Asteroid));
            var buffer = new SnapshotBuffer(() => 0);
            buffer.Update(new(1, 0, SimulationSpeed.Speed0, (reverse ? contacts.Reverse() : contacts).ToImmutableArray()));
            Screen = new(buffer, new LinearMotionPredictor(), timestampProvider: () => 0);
            Camera = (CameraState)typeof(GameSessionScreen).GetField("_camera", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(Screen)!;
        }
        internal string[] Membership()
        {
            var cells = (Dictionary<(double X, double Y), List<ObjectRenderState>>)typeof(GameSessionScreen)
                .GetField("_clusterCells", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(Screen)!;
            return cells.OrderBy(c => c.Key.X).ThenBy(c => c.Key.Y)
                .Select(c => $"{c.Key.X},{c.Key.Y}:{string.Join(',', c.Value.Select(s => s.Pose.ObjectId).Order(StringComparer.Ordinal))}")
                .ToArray();
        }
        internal void Frame(double ppu) { Camera.SetZoom(ppu); Screen.Render(_surface.Canvas, 1920, 1080); }
        public void Dispose() { Screen.Dispose(); _surface.Dispose(); }
    }

    [Fact]
    public void Cluster_membership_is_stable_within_zoom_band()
    {
        using var scene = new Scene();
        scene.Frame(.001);
        var initial = scene.Membership();
        var ids = scene.Screen.MapClusterIds;
        for (int i = 0; i < 20; i++)
        {
            scene.Frame(i % 2 == 0 ? .0009999 : .0010001);
            Assert.Equal(initial, scene.Membership());
            Assert.Equal(ids, scene.Screen.MapClusterIds);
        }
    }

    [Fact]
    public void Pan_does_not_reassign_cluster_cells()
    {
        using var scene = new Scene();
        scene.Frame(.001);
        var initial = scene.Membership();
        var ids = scene.Screen.MapClusterIds;
        scene.Camera.SetFocus(2_000_000, 0);
        scene.Frame(.001);
        Assert.Equal(initial, scene.Membership());
        Assert.Equal(ids, scene.Screen.MapClusterIds);
    }

    [Fact]
    public void Cluster_boundary_transition_is_deterministic()
    {
        using var scene = new Scene();
        using var reversed = new Scene(true);
        (double Zoom, int Level)[] frames =
        [(.001, 0), (.00045001, 0), (.00044999, 1), (.00049999, 1), (.00050001, 1), (.00055001, 0)];
        foreach (var (zoom, level) in frames)
        {
            scene.Frame(zoom);
            reversed.Frame(zoom);
            Assert.Equal(level, scene.Screen.ClusterLevel);
            Assert.Equal(scene.Screen.MapClusterIds, reversed.Screen.MapClusterIds);
            Assert.Equal(scene.Membership(), reversed.Membership());
        }
        scene.Frame(.00110001);
        Assert.Null(scene.Screen.ClusterLevel);
        Assert.Empty(scene.Screen.MapClusterIds);
    }

    [Theory]
    [InlineData(-.01)]
    [InlineData(.41)]
    [InlineData(double.NaN)]
    public void Invalid_hysteresis_is_rejected(double value) =>
        Assert.Throws<ArgumentException>(() => new TacticalMapSettings { ClusterHysteresis = value }.Validate());
}
