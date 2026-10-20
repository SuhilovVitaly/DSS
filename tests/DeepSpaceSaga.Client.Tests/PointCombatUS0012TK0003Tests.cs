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
    public void Threat_classes_have_distinct_width_and_dash_in_actual_render()
    {
        using var v = new View(Scene(Torpedo("danger"), Torpedo("near", y: 110), Torpedo("miss", y: 150)));
        var danger = v.Screen.ObservedThreatVisuals["danger"]; var near = v.Screen.ObservedThreatVisuals["near"]; var miss = v.Screen.ObservedThreatVisuals["miss"];
        Assert.Equal(GameSessionScreen.ObservedThreatClass.Dangerous, danger.Class); Assert.Equal(GameSessionScreen.ObservedThreatClass.Near, near.Class);
        Assert.Equal(GameSessionScreen.ObservedThreatClass.Miss, miss.Class);
        Assert.True(danger.WidthPx > near.WidthPx && near.WidthPx > miss.WidthPx);
        Assert.Null(danger.Dash); Assert.NotNull(near.Dash); Assert.NotNull(miss.Dash); Assert.False(near.Dash.SequenceEqual(miss.Dash));
        Assert.NotEqual(danger.Color, near.Color); Assert.NotEqual(near.Color, miss.Color); Assert.Empty(v.Audio);
        v.Capture();
    }

    [Fact]
    public void Threat_alerts_are_rate_limited_and_accessible()
    {
        using var v = new View(Scene());
        v.Show(Scene(Torpedo("a")) with { SnapshotSequence = 2 }, 100); Assert.Single(v.Audio);
        v.Show(Scene(Torpedo("a"), Torpedo("b")) with { SnapshotSequence = 3 }, 1500); Assert.Single(v.Audio);
        v.Show(Scene(Torpedo("a"), Torpedo("b"), Torpedo("c")) with { SnapshotSequence = 4 }, 1600); Assert.Equal(2, v.Audio.Count);
        var urgent = Scene(Torpedo("a", 100)) with { SnapshotSequence = 5 };
        v.Show(urgent, 1700); Assert.Equal(2, v.Audio.Count); // warning kinds share the 1.5 s floor
        v.NowMs = 3100; v.Render(); Assert.Equal(CombatAlertKind.Urgent, v.Audio[^1].Kind); Assert.Equal(3, v.Audio.Count);
        Assert.Equal(2, v.Audio[^1].Pulses);
        v.NowMs = 4599; v.Render(); Assert.Equal(3, v.Audio.Count);
        v.NowMs = 4600; v.Render(); Assert.Equal(4, v.Audio.Count);
        var emitted = v.Screen.RecentAlertSignals.Where(s => s.Kind != CombatAlertKind.EnemyLaunch).ToArray();
        for (int i = 1; i < emitted.Length; i++) Assert.True(emitted[i].RealTimeMs - emitted[i - 1].RealTimeMs >= 1500);
    }

    [Theory]
    [InlineData(false, 3000)]
    [InlineData(true, 1500)]
    public void Existing_danger_resumes_current_cadence_without_replaying_load(bool urgent, long interval)
    {
        using var v = new View(Scene(Torpedo("existing", urgent ? 100 : 200)));
        Assert.Empty(v.Audio); v.NowMs = interval - 1; v.Render(); Assert.Empty(v.Audio);
        v.NowMs = interval; v.Render(); Assert.Single(v.Audio);
        Assert.Equal(urgent ? CombatAlertKind.Urgent : CombatAlertKind.Danger, v.Audio[0].Kind);
    }

    [Fact]
    public void Offscreen_hostile_launch_has_edge_indicator_and_rate_limited_real_flash()
    {
        using var v = new View(Scene());
        var first = Scene(Torpedo("shot-1", 5000)) with { SnapshotSequence = 2, CombatJournal = [Launch(1)] };
        v.Show(first, 100);
        Assert.Single(v.Audio, c => c.Kind == CombatAlertKind.EnemyLaunch); Assert.Equal(1, v.Screen.LaunchFlashCount);
        Assert.True(v.Screen.ThreatOffscreenIndicators.TryGetValue("enemy", out var arrow)); Assert.True(v.Screen.AvailableMapRect().Contains(arrow));
        v.Show(first with { SnapshotSequence = 3, CombatJournal = [Launch(1), Launch(2)] }, 1000);
        Assert.Single(v.Audio, c => c.Kind == CombatAlertKind.EnemyLaunch); Assert.Equal(0, v.Screen.LaunchFlashCount);
        v.Show(first with { SnapshotSequence = 4, CombatJournal = [Launch(1), Launch(2), Launch(3)] }, 1600);
        Assert.Equal(2, v.Audio.Count(c => c.Kind == CombatAlertKind.EnemyLaunch)); Assert.Equal(1, v.Screen.LaunchFlashCount);
        v.NowMs = 2200; v.Render(); Assert.Equal(0, v.Screen.LaunchFlashCount);
    }

    [Fact]
    public void New_load_session_does_not_replay_old_launch_audio_or_flash()
    {
        using var v = new View(Scene()); var scene = Scene() with { SnapshotSequence = 2, CombatJournal = [Launch(10)] };
        v.Show(scene, 100); Assert.Single(v.Audio); Assert.Equal(1, v.Screen.LaunchFlashCount);
        v.Buffer.UpdatePresentation(new(Guid.NewGuid(), 1, 0, [], scene with { SnapshotSequence = 1 }, false, 0));
        v.NowMs = 200; v.Render(); Assert.Single(v.Audio); Assert.Empty(v.Screen.RecentAlertSignals); Assert.Equal(0, v.Screen.LaunchFlashCount);
        v.NowMs = 10000; v.Render(); Assert.Single(v.Audio); Assert.Equal(0, v.Screen.LaunchFlashCount);
    }

    [Fact]
    public void Public_friendly_actor_is_not_reported_as_enemy_launch()
    {
        var scene = Scene(); scene = scene with { Objects = scene.Objects.Select(o => o.ObjectId == "enemy" ? o with { RelationToPlayer = PlayerRelation.Friend } : o).ToImmutableArray() };
        using var v = new View(scene); v.Show(scene with { SnapshotSequence = 2, CombatJournal = [Launch(1)] }, 100);
        Assert.Empty(v.Audio); Assert.Equal(0, v.Screen.LaunchFlashCount); Assert.Empty(v.Screen.ThreatOffscreenIndicators);
    }

    [Fact]
    public void Paused_scene_suppresses_periodic_audio_without_a_backlog()
    {
        using var v = new View(Scene()); v.Show(Scene(Torpedo("a")) with { SnapshotSequence = 2 }, 100); Assert.Single(v.Audio);
        var paused = Scene(Torpedo("a", 100)) with { SnapshotSequence = 3, CurrentSpeed = SimulationSpeed.Speed0 };
        v.Show(paused, 3000); v.NowMs = 20000; v.Render(); Assert.Single(v.Audio);
        v.Show(paused with { SnapshotSequence = 4, CurrentSpeed = SimulationSpeed.Speed1 }, 20100);
        Assert.Equal(2, v.Audio.Count); v.Render(); Assert.Equal(2, v.Audio.Count);
    }

    [Fact]
    public void Audio_device_failure_does_not_break_visual_alerts()
    {
        using var v = new View(Scene(), brokenAudio: true);
        v.Show(Scene() with { SnapshotSequence = 2, CombatJournal = [Launch(1)] }, 100);
        Assert.Single(v.Screen.RecentAlertSignals); Assert.Equal(1, v.Screen.LaunchFlashCount);
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

    [Fact]
    public void Alert_styles_load_from_source_and_deployed_data_and_legacy_fallback()
    {
        var source = CombatVisualSettings.Load(Palette); var packaged = CombatVisualSettings.Load(Path.Combine(AppContext.BaseDirectory, CombatVisualSettings.RelativePath));
        Assert.Equal(source.ThreatAlerts, packaged.ThreatAlerts); Assert.Equal(1500, source.ThreatAlerts.MinimumSignalIntervalRealMs);
        string file = Path.GetTempFileName();
        try
        {
            var json = JsonNode.Parse(File.ReadAllText(Palette))!.AsObject(); json.Remove("threatAlerts"); File.WriteAllText(file, json.ToJsonString());
            Assert.Equal(ThreatAlertVisualSettings.Default, CombatVisualSettings.Load(file).ThreatAlerts);
            using var v = new View(Scene(), source with { ThreatAlerts = source.ThreatAlerts with { SoundEnabled = false } });
            v.Show(Scene() with { SnapshotSequence = 2, CombatJournal = [Launch(1)] }, 100); Assert.Empty(v.Audio); Assert.Equal(1, v.Screen.LaunchFlashCount);
        }
        finally { File.Delete(file); }
    }

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

    [Fact]
    public async Task Actual_hostile_launch_emits_audio_and_flash_only_after_scene_admission()
    {
        await using var world = new PointCombatSessionFixture(engineEnabled: true, launchHostile: false, enemyX: 5000);
        long tick = 0; var buffer = new SnapshotBuffer(() => tick); await using var handle = new GameSessionHandle(world.Connection, buffer);
        var audio = new List<CombatAlertCue>();
        var screen = new GameSessionScreen(buffer, new LinearMotionPredictor(), handle, timestampProvider: () => tick, alertAudio: audio.Add);
        using var bitmap = new SKBitmap(1920, 1200); using var canvas = new SKCanvas(bitmap);
        using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(12));
        async Task Wait(Func<bool> condition, bool render)
        { while (true) { if (render) screen.Render(canvas, 1920, 1200); Assert.Null(handle.Failure); if (condition()) return; await Task.Delay(10, deadline.Token); } }
        await Wait(() => screen.RenderStates.Any(o => o.IsPlayerShip), true);
        await handle.SetSpeedAsync(SimulationSpeed.Speed1);
        Assert.Equal(DeepSpaceSaga.Engine.CommandStartDisposition.Started,
            world.Engine.TryStartHostilePointLaunch(PointCombatSessionFixture.Enemy, "MOD-PIRATE-TORPEDO-01", 5600, 0, "slow", 0).Disposition);
        await Wait(() => buffer.Latest?.Snapshot.Objects.Any(o => o.ObservedProjectile is not null) == true, false);
        Assert.DoesNotContain(buffer.Presented!.Snapshot.Objects, o => o.ObservedProjectile is not null); Assert.Empty(audio); Assert.Equal(0, screen.LaunchFlashCount);
        tick = Stopwatch.Frequency * 150 / 1000; screen.Render(canvas, 1920, 1200);
        Assert.Contains(buffer.Presented.Snapshot.Objects, o => o.ObservedProjectile is not null);
        var cue = Assert.Single(audio); Assert.Equal(CombatAlertKind.EnemyLaunch, cue.Kind); Assert.Equal(880, cue.FrequencyHz); Assert.Equal(1, cue.Pulses);
        Assert.Equal(1, screen.LaunchFlashCount); Assert.Contains(PointCombatSessionFixture.Enemy, screen.ThreatOffscreenIndicators.Keys);
        screen.OnDeactivated();
    }
}
