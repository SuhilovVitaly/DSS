using DeepSpaceSaga.Client.UI.Controls;
using DeepSpaceSaga.Client.UI.Screens;
using Silk.NET.Input;
using SkiaSharp;

namespace DeepSpaceSaga.Client.UI.Screens.Station;

/// <summary>
/// Station overlay (Docs/FirstRelease/Screens/Station.md). Placeholder shell:
/// Representatives/Install Drilling Unit/Undock are not yet implemented, so the
/// panel shows a "not available yet" line for each of them. `Trade`, `Hire`,
/// `Finance` and `Contracts` are real buttons — `Trade`/`Hire`/`Contracts` open
/// <see cref="Trade.TradeScreen"/>/<see cref="Hire.HireScreen"/>/
/// <see cref="Contracts.ContractsScreen"/> (stubs, same open/close/pause-only shell
/// as this screen; `Contracts` was split out of `Hire` — passenger contracts vs.
/// crew hiring); `Finance` opens the pre-existing <see cref="Finance.FinanceScreen"/>
/// (already reachable from GameSessionScreen's Mechanics panel / Ctrl+F) — all four
/// as a nested modal on top of this one. Opened from GameSessionScreen by
/// left-clicking the station the player ship is currently docked to
/// (ScreenEvent.OpenStation); closes via the toolbar's exit-button icon (see
/// StationToolbar), Escape, or a click outside the panel (on the dimmed background),
/// returning to GameSessionScreen while the docked state itself is untouched — clicking
/// the station again reopens this
/// screen. Pause-on-open/resume-on-close is handled generically by SkiaWindow's
/// PushModalAsync/PopModalAsync — this screen has no speed/pause logic of its own.
/// Structural twin of <see cref="Finance.FinanceScreen"/>.
/// </summary>
public sealed class StationScreen : IScreen
{
    private readonly SnapshotBuffer? _buffer;

    private int _screenWidth;
    private int _screenHeight;
    private StationButton _hoveredButton = StationButton.None;
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

    public StationScreen(SnapshotBuffer? buffer = null)
    {
        _buffer = buffer;
    }

    /// <summary>
    /// Remaining not-yet-implemented lines, tagged with the body row they occupy
    /// (Station.md's "Минимальные кнопки" order: Trade=0, Finance=1,
    /// Representatives=2, Install Drilling Unit=3, Hire=4, Undock=5; Contracts=6 is a
    /// newly appended row, not one of these original six) — rows already converted to
    /// real buttons (Trade, Finance, Hire) are simply absent here, so the remaining
    /// lines keep their original row instead of repacking upward.
    /// </summary>
    private static readonly (int Row, string Text)[] PlaceholderLines =
    {
        (2, "Representatives: not available yet"),
        (3, "Install Drilling Unit: not available yet"),
        (5, "Undock: not available yet"),
    };

    public void OnActivated()
    {
        _hoveredButton = StationButton.None;
        _isExitButtonHovered = false;
        _foodRationsHoverStartedAtMs = null;
        _crewHoverStartedAtMs = null;
        _tokensHoverStartedAtMs = null;
        _fuelHoverStartedAtMs = null;
    }

    public void OnDeactivated() { }

    public ScreenEvent OnKeyDown(Key key) =>
        key == Key.Escape ? ScreenEvent.CloseStation : ScreenEvent.None;

    public ScreenEvent OnMouseDown(float x, float y, MouseButton button)
    {
        if (button != MouseButton.Left)
            return ScreenEvent.None;

        var hit = StationLayout.HitTest(x, y, _screenWidth, _screenHeight);
        if (hit == StationButton.Trade)
            return ScreenEvent.OpenTrade;
        if (hit == StationButton.Hire)
            return ScreenEvent.OpenHire;
        if (hit == StationButton.Finance)
            return ScreenEvent.OpenFinance;
        if (hit == StationButton.Contracts)
            return ScreenEvent.OpenContracts;

        if (IsExitButtonHit(x, y))
            return ScreenEvent.CloseStation;

        // Click on the dimmed background outside the panel also closes it.
        if (!StationLayout.IsInsidePanel(x, y, _screenWidth, _screenHeight))
            return ScreenEvent.CloseStation;

        return ScreenEvent.None;
    }

    /// <summary>Convenience shortcut for a left click — kept for existing call-site/test conventions.</summary>
    public ScreenEvent OnMouseDown(float x, float y) => OnMouseDown(x, y, MouseButton.Left);

    public bool OnMouseMove(float x, float y)
    {
        _hoveredButton = StationLayout.HitTest(x, y, _screenWidth, _screenHeight);
        _isExitButtonHovered = IsExitButtonHit(x, y);

        // Not a button — hovering it only shows a delayed tooltip (see Render), so it must
        // not affect the interactive-cursor swap the way the other buttons do.
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

        return _hoveredButton != StationButton.None || _isExitButtonHovered;
    }

    /// <summary>True when (x, y) lands on the toolbar's exit-button icon (see StationToolbar).</summary>
    private bool IsExitButtonHit(float x, float y)
    {
        float pl = StationLayout.PanelLeft(_screenWidth);
        float pt = StationLayout.PanelTop(_screenHeight);
        var local = StationToolbar.ExitButtonLocalRect();
        return x >= pl + local.Left && x <= pl + local.Right && y >= pt + local.Top && y <= pt + local.Bottom;
    }

    /// <summary>True when (x, y) lands on the toolbar's food-rations readout (see StationToolbar).</summary>
    private bool IsFoodRationsHit(float x, float y)
    {
        float pl = StationLayout.PanelLeft(_screenWidth);
        float pt = StationLayout.PanelTop(_screenHeight);
        var local = StationToolbar.FoodRationsLocalRect();
        return x >= pl + local.Left && x <= pl + local.Right && y >= pt + local.Top && y <= pt + local.Bottom;
    }

    /// <summary>True when (x, y) lands on the toolbar's crew readout (see StationToolbar).</summary>
    private bool IsCrewHit(float x, float y)
    {
        float pl = StationLayout.PanelLeft(_screenWidth);
        float pt = StationLayout.PanelTop(_screenHeight);
        var local = StationToolbar.CrewLocalRect();
        return x >= pl + local.Left && x <= pl + local.Right && y >= pt + local.Top && y <= pt + local.Bottom;
    }

    /// <summary>True when (x, y) lands on the toolbar's tokens/credits readout (see StationToolbar).</summary>
    private bool IsTokensHit(float x, float y)
    {
        float pl = StationLayout.PanelLeft(_screenWidth);
        float pt = StationLayout.PanelTop(_screenHeight);
        var local = StationToolbar.TokensLocalRect();
        return x >= pl + local.Left && x <= pl + local.Right && y >= pt + local.Top && y <= pt + local.Bottom;
    }

    /// <summary>True when (x, y) lands on the toolbar's fuel readout (see StationToolbar).</summary>
    private bool IsFuelHit(float x, float y)
    {
        float pl = StationLayout.PanelLeft(_screenWidth);
        float pt = StationLayout.PanelTop(_screenHeight);
        var local = StationToolbar.FuelLocalRect();
        return x >= pl + local.Left && x <= pl + local.Right && y >= pt + local.Top && y <= pt + local.Bottom;
    }

    public ScreenEvent OnMouseWheel(float x, float y, float delta) => ScreenEvent.None;

    public void Render(SKCanvas canvas, int width, int height)
    {
        _screenWidth = width;
        _screenHeight = height;

        float pl = StationLayout.PanelLeft(width);
        float pt = StationLayout.PanelTop(height);
        var panelRect = new SKRect(pl, pt, pl + StationLayout.PanelWidth, pt + StationLayout.PanelHeight);
        MenuStyle.DrawPanel(canvas, panelRect);

        var snapshot = _buffer?.Latest?.Snapshot;
        string? stationName = StationToolbar.ResolveDockedStationName(snapshot);
        StationToolbar.Draw(canvas, pl, pt, stationName, isStationHub: true,
            isExitButtonHovered: _isExitButtonHovered,
            foodRationsCount: StationToolbar.ResolveFoodRationsCount(snapshot),
            crewCount: StationToolbar.ResolveCrewCount(snapshot),
            cabinsCount: StationToolbar.ResolveCabinsCount(snapshot),
            creditsCount: StationToolbar.ResolveCreditsCount(snapshot),
            fuelAmountKg: StationToolbar.ResolveFuelAmountKg(snapshot),
            fuelCapacityKg: StationToolbar.ResolveFuelCapacityKg(snapshot));

        float cx = pl + StationLayout.PanelWidth / 2f;

        DrawTradeButton(canvas, pl, pt);
        DrawHireButton(canvas, pl, pt);
        DrawFinanceButton(canvas, pl, pt);
        DrawContractsButton(canvas, pl, pt);

        foreach (var (row, text) in PlaceholderLines)
        {
            float textY = pt + StationLayout.BodyStartY + row * StationLayout.BodyLineHeight;
            canvas.DrawText(text, cx, textY, MenuStyle.TextStatus);
        }

        // Drawn last: the tooltip hangs below the toolbar into the body area and must
        // stay on top of the buttons and lines drawn above.
        StationToolbar.DrawTooltips(canvas, pl, pt,
            isFoodRationsHovered: IsFoodRationsTooltipVisible,
            isCrewHovered: IsCrewTooltipVisible,
            isTokensHovered: IsTokensTooltipVisible,
            isFuelHovered: IsFuelTooltipVisible);
    }

    private void DrawTradeButton(SKCanvas canvas, float panelLeft, float panelTop)
    {
        var (left, top, right, bottom) = StationLayout.TradeButtonLocalRect();
        var rect = new SKRect(panelLeft + left, panelTop + top, panelLeft + right, panelTop + bottom);

        MenuStyle.DrawButton(canvas, rect, "TRADE",
            _hoveredButton == StationButton.Trade ? ButtonState.Hovered : ButtonState.Normal);
    }

    private void DrawHireButton(SKCanvas canvas, float panelLeft, float panelTop)
    {
        var (left, top, right, bottom) = StationLayout.HireButtonLocalRect();
        var rect = new SKRect(panelLeft + left, panelTop + top, panelLeft + right, panelTop + bottom);

        MenuStyle.DrawButton(canvas, rect, "HIRE",
            _hoveredButton == StationButton.Hire ? ButtonState.Hovered : ButtonState.Normal);
    }

    private void DrawFinanceButton(SKCanvas canvas, float panelLeft, float panelTop)
    {
        var (left, top, right, bottom) = StationLayout.FinanceButtonLocalRect();
        var rect = new SKRect(panelLeft + left, panelTop + top, panelLeft + right, panelTop + bottom);

        MenuStyle.DrawButton(canvas, rect, "FINANCE",
            _hoveredButton == StationButton.Finance ? ButtonState.Hovered : ButtonState.Normal);
    }

    private void DrawContractsButton(SKCanvas canvas, float panelLeft, float panelTop)
    {
        var (left, top, right, bottom) = StationLayout.ContractsButtonLocalRect();
        var rect = new SKRect(panelLeft + left, panelTop + top, panelLeft + right, panelTop + bottom);

        MenuStyle.DrawButton(canvas, rect, "CONTRACTS",
            _hoveredButton == StationButton.Contracts ? ButtonState.Hovered : ButtonState.Normal);
    }
}
