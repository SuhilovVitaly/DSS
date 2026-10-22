using System.Globalization;
using DeepSpaceSaga.Contracts;

namespace DeepSpaceSaga.Engine;

public sealed partial class SimulationEngine
{
    private string? _debugStartupError;

    /// <summary>Bootstrap-only file read, before starting session transport. No live watcher.</summary>
    public void ApplyStartupDebugTuningFile(string path, Action<string>? warning = null)
    {
        if (!File.Exists(path)) return;
        string? reason;
        try
        {
            string json = File.ReadAllText(path);
            var catalog = new DebugTuningState(_catalogRegistry);
            if (DebugTuningFileFormat.TryParse(json, catalog.Snapshot.Defaults, out var patch, out reason) &&
                catalog.TryApply(patch, out var fileState, out reason))
            {
                lock (_worldStateLock)
                {
                    string id;
                    lock (_commandGate)
                    {
                        int sequence = 0;
                        do { id = "debug-tuning.startup." + (sequence++).ToString(CultureInfo.InvariantCulture); }
                        while (_knownCommands.Contains(id));
                    }
                    ReceiveCommand(new(id, 0, "", "", DebugTuningCommandTypes.Apply, DebugTuning: new(fileState.Snapshot.Values)));
                    // Execute the ordinary queue phase without consuming receipts/events
                    // that the first transport snapshot still needs to deliver.
                    ApplyPendingCommands(_clock.Capture().MotionTimeMs);
                    var result = _commandResults.FirstOrDefault(r => r.CommandId == id);
                    if (result?.Status == CommandResultStatus.Executed) { _debugStartupError = null; return; }
                    reason = result?.ReasonCode ?? DebugTuningReasonCodes.SessionNotRunning;
                }
            }
        }
        catch (IOException error) { reason = error.Message; }
        catch (UnauthorizedAccessException error) { reason = error.Message; }
        _debugStartupError = path + ": " + reason;
        try { warning?.Invoke("Debug tuning file rejected: " + _debugStartupError); }
        catch (IOException) { }
        catch (UnauthorizedAccessException) { }
    }
}
