using System.Collections.Immutable;
using DeepSpaceSaga.Client.UI.Screens.GameSession;
using DeepSpaceSaga.Client.UI.Screens.GameSession.Controls;
using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Motion;
using SkiaSharp;

namespace DeepSpaceSaga.Client.Tests;

public sealed class PointCombatUS0013TK0001Tests
{
    private static ImmutableArray<InstalledModuleSnapshot> Modules => Enumerable.Range(0, 9).Select(i =>
        new InstalledModuleSnapshot("m" + i, "type", "Module " + i, i, [], ArmorCurrent: 20, ArmorMax: 40,
            StructureCurrent: 50, StructureMax: 100, IsDestroyed: i < 3, IsCritical: i == 4,
            DisabledUntilMotionTimeMs: i == 5 ? 8000 : null)).ToImmutableArray();

    [Fact]
    public void Module_rows_keep_structure_and_destroyed_state()
    {
        var diagram = new ModuleDiagram();
        diagram.Update(Modules);
        Assert.Equal(9, diagram.Tiles.Count);
        Assert.Equal(3, diagram.Tiles.Count(tile => tile.Destroyed));
        Assert.Equal(50, diagram.Tiles[4].Structure);
        using var bitmap = new SKBitmap(480, 300); using var canvas = new SKCanvas(bitmap);
        var panel = new CommandsPanel { OwnModuleDiagram = diagram };
        panel.Render(canvas, Modules, 300, "player");
        Assert.Contains(panel.CommandPanelRows.SelectMany(r => r.DetailLines), l => l.Contains(Localization.Get("World.Panel.Structure")));
        Assert.Contains(panel.CommandPanelRows.SelectMany(r => r.DetailLines), l => l.Contains(Localization.Get("World.Panel.Destroyed")));
    }

    [Fact]
    public async Task Critical_flag_comes_from_actual_configured_engine_rules()
    {
        var source = PointCombatSessionFixture.Registry();
        // Use an installed role rather than assuming a particular module registry order.
        await using var baseline = new PointCombatSessionFixture(source);
        string role = baseline.Engine.CaptureSnapshot().InstalledModules.First(m => m.CombatRole is not null).CombatRole!;
        var rules = source.CombatRules! with { ShipDestruction = source.CombatRules!.ShipDestruction with { CriticalRoles = [role] } };
        await using var world = new PointCombatSessionFixture(PointCombatSessionFixture.Registry(rules: rules));
        var snapshot = world.Engine.CaptureSnapshot();
        Assert.Contains(snapshot.InstalledModules, m => m.IsCritical);
        foreach (var module in snapshot.InstalledModules) Assert.Equal(module.CombatRole == role, module.IsCritical);
    }

    [Theory]
    [InlineData(1f, 1920, 1080)]
    [InlineData(1.5f, 1920, 1080)]
    [InlineData(1.5f, 1280, 720)]
    public void Actual_screen_moves_module_details_to_left_panels(float scale, int width, int height)
    {
        var snapshot = new AuthoritativeSnapshot(1, 0, SimulationSpeed.Speed0,
            [new("player", 0, 0, 0, 0, RenderObjectType: SpaceObjectType.PlayerShip)], "player",
            InstalledModules: Modules, CombatJournal: [], Encounter: new(CombatEncounterState.Active, 0, null, new()));
        var buffer = new SnapshotBuffer(() => 0); buffer.Update(snapshot);
        var screen = new GameSessionScreen(buffer, new LinearMotionPredictor(), timestampProvider: () => 0, uiScale: scale);
        using var bitmap = new SKBitmap(width, height); using var canvas = new SKCanvas(bitmap);
        screen.Render(canvas, width, height);
        Assert.Equal(9, screen.CommandsPanel.OwnModuleDiagram!.Tiles.Count);
        Assert.True(screen.ObjectInfoPanel.RowBodyRects[0].Height > 0);
        Assert.Equal(9, screen.CommandsPanel.OwnModuleDiagram!.Tiles.Count);
        Assert.Contains(screen.CommandsPanel.CommandPanelRows.SelectMany(r => r.DetailLines), s => s.Contains(Localization.Get("World.Panel.Structure")) && s.Contains("50"));
        if (Environment.GetEnvironmentVariable("DSS_POINT_RENDER_ARTIFACT_DIRECTORY") is { Length: > 0 } directory)
        {
            Directory.CreateDirectory(directory); using var image = SKImage.FromBitmap(bitmap); using var png = image.Encode(SKEncodedImageFormat.Png, 100);
            using var file = File.Create(Path.Combine(directory, $"module-diagram-{width}-{height}-{scale}.png")); png.SaveTo(file);
        }
        screen.OnDeactivated();
    }
}
