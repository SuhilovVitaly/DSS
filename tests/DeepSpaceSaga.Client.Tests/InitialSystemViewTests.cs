using DeepSpaceSaga.Client.UI.Screens.GameSession;
using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Motion;
using SkiaSharp;

namespace DeepSpaceSaga.Client.Tests;

public sealed class InitialSystemViewTests
{
    [Fact]
    public void RealDefaultSystemRenders()
    {
        using var engine = DeepSpaceSaga.Engine.SimulationEngine.CreateFromSettingsFile(DefaultSystemContentTests.Settings);
        var buffer = new SnapshotBuffer();
        buffer.Update(engine.CaptureSnapshot());
        var screen = new GameSessionScreen(buffer, new LinearMotionPredictor());
        using var bitmap = new SKBitmap(1920, 1080);
        using var canvas = new SKCanvas(bitmap);
        canvas.Clear(new SKColor(8, 14, 22));
        screen.Render(canvas, 1920, 1080);
        Assert.True(screen.FitMapView(MapFitMode.System));
        canvas.Clear(new SKColor(8, 14, 22));
        screen.Render(canvas, 1920, 1080);
        if (Environment.GetEnvironmentVariable("DSS_SOLAR_EVIDENCE") is { } path)
        {
            using var image = SKImage.FromBitmap(bitmap);
            using var data = image.Encode(SKEncodedImageFormat.Png, 100);
            using var file = File.Create(path);
            data.SaveTo(file);
        }
    }

    private static SolarSystemMapSnapshot Map() => new(1, 1, 1100000,
        [new BeltMapData("belt", 900000, 1000000, 1)], [], []);

    [Theory]
    [InlineData(1280, 720)]
    [InlineData(1920, 1080)]
    public void SystemFitIncludesWholeBelts(int width, int height)
    {
        var buffer = new SnapshotBuffer();
        buffer.Update(new AuthoritativeSnapshot(1, 0, SimulationSpeed.Speed0,
            [new("ship", 700000, 0, 0, 0, RenderObjectType: "PlayerShip")], "ship", SolarSystemMap: Map()));
        var screen = new GameSessionScreen(buffer, new LinearMotionPredictor());
        using var bitmap = new SKBitmap(width, height);
        using var canvas = new SKCanvas(bitmap);
        screen.Render(canvas, width, height);
        Assert.True(screen.FitMapView(MapFitMode.System));
        screen.Render(canvas, width, height);
        foreach (var point in new[] { (-1100000d, -1100000d), (1100000d, 1100000d) })
        {
            double x = width / 2.0 + (point.Item1 - screen.CameraFocusX) * screen.CameraPixelsPerWorldUnit;
            double y = height / 2.0 + (point.Item2 - screen.CameraFocusY) * screen.CameraPixelsPerWorldUnit;
            Assert.InRange(x, 0, width);
            Assert.InRange(y, 0, height);
        }
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void SystemLayerKeepsSelection(bool enabled)
    {
        var buffer = new SnapshotBuffer();
        buffer.Update(new AuthoritativeSnapshot(1, 0, SimulationSpeed.Speed0,
            [new("ship", 700000, 0, 0, 0, RenderObjectType: "PlayerShip"),
             new("station", 700150, 0, 0, 0, RenderObjectType: "Station")], "ship", SolarSystemMap: enabled ? Map() : null));
        var screen = new GameSessionScreen(buffer, new LinearMotionPredictor());
        using var bitmap = new SKBitmap(1920, 1080);
        using var canvas = new SKCanvas(bitmap);
        screen.Render(canvas, 1920, 1080);
        screen.OnMouseDown(1110, 540);
        Assert.Equal("station", screen.SelectedObjectId);
        screen.Render(canvas, 1920, 1080);
        Assert.Equal("station", screen.SelectedObjectId);
        screen.OnMouseDown(960, 540);
        Assert.Equal("ship", screen.SelectedObjectId);
    }
}
