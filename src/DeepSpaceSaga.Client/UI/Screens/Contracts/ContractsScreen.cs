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

    /// <summary>
    /// Real-time (Environment.TickCount64) timestamp the pointer first entered the
    /// food-rations readout, or null while not hovering it — the tooltip only appears
    /// once MenuStyle.TooltipHoverDelaySeconds has elapsed since this moment (checked in
    /// Render every frame, since OnMouseMove alone would never fire again while the
    /// pointer sits still).
    /// </summary>
    private long? _foodRationsHoverStartedAtMs;

    /// <summary>Test seam — true once the food-rations hover delay has elapsed.</summary>
    internal bool IsFoodRationsTooltipVisible =>
        _foodRationsHoverStartedAtMs is { } startedAtMs
        && Environment.TickCount64 - startedAtMs >= MenuStyle.TooltipHoverDelaySeconds * 1000;

    /// <summary>Same real-time hover-delay tracking as <see cref="_foodRationsHoverStartedAtMs"/>, for the crew readout.</summary>
    private long? _crewHoverStartedAtMs;

    /// <summary>Test seam — true once the crew-readout hover delay has elapsed.</summary>
    internal bool IsCrewTooltipVisible =>
        _crewHoverStartedAtMs is { } startedAtMs
        && Environment.TickCount64 - startedAtMs >= MenuStyle.TooltipHoverDelaySeconds * 1000;

    /// <summary>Same real-time hover-delay tracking as <see cref="_foodRationsHoverStartedAtMs"/>, for the tokens readout.</summary>
    private long? _tokensHoverStartedAtMs;

    /// <summary>Test seam — true once the tokens-readout hover delay has elapsed.</summary>
    internal bool IsTokensTooltipVisible =>
        _tokensHoverStartedAtMs is { } startedAtMs
        && Environment.TickCount64 - startedAtMs >= MenuStyle.TooltipHoverDelaySeconds * 1000;

    /// <summary>Same real-time hover-delay tracking as <see cref="_foodRationsHoverStartedAtMs"/>, for the fuel readout.</summary>
    private long? _fuelHoverStartedAtMs;

    /// <summary>Test seam — true once the fuel-readout hover delay has elapsed.</summary>
    internal bool IsFuelTooltipVisible =>
        _fuelHoverStartedAtMs is { } startedAtMs
        && Environment.TickCount64 - startedAtMs >= MenuStyle.TooltipHoverDelaySeconds * 1000;

    private const string PlaceholderLine = "Contracts: not available yet";

    public ContractsScreen(SnapshotBuffer? buffer = null)
    {
        _buffer = buffer;
    }

    public void OnActivated()
    {
        _isStationNameHovered = false;
        _isExitButtonHovered = false;
        _foodRationsHoverStartedAtMs = null;
        _crewHoverStartedAtMs = null;
        _tokensHoverStartedAtMs = null;
        _fuelHoverStartedAtMs = null;
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

        // Not a button — hovering it only shows a delayed tooltip (see Render), so it must
        // not affect the interactive-cursor swap the way the name link / exit button do.
        if (IsFoodRationsHit(x, y))
            _foodRationsHoverStartedAtMs ??= Environment.TickCount64;
        else
            _foodRationsHoverStartedAtMs = null;

        if (IsCrewHit(x, y))
            _crewHoverStartedAtMs ??= Environment.TickCount64;
        else
            _crewHoverStartedAtMs = null;

        if (IsTokensHit(x, y))
            _tokensHoverStartedAtMs ??= Environment.TickCount64;
        else
            _tokensHoverStartedAtMs = null;

        if (IsFuelHit(x, y))
            _fuelHoverStartedAtMs ??= Environment.TickCount64;
        else
            _fuelHoverStartedAtMs = null;

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
            foodRationsCount: StationToolbar.ResolveFoodRationsCount(snapshot),
            crewCount: StationToolbar.ResolveCrewCount(snapshot),
            cabinsCount: StationToolbar.ResolveCabinsCount(snapshot),
            creditsCount: StationToolbar.ResolveCreditsCount(snapshot),
            fuelAmountKg: StationToolbar.ResolveFuelAmountKg(snapshot),
            fuelCapacityKg: StationToolbar.ResolveFuelCapacityKg(snapshot));

        float cx = pl + ContractsLayout.PanelWidth / 2f;
        canvas.DrawText(PlaceholderLine, cx, pt + ContractsLayout.BodyStartY, MenuStyle.TextStatus);

        // Drawn last: the tooltip hangs below the toolbar into the body area and must
        // stay on top of everything the screen drew.
        StationToolbar.DrawTooltips(canvas, pl, pt,
            isFoodRationsHovered: IsFoodRationsTooltipVisible,
            isCrewHovered: IsCrewTooltipVisible,
            isTokensHovered: IsTokensTooltipVisible,
            isFuelHovered: IsFuelTooltipVisible);
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

    /// <summary>True when (x, y) lands on the toolbar's food-rations readout (see StationToolbar).</summary>
    private bool IsFoodRationsHit(float x, float y)
    {
        float pl = ContractsLayout.PanelLeft(_screenWidth);
        float pt = ContractsLayout.PanelTop(_screenHeight);
        var local = StationToolbar.FoodRationsLocalRect();
        return x >= pl + local.Left && x <= pl + local.Right && y >= pt + local.Top && y <= pt + local.Bottom;
    }

    /// <summary>True when (x, y) lands on the toolbar's crew readout (see StationToolbar).</summary>
    private bool IsCrewHit(float x, float y)
    {
        float pl = ContractsLayout.PanelLeft(_screenWidth);
        float pt = ContractsLayout.PanelTop(_screenHeight);
        var local = StationToolbar.CrewLocalRect();
        return x >= pl + local.Left && x <= pl + local.Right && y >= pt + local.Top && y <= pt + local.Bottom;
    }

    /// <summary>True when (x, y) lands on the toolbar's tokens/credits readout (see StationToolbar).</summary>
    private bool IsTokensHit(float x, float y)
    {
        float pl = ContractsLayout.PanelLeft(_screenWidth);
        float pt = ContractsLayout.PanelTop(_screenHeight);
        var local = StationToolbar.TokensLocalRect();
        return x >= pl + local.Left && x <= pl + local.Right && y >= pt + local.Top && y <= pt + local.Bottom;
    }

    /// <summary>True when (x, y) lands on the toolbar's fuel readout (see StationToolbar).</summary>
    private bool IsFuelHit(float x, float y)
    {
        float pl = ContractsLayout.PanelLeft(_screenWidth);
        float pt = ContractsLayout.PanelTop(_screenHeight);
        var local = StationToolbar.FuelLocalRect();
        return x >= pl + local.Left && x <= pl + local.Right && y >= pt + local.Top && y <= pt + local.Bottom;
    }
}
