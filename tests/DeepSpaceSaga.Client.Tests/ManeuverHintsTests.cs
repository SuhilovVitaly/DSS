using DeepSpaceSaga.Client.UI;
using DeepSpaceSaga.Client.UI.Screens.GameSession.Controls;
using DeepSpaceSaga.Contracts;
using Silk.NET.Input;
using SkiaSharp;

namespace DeepSpaceSaga.Client.Tests;

public sealed class ManeuverHintsTests
{
    [Theory]
    [InlineData("Russian")]
    [InlineData("English")]
    public void Hints_follow_actual_bindings_and_all_prose_is_translated(string language)
    {
        using var locale = Localization.UseLocaleForTests(language);
        using var debug = new DebugTuningPanel { Visible = true };
        using var surface = SKSurface.Create(new SKImageInfo(1000, 700));
        surface.Canvas.Clear(SKColors.Black);
        var threat = new ThreatPanel();
        var rows = new[] { new ThreatPanel.Row("observed", "slow", 8, true, false, true, true, true) };
        var input = CombatInputSettings.Default;
        debug.ConfigureBindings(input); debug.Prepare(1000, 700, new(0, [], []));
        debug.SetFeedback(null, null, false, false, null, 0);
        debug.ToggleHelp(debug.Bounds.Right - 20, debug.Bounds.Top + 15);
        threat.Render(surface.Canvas, new(0, 0, 500, 700), rows, PointCombatVisualSettings.Default, input);
        Assert.Contains("[E]", threat.ButtonLabels[0]); Assert.Contains("[Z]", threat.ButtonLabels[1]); Assert.Contains("[C]", threat.ButtonLabels[2]);
        Assert.Contains("W/" + DebugTuningPanel.KeyLabel(Key.Up), debug.ControlsHint);
        Assert.Contains("S/" + DebugTuningPanel.KeyLabel(Key.Down), debug.ControlsHint); Assert.Contains("Shift", debug.ControlsHint);
        Assert.DoesNotContain("Debug.", debug.ControlsHint); Assert.DoesNotContain("{", debug.ControlsHint);
        debug.Render(surface.Canvas);
        const string directory = "D:/DeepSpaceSaga/artifacts/validation/ep0013/hints-20261022";
        Directory.CreateDirectory(directory);
        using (var image = surface.Snapshot()) using (var data = image.Encode(SKEncodedImageFormat.Png, 100))
        using (var output = File.Create(Path.Combine(directory, language + ".png"))) data.SaveTo(output);
        input = input with { CountermissileAim = Key.R, EvadeLeft = Key.V, EvadeRight = Key.B, Accelerate = [Key.U] };
        debug.ConfigureBindings(input); threat.Render(surface.Canvas, new(0, 0, 500, 700), rows, PointCombatVisualSettings.Default, input);
        Assert.Contains("[R]", threat.ButtonLabels[0]); Assert.Contains("[V]", threat.ButtonLabels[1]); Assert.Contains("[B]", threat.ButtonLabels[2]);
        Assert.Contains("U", debug.ControlsHint); Assert.DoesNotContain("W/", debug.ControlsHint);
        foreach (string key in new[] { "Debug.HelpControls", "Combat.Reason.invalid_speed_payload", "Debug.Key.Up", "Debug.Key.Down", "Debug.Key.Left", "Debug.Key.Right" }) Assert.NotEqual(key, Localization.Get(key));
    }
}
