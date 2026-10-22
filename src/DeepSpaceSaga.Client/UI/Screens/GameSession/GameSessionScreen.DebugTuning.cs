using DeepSpaceSaga.Client.UI.Screens.GameSession.Controls;
using DeepSpaceSaga.Contracts;
using Silk.NET.Input;
using SkiaSharp;

namespace DeepSpaceSaga.Client.UI.Screens.GameSession;

public sealed partial class GameSessionScreen
{
    private readonly DebugTuningPanel _debugTuningPanel = new();
    internal DebugTuningPanel DebugPanel => _debugTuningPanel;
    private readonly List<DebugSubmission> _debugSubmissions = [];
    private sealed record DebugSubmission(DebugTuningValue Value, string CommandId, Task SendTask, long Revision);
    internal string? DebugError { get; private set; }

    private void InitializeDebugTuning()
    {
        if (_handle is not null) _handle.Closing += DisposeDebugTuning;
    }

    internal void DisposeDebugTuning()
    {
        if (_handle is not null) _handle.Closing -= DisposeDebugTuning;
        _debugTuningPanel.Dispose();
    }

    private void RenderDebugTuning(SKCanvas canvas)
    {
        if (!_debugTuningPanel.Visible) return;
        PrepareDebugTuning();
        _debugTuningPanel.Render(canvas);
    }

    internal void PrepareDebugTuning()
    {
        var snapshot = _buffer.Latest?.Snapshot.DebugTuning;
        _debugTuningPanel.Prepare(_uiViewportW, _uiViewportH, snapshot);
        for (int i = _debugSubmissions.Count - 1; i >= 0; i--)
        {
            var submission = _debugSubmissions[i];
            var receipt = _buffer.FindCommandResult(submission.CommandId);
            bool failed = submission.SendTask.IsFaulted || submission.SendTask.IsCanceled;
            if (!failed && (receipt is null || receipt.Status == CommandResultStatus.Deferred ||
                receipt.Status == CommandResultStatus.Executed && snapshot?.Revision <= submission.Revision)) continue;
            if (failed) { _ = submission.SendTask.Exception; DebugError = "Debug.SendFailed"; }
            else DebugError = receipt!.Status == CommandResultStatus.Executed ? null : "Debug." + (receipt.ReasonCode ?? "Rejected");
            _debugSubmissions.RemoveAt(i);
        }
        for (int i = 0; i < _debugTuningPanel.RowCount; i++)
        {
            var row = _debugTuningPanel.GetRow(i);
            double? pending = null;
            for (int j = _debugSubmissions.Count - 1; j >= 0; j--)
                if (DebugTuningPanel.SameKey(row.Value, _debugSubmissions[j].Value)) { pending = _debugSubmissions[j].Value.Value; break; }
            _debugTuningPanel.SetPending(i, pending);
        }
    }

    private bool HandleDebugKey(Key key)
    {
        if (key == CombatInputs.DebugPanel)
        {
            _debugTuningPanel.Visible = !_debugTuningPanel.Visible;
            _isPanningMap = false;
            if (_debugTuningPanel.Visible) PrepareDebugTuning();
            return true;
        }
        if (key == Key.Escape && PointAim is null && _debugTuningPanel.Visible)
        { _debugTuningPanel.Visible = false; return true; }
        return false;
    }

    private bool HandleDebugMouse(float x, float y, MouseButton? button, float wheel = 0)
    {
        float uiX = x / _uiScale, uiY = y / _uiScale;
        if (!_debugTuningPanel.Contains(uiX, uiY)) return false;
        PrepareDebugTuning();
        if (button == MouseButton.Left && _debugTuningPanel.ToggleSection(uiX, uiY)) return true;
        if ((button == MouseButton.Left || float.IsFinite(wheel) && wheel != 0) &&
            _debugTuningPanel.TryEdit(uiX, uiY, Math.Sign(wheel), IsCtrlDown, out int index, out double value))
        {
            if (_handle is not null)
            {
                var entry = _debugTuningPanel.GetRow(index).Value with { Value = value };
                try
                {
                    var send = _handle.SendDebugTuningAsync(new([entry]), out string id).AsTask();
                    _debugSubmissions.Add(new(entry, id, send, _buffer.Latest?.Snapshot.DebugTuning?.Revision ?? 0));
                    _debugTuningPanel.SetPending(index, value);
                }
                catch (Exception) { DebugError = "Debug.SendFailed"; }
            }
        }
        else if (wheel != 0 && float.IsFinite(wheel)) _debugTuningPanel.Scroll(uiX, uiY, wheel);
        return true;
    }
}
