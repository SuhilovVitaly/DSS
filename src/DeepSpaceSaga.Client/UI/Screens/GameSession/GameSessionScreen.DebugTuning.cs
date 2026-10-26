using DeepSpaceSaga.Client.UI.Screens.GameSession.Controls;
using DeepSpaceSaga.Contracts;
using System.Diagnostics;
using Silk.NET.Input;
using SkiaSharp;

namespace DeepSpaceSaga.Client.UI.Screens.GameSession;

public sealed partial class GameSessionScreen
{
    private readonly DebugTuningPanel _debugTuningPanel = new();
    internal DebugTuningPanel DebugPanel => _debugTuningPanel;
    private readonly List<DebugSubmission> _debugSubmissions = [];
    private sealed record DebugSubmission(DebugTuningValue Value, string CommandId, Task SendTask, long Revision, long Order);
    private long _debugSubmissionOrder, _debugFeedbackOrder;
    internal string? DebugError { get; private set; }
    private Task<string?>? _debugFolderOpen;
    private string? _debugFolderPath;
    private long? _debugResetArmedAt;
    private bool _debugStartupDismissed;
    internal Action<string>? DebugOpenFolderForTests { get; set; }

    private void InitializeDebugTuning()
    {
        _debugTuningPanel.ConfigureBindings(CombatInputs);
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
        if (_debugFolderOpen is { IsCompletedSuccessfully: true } completed)
        {
            DebugError = completed.Result is null ? null : "Debug.OpenFolderFailed";
            _debugFolderOpen = null;
        }
        var snapshot = _buffer.Latest?.Snapshot.DebugTuning;
        _debugTuningPanel.Prepare(_uiViewportW, _uiViewportH, snapshot);
        for (int i = _debugSubmissions.Count - 1; i >= 0; i--)
        {
            var submission = _debugSubmissions[i];
            var receipt = _buffer.FindCommandResult(submission.CommandId);
            bool failed = submission.SendTask.IsFaulted || submission.SendTask.IsCanceled;
            if (!failed && (receipt is null || receipt.Status == CommandResultStatus.Deferred ||
                receipt.Status == CommandResultStatus.Executed && snapshot?.Revision <= submission.Revision)) continue;
            if (failed) _ = submission.SendTask.Exception;
            if (submission.Order >= _debugFeedbackOrder)
            {
                _debugFeedbackOrder = submission.Order;
                DebugError = failed ? "Debug.SendFailed" : receipt!.Status == CommandResultStatus.Executed ? null : "Debug." + (receipt.ReasonCode ?? "Rejected");
            }
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
        if (_debugResetArmedAt is { } armed && (_timestampProvider() - armed) / (double)Stopwatch.Frequency > 1) _debugResetArmedAt = null;
        _debugTuningPanel.SetFeedback(DebugError, _debugStartupDismissed ? null : snapshot?.StartupError,
            _debugSubmissions.Count != 0, _debugResetArmedAt is not null, _handle?.DebugTuningFiles.Status, snapshot?.Revision ?? 0, DebugError == "Debug.OpenFolderFailed" ? _debugFolderPath : null);
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
        { _debugTuningPanel.Visible = false; _debugResetArmedAt = null; return true; }
        return false;
    }

    private bool HandleDebugMouse(float x, float y, MouseButton? button, float wheel = 0)
    {
        float uiX = x / _uiScale, uiY = y / _uiScale;
        if (!_debugTuningPanel.Contains(uiX, uiY)) return false;
        PrepareDebugTuning();
        if (button == MouseButton.Left)
        {
            if (_debugTuningPanel.ToggleHelp(uiX, uiY)) return true;
            if (_debugTuningPanel.FolderBounds.Contains(uiX, uiY))
            {
                _debugFolderPath = _handle?.DebugTuningFiles.DirectoryPath ?? Path.Combine(AppContext.BaseDirectory, "DebugTuning");
                if (_debugFolderOpen is not null) return true;
                if (DebugOpenFolderForTests is { } open)
                {
                    try { Directory.CreateDirectory(_debugFolderPath); open(_debugFolderPath); DebugError = null; }
                    catch (Exception error) when (error is IOException or UnauthorizedAccessException or System.ComponentModel.Win32Exception)
                    {
                        DebugError = "Debug.OpenFolderFailed";
                        try { InterfaceLog.Write($"Could not open folder '{_debugFolderPath}': {error.Message}"); }
                        catch (Exception logError) when (logError is IOException or UnauthorizedAccessException) { }
                    }
                }
                else _debugFolderOpen = ShellLauncher.OpenDirectory(_debugFolderPath);
                PrepareDebugTuning();
                return true;
            }
            if (_debugTuningPanel.ResetAllBounds.Contains(uiX, uiY))
            {
                if (_debugResetArmedAt is null) { _debugResetArmedAt = _timestampProvider(); PrepareDebugTuning(); return true; }
                _debugResetArmedAt = null;
                if (_handle is not null && _buffer.Latest?.Snapshot.DebugTuning is { } tuning)
                {
                    try
                    {
                        var send = _handle.SendDebugTuningAsync(new(tuning.Defaults), out string id, resetAll: true).AsTask();
                        foreach (var entry in tuning.Defaults) _debugSubmissions.Add(new(entry, id, send, tuning.Revision, ++_debugSubmissionOrder));
                        _debugFeedbackOrder = _debugSubmissionOrder;
                        _debugStartupDismissed = true; DebugError = null;
                    }
                    catch (Exception) { DebugError = "Debug.SendFailed"; }
                }
                PrepareDebugTuning(); return true;
            }
        }
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
                    _debugSubmissions.Add(new(entry, id, send, _buffer.Latest?.Snapshot.DebugTuning?.Revision ?? 0, ++_debugSubmissionOrder));
                    _debugFeedbackOrder = _debugSubmissionOrder;
                    _debugStartupDismissed = true; DebugError = null;
                    _debugTuningPanel.SetPending(index, value);
                }
                catch (Exception) { DebugError = "Debug.SendFailed"; }
            }
        }
        else if (wheel != 0 && float.IsFinite(wheel)) _debugTuningPanel.Scroll(uiX, uiY, wheel);
        return true;
    }
}
