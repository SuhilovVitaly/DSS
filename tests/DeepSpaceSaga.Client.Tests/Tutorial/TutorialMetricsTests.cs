using System.Text.Json;
using DeepSpaceSaga.Client.UI.Screens;
using DeepSpaceSaga.Client.UI.Screens.TacticalBattle;
using DeepSpaceSaga.Client.UI.Screens.Tutorial;
using Silk.NET.Input;
using SkiaSharp;
using static DeepSpaceSaga.Client.Tests.Tutorial.TutorialCommsPanelTests;

namespace DeepSpaceSaga.Client.Tests.Tutorial;

/// <summary>EP-0028-US-0006-TK-0001: local per-step tutorial events next to the battle report (shown/completed/dismissed/expired/hints).</summary>
[Collection("Battle localization")]
public sealed class TutorialMetricsTests : IDisposable
{
    private readonly string _directory = Path.Combine(Path.GetTempPath(), "dss-tutorial-metrics-" + Guid.NewGuid().ToString("N"));

    public void Dispose()
    {
        if (Directory.Exists(_directory)) Directory.Delete(_directory, recursive: true);
    }

    [Fact]
    public void StepEventsAreWrittenInOrderNextToBattleReport()
    {
        using var locale = Localization.UseLocaleForTests("Russian");
        var session = new TutorialSession(Script(null,
            Step("pick", complete: TutorialCompletion.PilotSelected, priority: 90),
            Step("ack", priority: 80)))
        { MetricsDirectory = _directory };

        session.Observe(Move(now: 0));                        // shown: pick
        session.Observe(Move(now: 10, selected: "vera"));     // completed: pick
        session.Observe(Move(now: 100_000, selected: "vera")); // shown: ack (after the minimum gap)
        Draw(session, now: 100_000);
        var layout = Draw(session, now: 100_000);
        Assert.True(session.OnMouseDown(Center(layout.Rects["tutorial:ack"]), MouseButton.Left)); // ack -> completed

        var metrics = session.Metrics!;
        metrics.Flush(); // written in the background (US-0022-TK-0024)
        string expected = Path.ChangeExtension(TacticalBattleMetrics.ReportPath("battle", _directory), ".tutorial.json");
        Assert.Equal(expected, metrics.FilePath);
        Assert.True(File.Exists(expected));
        Assert.Empty(Directory.GetFiles(_directory, "*.tmp")); // atomic: no temp file left behind

        using var json = JsonDocument.Parse(File.ReadAllText(expected));
        var root = json.RootElement;
        Assert.Equal(1, root.GetProperty("schemaVersion").GetInt32());
        Assert.Equal("battle", root.GetProperty("battleId").GetString());
        Assert.Equal("initial", root.GetProperty("scenarioId").GetString());
        Assert.Equal(1, root.GetProperty("seed").GetInt64());
        Assert.True(root.TryGetProperty("updatedUtc", out _));
        var events = root.GetProperty("events").EnumerateArray()
            .Select(e => (e.GetProperty("step").GetString(), e.GetProperty("kind").GetString(), e.GetProperty("phase").GetString(),
                e.GetProperty("turn").GetInt32(), e.GetProperty("atMs").GetInt64()))
            .ToArray();
        Assert.Equal(new (string?, string?, string?, int, long)[]
        {
            ("pick", "shown", "Move", 1, 0),
            ("pick", "completed", "Move", 1, 10),
            ("ack", "shown", "Move", 1, 100_000),
            ("ack", "completed", "Move", 1, 100_000),
        }, events);

        string text = File.ReadAllText(expected);
        Assert.DoesNotContain(Environment.UserName, text, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(Path.GetTempPath().TrimEnd('\\', '/'), text, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void DismissedAndExpiredStepsAreRecorded()
    {
        using var locale = Localization.UseLocaleForTests("Russian");
        var session = new TutorialSession(Script(null,
            Step("first", complete: TutorialCompletion.PilotSelected, priority: 90),
            Step("second", complete: TutorialCompletion.PilotSelected, priority: 80)))
        { MetricsDirectory = _directory };

        session.Observe(Move(now: 0));
        var layout = Draw(session);
        Assert.True(session.OnMouseDown(Center(layout.Rects["tutorial:ack"]), MouseButton.Left)); // "Got it" on an action step
        session.Observe(Move(now: 100_000));                                                       // second shown
        session.Observe(new("Fire", 1, TutorialTestBattle.Battle(Contracts.TacticalBattlePhase.Fire, 1), null, null, null, 100_100));

        session.Metrics!.Flush();
        Assert.Equal(new[] { "first:shown", "first:dismissed", "second:shown", "second:expired" }, Kinds(session.Metrics.FilePath));
    }

    [Fact]
    public void HintsToggleIsRecorded()
    {
        using var locale = Localization.UseLocaleForTests("English");
        var session = new TutorialSession(Script(null, Step("only", complete: TutorialCompletion.PilotSelected)))
        { MetricsDirectory = _directory, PersistHints = _ => true };

        session.Observe(Move(now: 5_000)); // the window clock is machine uptime; the file keeps time since the first frame
        var layout = Draw(session);
        Assert.True(session.OnMouseDown(Center(layout.Rects["tutorial:hints"]), MouseButton.Left)); // off: dismisses the line
        layout = Draw(session);
        Assert.True(session.OnMouseDown(Center(layout.Rects["tutorial:chip"]), MouseButton.Left));  // on again

        session.Metrics!.Flush();
        Assert.Equal(new[] { "only:shown", "only:dismissed", ":hints-disabled", ":hints-enabled" }, Kinds(session.Metrics.FilePath));
        using var json = JsonDocument.Parse(File.ReadAllText(session.Metrics.FilePath));
        var hints = json.RootElement.GetProperty("events")[2];
        Assert.Equal(JsonValueKind.Null, hints.GetProperty("step").ValueKind);
        Assert.Equal("Move", hints.GetProperty("phase").GetString());
        Assert.Equal(1, hints.GetProperty("turn").GetInt32());
        Assert.Equal(0, hints.GetProperty("atMs").GetInt64());
    }

    [Fact]
    public void WriteFailureIsLoggedAndBattleContinues()
    {
        using var locale = Localization.UseLocaleForTests("Russian");
        Directory.CreateDirectory(_directory);
        string blocked = Path.Combine(_directory, "not-a-directory");
        File.WriteAllText(blocked, "a file where the metrics folder should be");
        var warnings = new List<string>();
        var session = new TutorialSession(Script(null,
            Step("first", complete: TutorialCompletion.PilotSelected, priority: 90),
            Step("second", priority: 80)))
        { MetricsDirectory = blocked, MetricsWarning = warnings.Add };

        session.Observe(Move(now: 0));
        session.Observe(Move(now: 10, selected: "vera"));
        session.Observe(Move(now: 100_000, selected: "vera"));
        Draw(session, now: 100_000); // still draws
        session.Metrics!.Flush();

        Assert.Equal("second", session.Director.Current?.Step.Id); // the director goes on
        var warning = Assert.Single(warnings);                     // one line per battle, not per event
        Assert.StartsWith("TUTORIAL METRICS: ", warning);
        Assert.Equal(3, session.Metrics.Events.Count);            // events are kept in memory regardless

        var noWarning = new TutorialSession(Script(null, Step("only"))) { MetricsDirectory = blocked, MetricsWarning = _ => throw new IOException("log is gone") };
        noWarning.Observe(Move()); // a failing log never escapes either
    }

    [Fact]
    public void AfterFirstWriteFailureNewEventsAreNotWrittenToDisk()
    {
        // US-0022-TK-0024: an unwritable metrics folder is not retried on every later step event.
        using var locale = Localization.UseLocaleForTests("Russian");
        Directory.CreateDirectory(_directory);
        string blocked = Path.Combine(_directory, "not-a-directory");
        File.WriteAllText(blocked, "a file where the metrics folder should be");
        var warnings = new List<string>();
        var session = new TutorialSession(Script(null,
            Step("first", complete: TutorialCompletion.PilotSelected, priority: 90),
            Step("second", priority: 80)))
        { MetricsDirectory = blocked, MetricsWarning = warnings.Add };

        session.Observe(Move(now: 0)); // shown: first -> the write fails
        var metrics = session.Metrics!;
        metrics.Flush();
        Assert.True(metrics.WritesDisabled);

        File.Delete(blocked); // the folder could be created now, but writing stays off for this battle
        session.Observe(Move(now: 10, selected: "vera"));      // completed: first
        session.Observe(Move(now: 100_000, selected: "vera")); // shown: second
        metrics.Flush();

        Assert.Equal(3, metrics.Events.Count); // still recorded in memory
        Assert.False(Directory.Exists(blocked));
        Assert.False(File.Exists(metrics.FilePath));
        Assert.Single(warnings);
    }

    [Fact]
    public void RecordingDoesNoFileIoAndCoalescesPendingEvents()
    {
        // US-0022-TK-0024: a record (called from the render frame) only queues the write; queued events share one write.
        var queued = new List<Action>();
        var metrics = new TutorialMetrics("battle", _directory)
        {
            Background = work => { queued.Add(work); return Task.CompletedTask; },
        };

        metrics.RecordHints(false, "Move", 1, 0);
        metrics.RecordHints(true, "Move", 1, 10);

        Assert.False(Directory.Exists(_directory)); // nothing touched the disk on the calling thread
        var work = Assert.Single(queued);           // the second event joins the pending write
        work();

        Assert.Equal(new[] { ":hints-disabled", ":hints-enabled" }, Kinds(metrics.FilePath));
        Assert.Empty(Directory.GetFiles(_directory, "*.tmp"));
    }

    [Fact]
    public void NoMetricsWithoutDirectory()
    {
        var session = new TutorialSession(Script(null, Step("only")));
        session.Observe(Move());
        Assert.Null(session.Metrics);
    }

    private static string[] Kinds(string path)
    {
        using var json = JsonDocument.Parse(File.ReadAllText(path));
        return json.RootElement.GetProperty("events").EnumerateArray()
            .Select(e => $"{e.GetProperty("step").GetString()}:{e.GetProperty("kind").GetString()}").ToArray();
    }

    private static SKPoint Center(SKRect r) => new(r.MidX, r.MidY);
}
