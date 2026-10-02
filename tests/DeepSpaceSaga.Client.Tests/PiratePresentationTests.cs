using DeepSpaceSaga.Client.UI;
using DeepSpaceSaga.Client.UI.Screens.GameSession;
using DeepSpaceSaga.Client.UI.Screens.GameSession.Controls;
using DeepSpaceSaga.Contracts;
using SkiaSharp;

namespace DeepSpaceSaga.Client.Tests;

public sealed class PiratePresentationTests
{
    [Theory]
    [InlineData(1)]
    [InlineData(.001)]
    [InlineData(.00001)]
    public void Unselected_pirate_label_remains_visible_at_every_scale(double ppu)
    {
        var motion = new ObjectMotionSnapshot("pirate", 0, 0, .4, 120,
            RenderObjectType: SpaceObjectType.NpcShip, RelationToPlayer: PlayerRelation.Enemy, DisplayName: "Black Fang");
        ObjectRenderState[] states = [new(motion, motion, false)];
        var camera = new CameraState(0, 0, ppu);
        var renderer = new ObjectLabelRenderer();
        // Exhausted label budget also must not suppress the hostile contact.
        renderer.ComputeGeometries(states, .016, 800, 600, camera, mapSettings: new() { MaximumLabels = 0 }, isImportant: _ => false);
        using var bitmap = new SKBitmap(800, 600);
        bitmap.Erase(SKColors.Transparent);
        using var canvas = new SKCanvas(bitmap);
        renderer.DrawPlaques(canvas, states, 0, SimulationSpeed.Speed0, 800, 600, camera);
        canvas.Flush();
        Assert.Contains(bitmap.Pixels, p => p == SpaceMapColorResolver.NpcEnemyColor);
    }

    [Theory]
    [InlineData(SpaceObjectType.NpcShip, PlayerRelation.Enemy, true)]
    [InlineData(SpaceObjectType.NpcShip, PlayerRelation.Neutral, false)]
    [InlineData(SpaceObjectType.NpcShip, PlayerRelation.Friend, false)]
    [InlineData(SpaceObjectType.PlayerShip, PlayerRelation.Self, false)]
    public void Selected_ship_image_is_mirrored_only_for_pirates(string type, string relation, bool mirrored)
    {
        var path = Path.Combine(Path.GetTempPath(), $"pirate-image-{Guid.NewGuid():N}.png");
        try
        {
            using var source = new SKBitmap(200, 150);
            using (var sourceCanvas = new SKCanvas(source))
            using (var paint = new SKPaint { Color = SKColors.Blue })
            {
                sourceCanvas.Clear(SKColors.Red);
                sourceCanvas.DrawRect(100, 0, 100, 150, paint);
            }
            using (var png = source.Encode(SKEncodedImageFormat.Png, 100))
                File.WriteAllBytes(path, png.ToArray());
            var panel = new ObjectInfoPanel();
            using var bitmap = new SKBitmap(1280, 720);
            using var canvas = new SKCanvas(bitmap);
            panel.Render(canvas, 1280, 8, null,
                new("ship", "Black Fang", .4, 120, type, Image: path, RelationToPlayer: relation));
            canvas.Flush();
            var body = panel.RowBodyRects[1];
            Assert.Equal(mirrored ? SKColors.Blue : SKColors.Red, bitmap.GetPixel((int)body.Left + 31, (int)body.Top + 81));
            Assert.Equal(mirrored ? SKColors.Red : SKColors.Blue, bitmap.GetPixel((int)body.Left + 181, (int)body.Top + 81));
        }
        finally { File.Delete(path); }
    }
}
