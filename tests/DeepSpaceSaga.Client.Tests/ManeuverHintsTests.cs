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
        var maneuver = ManeuverInputSettings.Default;
        debug.ConfigureBindings(maneuver); debug.Prepare(1000, 700, new(0, [], []));
        debug.SetFeedback(null, null, false, false, null, 0);
        debug.ToggleHelp(debug.Bounds.Right - 20, debug.Bounds.Top + 15);
        Assert.Contains("W/" + DebugTuningPanel.KeyLabel(Key.Up), debug.ControlsHint);
        Assert.Contains("S/" + DebugTuningPanel.KeyLabel(Key.Down), debug.ControlsHint); Assert.Contains("Shift", debug.ControlsHint);
        Assert.DoesNotContain("Debug.", debug.ControlsHint); Assert.DoesNotContain("{", debug.ControlsHint);
        debug.Render(surface.Canvas);
        if (Environment.GetEnvironmentVariable("DSS_VALIDATION_ROOT") is { Length: > 0 } directory)
        {
            using var image = surface.Snapshot();
            using var data = image.Encode(SKEncodedImageFormat.Png, 100);
            using var output = File.Create(Path.Combine(directory, "maneuver-hints-" + language + ".png"));
            data.SaveTo(output);
        }
        debug.ConfigureBindings(maneuver with { Accelerate = [Key.U] });
        Assert.Contains("U", debug.ControlsHint); Assert.DoesNotContain("W/", debug.ControlsHint);
        foreach (string key in new[] { "Debug.HelpControls", "World.Panel.Reason.invalid_speed_payload", "Debug.Key.Up", "Debug.Key.Down", "Debug.Key.Left", "Debug.Key.Right" }) Assert.NotEqual(key, Localization.Get(key));
    }
}
