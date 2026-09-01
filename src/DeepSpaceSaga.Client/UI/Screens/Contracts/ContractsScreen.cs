using DeepSpaceSaga.Client.UI.Controls;
using DeepSpaceSaga.Client.UI.Screens;
using Silk.NET.Input;
using SkiaSharp;

namespace DeepSpaceSaga.Client.UI.Screens.Contracts;

/// <summary>
/// Contracts overlay (Docs/FirstRelease/Screens/Contracts.md). Placeholder shell only —
/// passenger contracts are not implemented yet, so the panel just shows a single "not
/// available yet" line instead of live contract data. Split out of the original `Hire`
/// screen (which now covers crew hiring specifically — Docs/FirstRelease/Screens/Hire.md)
/// so passenger contracts have their own screen. Opened from
/// <see cref="Station.StationScreen"/>'s `CONTRACTS` button (ScreenEvent.OpenContracts) as
/// a nested modal on top of it; closes via the toolbar's exit-button icon (see
/// StationToolbar), Escape, or a click outside the panel (on the dimmed background),
/// returning to <see cref="Station.StationScreen"/>. Pause-on-open/resume-on-close is
/// handled generically by SkiaWindow's PushModalAsync/PopModalAsync — this screen has no
/// speed/pause logic of its own. Structural twin of <see cref="Hire.HireScreen"/>/
/// <see cref="Trade.TradeScreen"/>/<see cref="Station.StationScreen"/>.
/// </summary>
public sealed class ContractsScreen : IScreen
{
    private readonly SnapshotBuffer? _buffer;

    private int _screenWidth;
    private int _screenHeight;
    private bool _isStationNameHovered;
    private bool _isExitButtonHovered;

    private const string PlaceholderLine = "Contracts: not available yet";

    public ContractsScreen(SnapshotBuffer? buffer = null)
    {
        _buffer = buffer;
    }

    public void OnActivated()
    {
        _isStationNameHovered = false;
        _isExitButtonHovered = false;
    }

    public void OnDeactivated() { }

    public ScreenEvent OnKeyDown(Key key) =>
        key == Key.Escape ? ScreenEvent.CloseContracts : ScreenEvent.None;

    public ScreenEvent OnMouseDown(float x, float y, MouseButton button)
    {
        if (button != MouseButton.Left)
            return ScreenEvent.None;

        if (IsExitButtonHit(x, y))
            return ScreenEvent.CloseContracts;

        if (IsStationNameHit(x, y))
            return ScreenEvent.NavigateToStation;

        // Click on the dimmed background outside the panel also closes it.
        if (!ContractsLayout.IsInsidePanel(x, y, _screenWidth, _screenHeight))
            return ScreenEvent.CloseContracts;

        return ScreenEvent.None;
    }

    /// <summary>Convenience shortcut for a left click — kept for existing call-site/test conventions.</summary>
    public ScreenEvent OnMouseDown(float x, float y) => OnMouseDown(x, y, MouseButton.Left);

    public bool OnMouseMove(float x, float y)
    {
        _isStationNameHovered = IsStationNameHit(x, y);
        _isExitButtonHovered = IsExitButtonHit(x, y);
        return _isStationNameHovered || _isExitButtonHovered;
    }

    public ScreenEvent OnMouseWheel(float x, float y, float delta) => ScreenEvent.None;

    public void Render(SKCanvas canvas, int width, int height)
    {
        _screenWidth = width;
        _screenHeight = height;

        float pl = ContractsLayout.PanelLeft(width);
        float pt = ContractsLayout.PanelTop(height);
        var panelRect = new SKRect(pl, pt, pl + ContractsLayout.PanelWidth, pt + ContractsLayout.PanelHeight);
        MenuStyle.DrawPanel(canvas, panelRect);

        var snapshot = _buffer?.Latest?.Snapshot;
        string? stationName = StationToolbar.ResolveDockedStationName(snapshot);
        StationToolbar.Draw(canvas, pl, pt, stationName, isStationHub: false, isHovered: _isStationNameHovered,
            windowName: "CONTRACTS", isExitButtonHovered: _isExitButtonHovered,
            foodRationsCount: StationToolbar.ResolveFoodRationsCount(snapshot));

        float cx = pl + ContractsLayout.PanelWidth / 2f;
        canvas.DrawText(PlaceholderLine, cx, pt + ContractsLayout.BodyStartY, MenuStyle.TextStatus);
    }

    /// <summary>True when (x, y) lands on the toolbar's station-name link (see StationToolbar).</summary>
    private bool IsStationNameHit(float x, float y)
    {
        string? stationName = StationToolbar.ResolveDockedStationName(_buffer?.Latest?.Snapshot);
        if (string.IsNullOrEmpty(stationName))
            return false;

        float pl = ContractsLayout.PanelLeft(_screenWidth);
        float pt = ContractsLayout.PanelTop(_screenHeight);
        var local = StationToolbar.NameLocalRect(stationName);
        return x >= pl + local.Left && x <= pl + local.Right && y >= pt + local.Top && y <= pt + local.Bottom;
    }

    /// <summary>True when (x, y) lands on the toolbar's exit-button icon (see StationToolbar).</summary>
    private bool IsExitButtonHit(float x, float y)
    {
        float pl = ContractsLayout.PanelLeft(_screenWidth);
        float pt = ContractsLayout.PanelTop(_screenHeight);
        var local = StationToolbar.ExitButtonLocalRect();
        return x >= pl + local.Left && x <= pl + local.Right && y >= pt + local.Top && y <= pt + local.Bottom;
    }
}
