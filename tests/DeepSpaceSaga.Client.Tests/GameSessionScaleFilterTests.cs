using System.Collections.Immutable;
using DeepSpaceSaga.Client.UI.Screens.GameSession;
using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Motion;
using SkiaSharp;
using Xunit;

namespace DeepSpaceSaga.Client.Tests;

/// <summary>
/// ТЗ-10: the scale visibility filter lives in the client render list only —
/// hidden objects must still exist in the snapshot/buffer. Tests use the
/// RenderStates test seam, never pixel checks (ТЗ-09 AC 4).
/// </summary>
[Collection("InterfaceLog")] // ApplyScale logs through InterfaceLog
public class GameSessionScaleFilterTests
{
    private const int ScreenWidth = 1920;
    private const int ScreenHeight = 1080;

    private static ObjectMotionSnapshot Obj(string id, string renderType) => new(
        ObjectId: id,
        X: 0,
        Y: 0,
        SpeedKmS: 0,
        Direction: 0,
        ObjectType: renderType,
        RenderObjectType: renderType);

    private static (SnapshotBuffer Buffer, GameSessionScreen Screen) CreateScreenWithAllTypes()
    {
        var buffer = new SnapshotBuffer();
        var objects = ImmutableArray.Create(
            Obj("P1", SpaceObjectType.PlayerShip),
            Obj("SUN-1", SpaceObjectType.Sun),
            Obj("PL-1", SpaceObjectType.Planet),
            Obj("STA-1", SpaceObjectType.Station),
            Obj("AST-1", SpaceObjectType.Asteroid),
            Obj("NPC-1", SpaceObjectType.NpcShip),
            Obj("UNK-1", SpaceObjectType.UnknownSpaceObject));
        buffer.Update(new AuthoritativeSnapshot(
            SnapshotSequence: 1,
            GameTimeMs: 0,
            CurrentSpeed: SimulationSpeed.Speed0,
            Objects: objects,
            PlayerShipObjectId: "P1"));

        var screen = new GameSessionScreen(buffer, new LinearMotionPredictor());
        return (buffer, screen);
    }

    private static void Render(GameSessionScreen screen)
    {
        using var bitmap = new SKBitmap(ScreenWidth, ScreenHeight);
        using var canvas = new SKCanvas(bitmap);
        screen.Render(canvas, ScreenWidth, ScreenHeight);
    }

    private static void ClickScale(GameSessionScreen screen, int scaleIndex)
    {
        Render(screen); // fills ScaleButtonRects
        screen.OnMouseDown(screen.ScaleButtonRects[scaleIndex].MidX, screen.ScaleButtonRects[scaleIndex].MidY);
        Render(screen); // rebuilds the render list at the new scale
    }

    private static HashSet<string> RenderIds(GameSessionScreen screen)
    {
        return screen.RenderStates.Select(s => s.Predicted.ObjectId).ToHashSet(StringComparer.Ordinal);
    }

    [Theory]
    [InlineData(0)] // M0.5 (PPU 2.0)
    [InlineData(1)] // M1 (PPU 1.0)
    public void At_full_scale_all_7_types_are_in_render_list(int scaleIndex)
    {
        var (_, screen) = CreateScreenWithAllTypes();
        ClickScale(screen, scaleIndex);

        var ids = RenderIds(screen);
        Assert.Equal(7, ids.Count);
        Assert.Contains("AST-1", ids);
        Assert.Contains("UNK-1", ids);
        Assert.Contains("NPC-1", ids);
        Assert.Contains("P1", ids);
        Assert.Contains("STA-1", ids);
        Assert.Contains("PL-1", ids);
        Assert.Contains("SUN-1", ids);
    }

    [Theory]
    [InlineData(2)] // M10 (PPU 0.1)
    [InlineData(3)] // M100 (PPU 0.01)
    [InlineData(4)] // M1000 (PPU 0.001)
    public void At_small_scale_asteroid_unknown_and_npc_are_filtered_out(int scaleIndex)
    {
        var (_, screen) = CreateScreenWithAllTypes();
        ClickScale(screen, scaleIndex);

        var ids = RenderIds(screen);
        Assert.DoesNotContain("AST-1", ids);
        Assert.DoesNotContain("UNK-1", ids);
        Assert.DoesNotContain("NPC-1", ids);
    }

    [Theory]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    public void PlayerShip_and_large_objects_stay_visible_at_small_scale(int scaleIndex)
    {
        var (_, screen) = CreateScreenWithAllTypes();
        ClickScale(screen, scaleIndex);

        var ids = RenderIds(screen);
        Assert.Contains("P1", ids);
        Assert.Contains("STA-1", ids);
        Assert.Contains("PL-1", ids);
        Assert.Contains("SUN-1", ids);
    }

    [Fact]
    public void Player_ship_with_null_render_type_is_still_visible_at_small_scale()
    {
        // Legacy payload without RenderObjectType — player ship resolves via identity.
        var buffer = new SnapshotBuffer();
        buffer.Update(new AuthoritativeSnapshot(
            SnapshotSequence: 1,
            GameTimeMs: 0,
            CurrentSpeed: SimulationSpeed.Speed0,
            Objects: ImmutableArray.Create(new ObjectMotionSnapshot(
                ObjectId: "P1", X: 0, Y: 0, SpeedKmS: 0, Direction: 0)),
            PlayerShipObjectId: "P1"));

        var screen = new GameSessionScreen(buffer, new LinearMotionPredictor());
        ClickScale(screen, 4); // M1000

        Assert.Contains("P1", RenderIds(screen));
    }

    [Fact]
    public void Filter_does_not_touch_snapshot_or_buffer()
    {
        var (buffer, screen) = CreateScreenWithAllTypes();
        ClickScale(screen, 4); // M1000 — most objects filtered from render list

        Assert.NotNull(buffer.Latest);
        Assert.Equal(7, buffer.Latest!.Snapshot.Objects.Length);
        Assert.Contains(buffer.Latest.Snapshot.Objects, o => o.ObjectId == "AST-1");
    }
}
