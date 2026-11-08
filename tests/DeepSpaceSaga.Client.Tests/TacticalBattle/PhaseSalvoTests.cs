using DeepSpaceSaga.Client.Tests.TacticalBattle.View;
using Silk.NET.Input;

namespace DeepSpaceSaga.Client.Tests.TacticalBattle;

[Collection("InterfaceLog")]
public sealed class PhaseSalvoTests
{
    [Fact]
    public async Task Salvo_results_replay_and_continue_use_same_revision()
    {

        await using var s = await Ep0019Scenes.Create("3-salvo-flight"); using var locale = Localization.UseLocaleForTests("Russian");
        using (var b = s.Render()) Assert.Contains(s.Screen.Layout.Texts, t => t.Text == "Выстрел 1 из 3");
        s.Screen.OnKeyDown(Key.Enter);
        using (var b = s.Render())
            foreach (string text in new[] { "ПРОМАХ", "Броня: 80 → 39", "49%", "3 выстрела · 1 попадание" }) Assert.Contains(s.Screen.Layout.Texts, t => t.Text == text);
        ulong revision = s.Screen.Flow.State!.Revision;
        s.Screen.OnMouseDown(1070 * 1.5f, 343 * 1.5f, MouseButton.Left);
        Assert.True(s.Screen.CaptureFrame(1920, 1080)!.Salvo!.InFlight);
        Assert.Equal(revision, s.Screen.Flow.State.Revision);
        s.Screen.OnKeyDown(Key.Enter); s.Screen.OnKeyDown(Key.Enter);
        Assert.False(s.Screen.HasSalvoPresentation);
    }
}
