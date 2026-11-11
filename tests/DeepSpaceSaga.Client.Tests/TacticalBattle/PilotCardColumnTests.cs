using DeepSpaceSaga.Client.Tests.TacticalBattle.View;
using DeepSpaceSaga.Client.UI.Screens.TacticalBattle;
using Silk.NET.Input;
using SkiaSharp;

namespace DeepSpaceSaga.Client.Tests.TacticalBattle;

[Collection("InterfaceLog")]
public sealed class PilotCardColumnTests
{
    [Theory]
    [InlineData("1-move")]
    [InlineData("2-fire")]
    public async Task Column_layout_probes_and_click_selection(string scene)
    {
        await using var s = await Ep0019Scenes.Create(scene); using var bitmap = s.Render();
        var cards = s.Screen.Layout.Rects.Where(pair => pair.Key.StartsWith("pilot:", StringComparison.Ordinal))
            .Select(pair => pair.Value).OrderBy(rect => rect.Top).ToArray();
        Assert.Equal(4, cards.Length);
        for (int i = 0; i < cards.Length; i++)
        {
            var r = cards[i];
            Assert.Equal(new SKRect(6, 6 + 87 * i, 196, 88 + 87 * i), r);
            Assert.True(RasterProbe.Fraction(bitmap, new(r.Left + 61, r.Top + 40.5f, r.Left + 70, r.Top + 41.5f), BattleDesign.Purple) >= .8);
            Assert.True(RasterProbe.Fraction(bitmap, new(r.Left + 61, r.Top + 62.5f, r.Left + 70, r.Top + 63.5f), BattleDesign.Green) >= .8);
        }
        ulong revision = s.Screen.Flow.State!.Revision;
        s.Screen.OnMouseDown(40, 150, MouseButton.Left);
        Assert.Equal("Юн", s.Screen.CaptureFrame(1920, 1080)!.SelectedDroneId);
        Assert.Equal(revision, s.Screen.Flow.State.Revision);
    }
}
