using DeepSpaceSaga.Contracts;

namespace DeepSpaceSaga.Engine;

public sealed partial class SimulationEngine
{
    private IReadOnlyList<DebugTuningValue>? CaptureDebugTuningOverlay()
    {
        var snapshot = _debugTuning.Snapshot;
        var changed = snapshot.Values.Where((value, i) => value.Value != snapshot.Defaults[i].Value).ToArray();
        return changed.Length == 0 ? null : changed;
    }
}
