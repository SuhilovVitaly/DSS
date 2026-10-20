using System.Threading.Channels;
using System.Diagnostics;

namespace DeepSpaceSaga.Client.UI;

public enum CombatAlertKind { EnemyLaunch, Danger, Urgent, CombatStarted }
public sealed record CombatAlertCue(CombatAlertKind Kind, int FrequencyHz, int DurationMs, int Pulses, int PulseGapMs, int MinimumIntervalMs = 1500);

internal sealed class CombatAudioPlaybackGate
{
    private long? _launchAt, _warningAt, _combatAt;
    internal bool TryStart(CombatAlertKind kind, long now, long frequency, int minimumIntervalMs)
    {
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
    private static readonly Channel<QueuedCue> Queue = Channel.CreateBounded<QueuedCue>(new BoundedChannelOptions(4)
    { SingleReader = true, FullMode = BoundedChannelFullMode.DropOldest });
    private static readonly Task Worker = Task.Run(PlayLoop);
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
    private static async Task PlayLoop()
    {
        var gate = new CombatAudioPlaybackGate();
        await foreach (var queued in Queue.Reader.ReadAllAsync())
        {
            if (!Sessions.IsActive(queued.Epoch)) continue;
            var cue = queued.Cue;
            if (!OperatingSystem.IsWindows()) { ReportUnavailable("Native tone playback requires Windows"); continue; }
            if (!gate.TryStart(cue.Kind, Stopwatch.GetTimestamp(), Stopwatch.Frequency, cue.MinimumIntervalMs)) continue;
            try
            {
                for (int i = 0; i < Math.Clamp(cue.Pulses, 1, 4); i++)
                {
                    if (!Sessions.IsActive(queued.Epoch)) break;
                    Console.Beep(Math.Clamp(cue.FrequencyHz, 37, 32767), Math.Clamp(cue.DurationMs, 1, 250));
                    if (i + 1 < cue.Pulses) await Task.Delay(Math.Clamp(cue.PulseGapMs, 0, 250));
                }
            }
            catch (Exception error) when (error is PlatformNotSupportedException or IOException or InvalidOperationException or System.ComponentModel.Win32Exception)
            { ReportUnavailable(error.Message); }
        }
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
