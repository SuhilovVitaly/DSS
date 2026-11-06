using System.Collections.Immutable;
using System.Diagnostics;
using System.Text.Json.Nodes;
using DeepSpaceSaga.Client.UI;
using DeepSpaceSaga.Client.UI.Screens.GameSession;
using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Motion;
using SkiaSharp;

namespace DeepSpaceSaga.Client.Tests;

public sealed class PointCombatUS0012TK0003Tests
{
    private static ObjectMotionSnapshot Player => new("player", 0, 0, 0, 0, MaxSpeedKmS: 6, RenderObjectType: SpaceObjectType.PlayerShip);
    private static ObjectMotionSnapshot Enemy => new("enemy", 5000, 0, 0, 0, RenderObjectType: SpaceObjectType.NpcShip, RelationToPlayer: PlayerRelation.Enemy);
    private static ObjectMotionSnapshot Torpedo(string id, double x = 200, double y = 0) => new(id, x, y, 6, 270,
        RenderObjectType: SpaceObjectType.Missile, ObservedProjectile: new(ProjectileKind.Torpedo, x, y, -1, 0, 6, "slow", 9));
    private static AuthoritativeSnapshot Scene(params ObjectMotionSnapshot[] shots) => new(1, 0, SimulationSpeed.Speed1,
        new[] { Player, Enemy }.Concat(shots).ToImmutableArray(), "player", CombatJournal: [], Encounter: new(CombatEncounterState.Active, 0, null, new()));
    private static CombatJournalEntry Launch(long id) => new(id, 0, CombatEventType.Launch, "enemy", "", "shot-" + id, 5000, 0,
        ProjectileKind: ProjectileKind.Torpedo, ProfileId: "slow", BlastRadiusKm: 9);

    private sealed class View : IDisposable
    {
        internal readonly SnapshotBuffer Buffer = new(() => 0);
        internal readonly GameSessionScreen Screen;
        internal readonly List<CombatAlertCue> Audio = [];
        internal long NowMs;
        private readonly SKBitmap _bitmap = new(1920, 1200);
        private readonly SKCanvas _canvas;
        internal View(AuthoritativeSnapshot initial, CombatVisualSettings? settings = null, bool brokenAudio = false)
        {
            Buffer.Update(initial);
            Screen = new(Buffer, new LinearMotionPredictor(), timestampProvider: () => NowMs * Stopwatch.Frequency / 1000,
                combatSettings: settings, alertAudio: cue => { if (brokenAudio) throw new InvalidOperationException("test audio device failure"); Audio.Add(cue); });
            _canvas = new(_bitmap); Render(); Render();
        }
        internal void Render() => Screen.Render(_canvas, 1920, 1200);
        internal void Show(AuthoritativeSnapshot scene, long time)
        { NowMs = time; Buffer.Update(scene); Render(); }
        internal void Capture()
        {
            if (Environment.GetEnvironmentVariable("DSS_POINT_RENDER_ARTIFACT_DIRECTORY") is not { Length: > 0 } directory) return;
            Directory.CreateDirectory(directory); using var image = SKImage.FromBitmap(_bitmap); using var data = image.Encode(SKEncodedImageFormat.Png, 100);
            using var output = File.Create(Path.Combine(directory, "threat-alerts-headless.png")); data.SaveTo(output);
        }
        public void Dispose() { Screen.OnDeactivated(); _canvas.Dispose(); _bitmap.Dispose(); }
    }

    [Fact]
    public void Native_audio_gate_limits_actual_playback_start_after_queue_delay()
    {
        var gate = new CombatAudioPlaybackGate();
        Assert.True(gate.TryStart(CombatAlertKind.EnemyLaunch, 100, 1000, 1500));
        Assert.False(gate.TryStart(CombatAlertKind.EnemyLaunch, 1500, 1000, 1500)); // queued 1500ms apart, actual starts only 1400ms apart
        Assert.True(gate.TryStart(CombatAlertKind.EnemyLaunch, 1600, 1000, 1500));
        Assert.True(gate.TryStart(CombatAlertKind.Danger, 200, 1000, 1500));
        Assert.False(gate.TryStart(CombatAlertKind.Urgent, 1600, 1000, 1500));
        Assert.True(gate.TryStart(CombatAlertKind.Urgent, 1700, 1000, 1500));
    }

    [Fact]
    public void Native_audio_session_change_discards_queued_old_and_reactivated_screen_cues()
    {
        var sessions = new CombatAudioSessionEpoch();
        long old = sessions.Activate(); Assert.True(sessions.IsActive(old));
        sessions.Deactivate(old); Assert.False(sessions.IsActive(old));
        long loaded = sessions.Activate(); Assert.True(sessions.IsActive(loaded)); Assert.False(sessions.IsActive(old));
        sessions.Deactivate(old); Assert.True(sessions.IsActive(loaded)); // old cleanup cannot mute the newly loaded screen
        long resumed = sessions.Activate(); Assert.False(sessions.IsActive(loaded)); Assert.True(sessions.IsActive(resumed));
    }

    private static string Palette => Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "src", "DeepSpaceSaga.Client", CombatVisualSettings.RelativePath));

    [Theory]
    [InlineData("minimumSignalIntervalRealMs", "1000")]
    [InlineData("dangerWidthMultiplier", "1")]
    [InlineData("urgentSignalIntervalRealMs", "4000")]
    [InlineData("soundEnabled", "text")]
    [InlineData("urgentPulseCount", "2.5")]
    [InlineData("toneDurationRealMs", "500")]
    [InlineData("unknownField", "1")]
    [InlineData("launchToneHz", "missing")]
    [InlineData("missDotLengthPx", "sameDash")]
    public void Invalid_alert_configuration_reports_file_and_group(string field, string value)
    {
        string file = Path.GetTempFileName();
        try
        {
            var json = JsonNode.Parse(File.ReadAllText(Palette))!; var group = json["threatAlerts"]!.AsObject();
            if (value == "missing") group.Remove(field);
            else if (value == "text") group[field] = "invalid";
            else if (value == "sameDash") { group[field] = 6; group["missDashGapPx"] = 4; }
            else group[field] = double.Parse(value, System.Globalization.CultureInfo.InvariantCulture);
            File.WriteAllText(file, json.ToJsonString());
            var error = Assert.Throws<InvalidDataException>(() => CombatVisualSettings.Load(file)); Assert.Contains(file, error.Message); Assert.Contains("threatAlerts", error.Message);
        }
        finally { File.Delete(file); }
    }
}
