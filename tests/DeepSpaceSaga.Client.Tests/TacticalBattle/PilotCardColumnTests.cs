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
        float top = 6;
        for (int i = 0; i < cards.Length; i++)
        {
            var r = cards[i];
            float height = scene == "1-move" && i == 0 ? 196 : 82;
            Assert.Equal(new SKRect(6, top, 196, top + height), r);
            top = r.Bottom + 5;
            Assert.True(RasterProbe.Fraction(bitmap, new(r.Left + 127, r.Top + 48, r.Left + 131, r.Top + 53), BattleDesign.Purple) >= .8);
            Assert.True(RasterProbe.Fraction(bitmap, new(r.Left + 149, r.Top + 48, r.Left + 153, r.Top + 53), BattleDesign.Cyan) >= .8);
            Assert.True(RasterProbe.Fraction(bitmap, new(r.Left + 171, r.Top + 48, r.Left + 175, r.Top + 53), BattleDesign.Green) >= .8);
        }
        Assert.Contains(s.Screen.Layout.Texts, text => text.Text == "80");
        Assert.Contains(s.Screen.Layout.Texts, text => text.Text == "65");
        Assert.Contains(s.Screen.Layout.Texts, text => text.Text == "100");
        Assert.DoesNotContain(s.Screen.Layout.Texts, text => text.Text is "80/80" or "65/65");
        ulong revision = s.Screen.Flow.State!.Revision;
        s.Screen.OnMouseDown(40, (cards[1].Top + 15) * 1.5f, MouseButton.Left);
        Assert.Equal("Юн", s.Screen.CaptureFrame(1920, 1080)!.SelectedDroneId);
        Assert.Equal(revision, s.Screen.Flow.State.Revision);
    }
}
