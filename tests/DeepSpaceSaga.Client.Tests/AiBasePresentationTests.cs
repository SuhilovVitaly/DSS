using DeepSpaceSaga.Client.UI.Screens.GameSession;
using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Motion;
using SkiaSharp;

namespace DeepSpaceSaga.Client.Tests;

public sealed class AiBasePresentationTests
{
    private static AuthoritativeSnapshot Snapshot(string type, bool ai) => new(1, 0, SimulationSpeed.Speed0,
        [new("ship", 0, 0, 0, 0, RenderObjectType: "PlayerShip"),
         new("station", 150, 0, 0, 0, DisplayName: "Station", RenderObjectType: "Station", RelationToPlayer: PlayerRelation.Enemy),
         new("planet", 150, 0, 0, 0, RenderObjectType: "Planet")], "ship",
        InstalledModules: [new("nav", "nav", "Navigation", 0, [NavigationComputerCommandTypes.Dock], "On", "Ready", 100,
            Commands: [new(NavigationComputerCommandTypes.Dock, "Dock", "object")])],
        AiMap: ai ? new(1, [new("station", type, "Ai", type == "Planetary" ? "planet" : null, null, 0, 0)]) : null,
        ClusterMap: ai ? null : new(1, "home", [new("home", "Home", "belt", ["station"], "scientific-military")],
            [new("station", "home", "scientific-military")], []));

    [Theory]
    [InlineData("Planetary")]
    [InlineData("Orbital")]
    public void AiBaseMarkerOrbitAndSelectionAreHidden(string type)
    {
        var source = Snapshot(type, true);
        var orbit = new OrbitalElements(150, 150, 30000, 90, 0, 0, "clockwise");
        var snapshot = source with
        {
            Objects = source.Objects.SetItem(1, source.Objects[1] with { Orbit = orbit }),
            SelectedObjectId = "station"
        };
        var buffer = new SnapshotBuffer(); buffer.Update(snapshot);
        var screen = new GameSessionScreen(buffer, new LinearMotionPredictor());
        using var bitmap = new SKBitmap(1920, 1080); using var canvas = new SKCanvas(bitmap);
        canvas.Clear(SKColors.Black);
        screen.Render(canvas, 1920, 1080);
        Assert.True(screen.ShowOrbits);
        Assert.Contains("station", screen.AggregatedMapObjectIds);
        Assert.DoesNotContain("station", screen.MapLabels.Keys);
        Assert.Null(screen.SelectedObjectId);
        // At the orbit's rightmost point only the planet marker overlaps. Check the
        // opposite arc against a frame containing no base.
        var pixel = bitmap.GetPixel(810, 540);
        buffer.Update(source with { SnapshotSequence = 2, Objects = source.Objects.RemoveAt(1), AiMap = source.AiMap! with { Bases = [] } });
        canvas.Clear(SKColors.Black);
        screen.Render(canvas, 1920, 1080);
        Assert.Equal(pixel, bitmap.GetPixel(810, 540));
        buffer.Update(snapshot with { SnapshotSequence = 3, SelectedObjectId = null });
        screen.Render(canvas, 1920, 1080);
        screen.OnMouseDown(1110, 540); screen.OnMouseUp(1110, 540);
        Assert.Equal("planet", screen.SelectedObjectId);
    }

    [Fact]
    public void HumanScientificMilitaryNotAi()
    {
        var buffer = new SnapshotBuffer(); buffer.Update(Snapshot("Orbital", false));
        var screen = new GameSessionScreen(buffer, new LinearMotionPredictor());
        using var bitmap = new SKBitmap(1920, 1080); using var canvas = new SKCanvas(bitmap);
        screen.Render(canvas, 1920, 1080);
        screen.OnMouseDown(1110, 540); screen.OnMouseUp(1110, 540);
        screen.Render(canvas, 1920, 1080);
        Assert.Equal("station", screen.SelectedObjectId);
        Assert.Null(screen.SelectedOrActiveObjectInfo!.Value.Owner);
        Assert.Equal("scientific-military", screen.SelectedOrActiveObjectInfo.Value.ClusterProfile);
        Assert.True(screen.IsModuleCommandEnabled(NavigationComputerCommandTypes.Dock));
    }
}
