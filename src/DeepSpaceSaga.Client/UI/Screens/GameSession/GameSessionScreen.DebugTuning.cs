using DeepSpaceSaga.Client.UI.Screens.GameSession.Controls;
using SkiaSharp;

namespace DeepSpaceSaga.Client.UI.Screens.GameSession;

public sealed partial class GameSessionScreen
{
    private readonly DebugTuningPanel _debugTuningPanel = new();
    internal DebugTuningPanel DebugPanel => _debugTuningPanel;

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
        _debugTuningPanel.Prepare(_uiViewportW, _uiViewportH, _buffer.Latest?.Snapshot.DebugTuning);
        _debugTuningPanel.Render(canvas);
    }
}
