using System.Collections.Immutable;
using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Client.UI.Screens.Dialogue;
using DeepSpaceSaga.Client.UI.Screens.GameSession;
using DeepSpaceSaga.Client.UI.Screens;
using DeepSpaceSaga.Motion;
using SkiaSharp;

namespace DeepSpaceSaga.Client.Tests.Demo;

public sealed class DemoEp0035Us0010Tk0005Tests
{
    [Theory]
    [InlineData("Russian")]
    [InlineData("English")]
    public void ActualDialogueRendersBatchWithScrollingAndExplicitUnknownEta(string language)
    {
        using var locale = Localization.UseLocaleForTests(language);
        var reports = Enumerable.Range(0, 4).Select(i => new SensorContactReport("r" + i, "g" + i, null, 100,
            90, 1500, i == 0 ? 0 : null, i == 0 ? null : "no_intercept", 12, ["lair", "lair"])).ToImmutableArray();
        var batch = new SensorContactReportBatch("batch", reports);
        var state = new DialogueState("sensor-dialogue", "dialogue.sensor-pirate-contact", null, "sensor-script", "report", 0, false,
            [new("acknowledge", "Demo.SensorReport.Acknowledge", true)], "Demo.SensorReport.Intro", "Sensor Operator", "sensor-script", null,
            ImmutableDictionary<string, string>.Empty.Add("sensorBatchId", "batch"));
        var buffer = new SnapshotBuffer();
        buffer.Update(new AuthoritativeSnapshot(1, 100, SimulationSpeed.Speed0, [new("player", 0, 0, 0, 0)], "player",
            ActiveDialogue: state, DemoCampaign: new(1, 1, PendingSensorReport: batch)));
        var game = new GameSessionScreen(buffer, new LinearMotionPredictor());
        Assert.Equal(ScreenEvent.OpenDialogue, game.ConsumePendingAutoTransition());
        var screen = new DialogueScreen(buffer, null, state);
        using var bitmap = new SKBitmap(1600, 900);
        using var canvas = new SKCanvas(bitmap);
        screen.Render(canvas, 1600, 900);
        Assert.True(screen.SpeechHasMoreBelow);
        string text = DialogueScreen.SensorReportText(batch);
        Assert.Contains(Localization.Get("Demo.SensorReport.NoEta"), text);
        Assert.Contains(string.Format(Localization.Get("Demo.SensorReport.Eta"), 0), text);
        Assert.Contains("150", text);
        Assert.DoesNotContain("{", text);
        Assert.DoesNotContain("sensor-script", text);
        screen.OnDeactivated();
    }
}
