using DeepSpaceSaga.Client.UI.Screens.GameSession;
using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Motion;
using SkiaSharp;

namespace DeepSpaceSaga.Client.Tests;

public class TacticalMapLayoutOrderTests
{
    [Theory]
    [InlineData(.8f)]
    [InlineData(1f)]
    [InlineData(1.2f)]
    [InlineData(1.5f)]
    public void First_frame_resize_and_scale_have_current_layout(float scale)
    {
        var buffer = new SnapshotBuffer(() => 0);
        buffer.Update(new(1, 0, SimulationSpeed.Speed0, [new("player", 0, 0, 0, 0)], PlayerShipObjectId: "player"));
        var screen = new GameSessionScreen(buffer, new LinearMotionPredictor(), timestampProvider: () => 0);
        using var surface = SKSurface.Create(new SKImageInfo(1920, 1080));
        SKRect freeAtMap = default, commandsAtMap = default, infoAtMap = default;
        screen.RenderStageCompleted = stage =>
        {
            if (stage != "coordinates_and_hit_test") return;
            freeAtMap = screen.AvailableMapRect();
            commandsAtMap = screen.CommandsPanel.CaptionRect;
            infoAtMap = screen.ObjectInfoPanel.CaptionRect;
        };
        void Check(int width, int height)
        {
            screen.Render(surface.Canvas, width, height);
            Assert.False(commandsAtMap.IsEmpty);
            Assert.False(infoAtMap.IsEmpty);
            Assert.Equal(screen.AvailableMapRect(), freeAtMap);
            Assert.Equal(screen.CommandsPanel.CaptionRect, commandsAtMap);
            Assert.Equal(screen.ObjectInfoPanel.CaptionRect, infoAtMap);
        }
        Check(1920, 1080);
        screen.SetUiScale(scale);
        Check(1280, 720);
        var caption = screen.ObjectInfoPanel.CaptionRect;
        screen.OnMouseDown((caption.Left + 12) * scale, (caption.Top + 12) * scale);
        Check(1280, 720);
    }
}
