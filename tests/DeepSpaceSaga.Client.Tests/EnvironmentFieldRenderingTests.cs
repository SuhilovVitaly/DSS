using System.Text.Json;
using DeepSpaceSaga.Client.UI;
using DeepSpaceSaga.Client.UI.Screens.GameSession;
using DeepSpaceSaga.Client.UI.Screens.GameSession.Controls;
using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Motion;
using SkiaSharp;

namespace DeepSpaceSaga.Client.Tests;

public sealed class EnvironmentFieldRenderingTests
{
    private static EnvironmentFieldData Field(string kind = "Debris") => new("field", kind, 0.5, "Parent", "station", null, 0, 0, 0, 100, 0, 360, 42);

    [Fact]
    public void FieldKindsDifferWithoutColor()
    {
        var masks = new List<byte[]>();
        foreach (string kind in new[] { "Radiation", "Dust", "Debris" })
        {
            using var bitmap = new SKBitmap(240, 240); using var canvas = new SKCanvas(bitmap); canvas.Clear(SKColors.Transparent);
            new EnvironmentFieldRenderer().Draw(canvas, [new(Field(kind), 0, 0)], new(0, 0, 1), 240, 240, null);
            masks.Add(Enumerable.Range(0, 240 * 240).Select(i => bitmap.GetPixel(i % 240, i / 240).Alpha).ToArray());
            Assert.Contains(ObjectInfoPanel.BuildLines(new("field", kind, 0, 0, null, FieldKind: kind, FieldIntensity: 0.5)), l => l.Value == kind);
        }
        Assert.False(masks[0].SequenceEqual(masks[1])); Assert.False(masks[0].SequenceEqual(masks[2])); Assert.False(masks[1].SequenceEqual(masks[2]));
    }

    [Fact]
    public void FieldSelectionMatchesAuthoritativeBoundary()
    {
        var orbit = new OrbitalElements(100, 100, 30000, 0, 0, 0, "clockwise");
        var field = Field() with { InnerRadius = 10, OuterRadius = 20, StartAngleDegrees = 350, SweepDegrees = 30, OffsetX = 5, OffsetY = -2 };
        var snapshot = new AuthoritativeSnapshot(1, 0, SimulationSpeed.Speed0, [], AiMap: new(1, [], Fields: [field]));
        var geometry = Assert.Single(EnvironmentFieldRenderer.Resolve(snapshot, [new("station", 300, 100, 0, 0)], 0));
        Assert.Equal(305, geometry.X); Assert.Equal(98, geometry.Y);
        Assert.True(geometry.Contains(305, 83)); Assert.True(geometry.Contains(305, 78));
        Assert.False(geometry.Contains(305, 77.999)); Assert.False(geometry.Contains(305, 89)); Assert.False(geometry.Contains(305, 113));
        bool AtAngle(double angle) => geometry.Contains(305 + 15 * Math.Sin(angle * Math.PI / 180), 98 - 15 * Math.Cos(angle * Math.PI / 180));
        Assert.True(AtAngle(350)); Assert.True(AtAngle(20));
        Assert.False(AtAngle(349.99)); Assert.False(AtAngle(20.01));
        var moving = Assert.Single(EnvironmentFieldRenderer.Resolve(snapshot, [new("station", 400, 200, 0, 0)], 0));
        Assert.Equal(405, moving.X); Assert.Equal(198, moving.Y);
        snapshot = snapshot with { AiMap = snapshot.AiMap! with { Fields = [field with { AnchorKind = "Orbit", ParentObjectId = null, Orbit = orbit }] } };
        var own = Assert.Single(EnvironmentFieldRenderer.Resolve(snapshot, [], 25));
        Assert.Equal(105, own.X, 6); Assert.Equal(-2, own.Y, 6);

        var mapSnapshot = new AuthoritativeSnapshot(1, 0, SimulationSpeed.Speed0,
            [new("ship", 0, 0, 0, 0, RenderObjectType: "PlayerShip"), new("station", 300, 0, 0, 0, RenderObjectType: "Station")], "ship",
            AiMap: new(1, [], Fields: [Field()]));
        var buffer = new SnapshotBuffer(); buffer.Update(mapSnapshot); var screen = new GameSessionScreen(buffer, new LinearMotionPredictor());
        using var bitmap = new SKBitmap(1920, 1080); using var canvas = new SKCanvas(bitmap);
        screen.Render(canvas, 1920, 1080);
        screen.OnMouseDown(960, 540); screen.OnMouseUp(960, 540);
        screen.OnMouseDown(1335, 540); screen.OnMouseUp(1335, 540); screen.Render(canvas, 1920, 1080);
        Assert.Equal("field", screen.SelectedFieldId); Assert.Equal("ship", screen.SelectedObjectId);
        Assert.Contains(ObjectInfoPanel.BuildLines(screen.SelectedOrActiveObjectInfo), l => l.Value == EnvironmentFieldRenderer.Notice);
        buffer.Update(mapSnapshot with
        {
            SnapshotSequence = 2,
            InstalledModules =
            [new("nav", "nav", "Navigation", 0, [NavigationComputerCommandTypes.Dock], "On", "Ready", 100,
                Commands: [new(NavigationComputerCommandTypes.Dock, "Dock", "object")])]
        });
        screen.Render(canvas, 1920, 1080);
        Assert.False(screen.IsModuleCommandEnabled(NavigationComputerCommandTypes.Dock));
        screen.OnMouseDown(1260, 540); screen.OnMouseUp(1260, 540);
        Assert.Equal("station", screen.SelectedObjectId); Assert.Null(screen.SelectedFieldId);
        Assert.True(screen.IsModuleCommandEnabled(NavigationComputerCommandTypes.Dock));
    }

    [Fact]
    public void DecorationsDoNotAffectBoundaryOrEntities()
    {
        var snapshot = new AuthoritativeSnapshot(1, 0, SimulationSpeed.Speed0, [new("station", 0, 0, 0, 0)], AiMap: new(1, [], Fields: [Field()]));
        string before = JsonSerializer.Serialize(snapshot);
        var fields = EnvironmentFieldRenderer.Resolve(snapshot, snapshot.Objects, 0);
        var renderer = new EnvironmentFieldRenderer();
        foreach (var (count, zoom) in new[] { (8, 0.1), (256, 1.0), (64, 2.0) })
        {
            renderer.DecorationSamples = count;
            using var bitmap = new SKBitmap(800, 600); using var canvas = new SKCanvas(bitmap);
            renderer.Draw(canvas, fields, new(0, 0, zoom), 800, 600, null);
            Assert.Single(fields); Assert.True(fields[0].Contains(100, 0)); Assert.False(fields[0].Contains(100.01, 0));
            Assert.Equal(1, renderer.CachedPatternCount); Assert.Single(snapshot.Objects);
        }
        using var emptyBitmap = new SKBitmap(1, 1); using var emptyCanvas = new SKCanvas(emptyBitmap);
        renderer.Draw(emptyCanvas, [], new(0, 0, 1), 1, 1, null);
        Assert.Equal(0, renderer.CachedPatternCount);
        Assert.Equal(before, JsonSerializer.Serialize(snapshot));
    }
}
