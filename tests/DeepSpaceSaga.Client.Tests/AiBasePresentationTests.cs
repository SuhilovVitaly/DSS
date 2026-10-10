using DeepSpaceSaga.Client.UI.Screens.GameSession;
using DeepSpaceSaga.Client.UI.Screens.GameSession.Controls;
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
    public void AiBaseSelectableAndIdentified(string type)
    {
        var buffer = new SnapshotBuffer(); buffer.Update(Snapshot(type, true));
        var screen = new GameSessionScreen(buffer, new LinearMotionPredictor());
        using var bitmap = new SKBitmap(1920, 1080); using var canvas = new SKCanvas(bitmap);
        screen.Render(canvas, 1920, 1080);
        screen.OnMouseDown(1110, 540); screen.OnMouseUp(1110, 540);
        screen.Render(canvas, 1920, 1080);
        Assert.Equal("station", screen.SelectedObjectId);
        var data = screen.SelectedOrActiveObjectInfo!.Value;
        Assert.Equal("Ai", data.Owner); Assert.Equal(type, data.BaseType);
        Assert.Contains(ObjectInfoPanel.BuildLines(data), l => l.Label == "Owner" && l.Value == "Ai");
        Assert.False(screen.IsModuleCommandEnabled(NavigationComputerCommandTypes.Dock));
        using var glyph = new SKBitmap(64, 64); using var glyphCanvas = new SKCanvas(glyph);
        glyphCanvas.Clear(SKColors.Transparent);
        AiMapPresentation.DrawBase(glyphCanvas, buffer.Latest!.Snapshot.AiMap!.Bases[0], 24, 24, 10);
        Assert.Contains(glyph.Pixels, p => p.Alpha > 0);
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
