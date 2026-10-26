using System.Threading.Channels;
using System.Diagnostics;

namespace DeepSpaceSaga.Client.UI;

public enum CombatAlertKind { EnemyLaunch, Danger, Urgent, CombatStarted, TorpedoReloadCompleted }
public sealed record CombatAlertCue(CombatAlertKind Kind, int FrequencyHz, int DurationMs, int Pulses, int PulseGapMs, int MinimumIntervalMs = 1500);

internal sealed class CombatAudioPlaybackGate
{
    private long? _launchAt, _warningAt, _combatAt;
    internal bool TryStart(CombatAlertKind kind, long now, long frequency, int minimumIntervalMs)
    {
        if (kind == CombatAlertKind.TorpedoReloadCompleted) return true;
        long? previous = kind switch { CombatAlertKind.EnemyLaunch => _launchAt, CombatAlertKind.CombatStarted => _combatAt, _ => _warningAt };
        if (previous is { } last && (now - last) * (1000d / frequency) < Math.Max(1500, minimumIntervalMs)) return false;
        if (kind == CombatAlertKind.EnemyLaunch) _launchAt = now; else if (kind == CombatAlertKind.CombatStarted) _combatAt = now; else _warningAt = now;
        return true;
    }
}

internal sealed class CombatAudioSessionEpoch
{
    private long _next, _active;
    internal long Activate()
    {
        long epoch = Interlocked.Increment(ref _next); Volatile.Write(ref _active, epoch); return epoch;
    }
    internal void Deactivate(long epoch) => Interlocked.CompareExchange(ref _active, 0, epoch);
    internal bool IsActive(long epoch) => epoch > 0 && Volatile.Read(ref _active) == epoch;
}

/// <summary>Concrete desktop sound path. The UI only enqueues; native tone playback never blocks rendering.</summary>
internal static class CombatAlertAudio
{
    private sealed record QueuedCue(long Epoch, CombatAlertCue Cue);
    private static readonly CombatAudioSessionEpoch Sessions = new();
    private static readonly Channel<QueuedCue> Queue = Channel.CreateBounded<QueuedCue>(new BoundedChannelOptions(128) { SingleReader = true, FullMode = BoundedChannelFullMode.DropOldest });
    private static readonly TaskCompletionSource<bool> Initialized = new(TaskCreationOptions.RunContinuationsAsynchronously);
    internal static Task<bool> Initialization => Initialized.Task;
    private static readonly Thread Worker = StartWorker();
    private static bool _reportedUnavailable;

    internal sealed class Session
    {
        private long _epoch;
        internal void Activate() => _epoch = Sessions.Activate();
        internal void Deactivate() => Sessions.Deactivate(_epoch);
        internal void Play(CombatAlertCue cue)
        { if (Sessions.IsActive(_epoch)) Queue.Writer.TryWrite(new(_epoch, cue)); }
    }
    internal static Session CreateSession() { _ = Worker; return new(); }
    private static Thread StartWorker()
    {
        var thread = new Thread(PlayLoop) { IsBackground = true, Name = "Combat OpenAL" };
        thread.Start();
        return thread;
    }
    private static void PlayLoop()
    {
        OpenAlTonePlayer? player = null;
        var gate = new CombatAudioPlaybackGate();
        try
        {
            player = new OpenAlTonePlayer();
            var settings = CombatVisualSettings.Load(Path.Combine(AppContext.BaseDirectory, CombatVisualSettings.RelativePath)).ThreatAlerts;
            foreach (double frequency in new[] { settings.LaunchToneHz, settings.DangerToneHz, settings.UrgentToneHz })
                player.Prepare(CombatTone.From(new(CombatAlertKind.Danger, (int)Math.Round(frequency), (int)Math.Round(settings.ToneDurationRealMs), 1, 0)));
            Initialized.TrySetResult(true);
            while (Queue.Reader.WaitToReadAsync().AsTask().GetAwaiter().GetResult())
                while (Queue.Reader.TryRead(out var queued))
                {
                    if (!Sessions.IsActive(queued.Epoch)) continue;
                    var cue = queued.Cue;
                    if (!gate.TryStart(cue.Kind, Stopwatch.GetTimestamp(), Stopwatch.Frequency, cue.MinimumIntervalMs)) continue;
                    for (int i = 0; i < Math.Clamp(cue.Pulses, 1, 4) && Sessions.IsActive(queued.Epoch); i++)
                    {
                        player.Play(CombatTone.From(cue), () => Sessions.IsActive(queued.Epoch));
                        if (i + 1 < Math.Clamp(cue.Pulses, 1, 4))
                            for (int delay = Math.Clamp(cue.PulseGapMs, 0, 250); delay > 0 && Sessions.IsActive(queued.Epoch); delay -= 10)
                                Thread.Sleep(Math.Min(delay, 10));
                    }
                }
        }
        catch (Exception error) when (error is not OutOfMemoryException)
        {
            Initialized.TrySetResult(false);
            ReportUnavailable(error.Message);
            player?.Dispose();
            player = null;
            // Drain unavailable-device requests so the UI's queue remains bounded in practice.
            while (Queue.Reader.WaitToReadAsync().AsTask().GetAwaiter().GetResult())
                while (Queue.Reader.TryRead(out _)) { }
        }
        finally { player?.Dispose(); }
    }
    private static void ReportUnavailable(string detail)
    {
        if (_reportedUnavailable) return;
        _reportedUnavailable = true;
        try { InterfaceLog.Write("Combat audio unavailable: " + detail); }
        catch (IOException) { }
        catch (UnauthorizedAccessException) { }
    }
}
