using DeepSpaceSaga.Client.UI.Controls;
using DeepSpaceSaga.Client.UI.Screens;
using Silk.NET.Input;
using SkiaSharp;
using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Client.UI.Screens.Trade;
using DeepSpaceSaga.Client.UI.Screens.GameSession;
using System.Collections.Immutable;

namespace DeepSpaceSaga.Client.UI.Screens.Station;

/// <summary>
/// Station overlay (Documentation/02-FirstRelease/Screens/Station.md). Placeholder shell:
/// Representatives/Install Drilling Unit are not yet implemented, so the
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
    private readonly GameSessionHandle? _session;
    internal GameSessionHandle? OpenedForHandle => _session;
    internal string? OpenedForStationObjectId { get; }
    internal long? OpenedAtPortFeeGameTimeMs { get; }
    internal bool HasValidVisit
    {
        get
        {
            var snapshot = _buffer?.Latest?.Snapshot;
            return _session is not null && ReferenceEquals(_buffer, _session.Buffer) &&
                _session.Failure is null && OpenedForStationObjectId is not null &&
                OpenedForStationObjectId == TradeModel.ResolveLocalStationId(snapshot) &&
                OpenedAtPortFeeGameTimeMs == snapshot?.PortFees?.FirstPortFeeGameTimeMs;
        }
    }

    private int _screenWidth;
    private int _screenHeight;
    private StationButton _hoveredButton = StationButton.None;
    private bool _isExitButtonHovered;
    private string? _selectedDestinationId;
    private string? _selectionStationId;
    private int _routeScroll;
    internal int RouteScrollOffset => _routeScroll;
    internal System.Collections.Immutable.ImmutableArray<StationRouteRow> RouteRows =>
        StationRoutePresentation.Build(_buffer?.Latest?.Snapshot.TradingRoutes ?? default)
            .Select(row => ClusterMapPresentation.ClusterName(_buffer?.Latest?.Snapshot, row.DestinationStationObjectId) is { } cluster
                ? row with { PrimaryText = row.PrimaryText.Replace(row.DestinationStationObjectId, $"{row.DestinationStationObjectId} · {cluster}", StringComparison.Ordinal) } : row)
            .ToImmutableArray();

    internal string? SelectedVoyageDestinationObjectId
    {
        get
        {
            RefreshRouteSelection(_buffer?.Latest?.Snapshot);
            return _selectedDestinationId;
        }
    }

    public string? SelectedDestinationId
    {
        get
        {
            RefreshRouteSelection(_buffer?.Latest?.Snapshot);
            var options = _buffer?.Latest?.Snapshot.Voyage?.RouteOptions ?? default;
            return RouteRows.Any(r => r.DestinationStationObjectId == _selectedDestinationId && r.IsEnabled) &&
                !options.IsDefaultOrEmpty && options.Any(option => option.IsAvailable &&
                option.BlockReasonCode is null && option.DestinationStationObjectId == _selectedDestinationId)
                ? _selectedDestinationId : null;
        }
    }

    private void RefreshRouteSelection(AuthoritativeSnapshot? snapshot)
    {
        var stationId = CurrentStationId(snapshot);
        if (stationId != _selectionStationId)
        {
            _selectedDestinationId = null;
            _routeScroll = 0;
        }
        _selectionStationId = stationId;
        var rows = StationRoutePresentation.Build(snapshot?.TradingRoutes ?? default);
        _routeScroll = Math.Clamp(_routeScroll, 0, Math.Max(0, rows.Length - StationLayout.VisibleRouteRows));
        if (snapshot?.Voyage is not { Phase: VoyagePhases.Docked } voyage || voyage.RouteOptions.IsDefaultOrEmpty || rows.IsDefaultOrEmpty)
        {
            _selectedDestinationId = null;
            return;
        }
        if (!rows.Any(row => row.DestinationStationObjectId == _selectedDestinationId && row.IsEnabled))
            _selectedDestinationId = rows.FirstOrDefault(row => row.DestinationStationObjectId == snapshot.SelectedObjectId && row.IsEnabled && voyage.RouteOptions.Any(option =>
                option.DestinationStationObjectId == row.DestinationStationObjectId && option.IsAvailable && option.BlockReasonCode is null))?.DestinationStationObjectId
                ?? rows.FirstOrDefault(row => row.IsEnabled && voyage.RouteOptions.Any(option =>
                option.DestinationStationObjectId == row.DestinationStationObjectId && option.IsAvailable && option.BlockReasonCode is null))?.DestinationStationObjectId;
    }

    internal static string RouteOptionText(VoyageRouteOptionSnapshot option) =>
        $"{option.DestinationDisplayName}  {option.DistanceClass}  ETA {TimeSpan.FromMilliseconds(option.TravelEstimateGameTimeMs):g}";

    internal static string DepartureReasonText(string? code) => code switch
    {
        CommandReasonCodes.VoyageDestinationRequired => "Select a destination",
        CommandReasonCodes.VoyageDestinationUnavailable => "Destination unavailable",
        CommandReasonCodes.RouteUnavailable => "Route temporarily unavailable",
        CommandReasonCodes.VoyageAlreadyActive => "A voyage is already active",
        CommandReasonCodes.VoyageWrongDestination => "Dock at the voyage destination",
        CommandReasonCodes.VoyageOutstandingDebt => "Settle outstanding port debt",
        CommandReasonCodes.VoyageInsufficientFuel or CommandReasonCodes.InsufficientVoyageFuel => "Insufficient fuel",
        CommandReasonCodes.FuelEfficiencyUnavailable => "Engine fuel efficiency unavailable",
        _ => string.IsNullOrWhiteSpace(code) ? "Departure unavailable" : $"Departure unavailable ({code})",
    };
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

    public StationScreen(SnapshotBuffer? buffer = null, GameSessionHandle? session = null)
    {
        _buffer = buffer;
        _session = session;
        var snapshot = buffer?.Latest?.Snapshot;
        OpenedForStationObjectId = TradeModel.ResolveLocalStationId(snapshot);
        OpenedAtPortFeeGameTimeMs = snapshot?.PortFees?.FirstPortFeeGameTimeMs;
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
    };

    public void OnActivated()
    {
        RefreshRouteSelection(_buffer?.Latest?.Snapshot);
        _hoveredButton = StationButton.None;
        _isExitButtonHovered = false;
        _foodRationsHoverStartedAtMs = null;
        _crewHoverStartedAtMs = null;
        _tokensHoverStartedAtMs = null;
        _fuelHoverStartedAtMs = null;
    }

    public void OnDeactivated() { }

    private static string? CurrentStationId(AuthoritativeSnapshot? snapshot) =>
        snapshot?.Objects.FirstOrDefault(o => o.ObjectId == snapshot.PlayerShipObjectId)?.DockedStationObjectId;

    public ScreenEvent OnKeyDown(Key key) =>
        key == Key.Escape ? ScreenEvent.CloseStation : ScreenEvent.None;

    public ScreenEvent OnMouseDown(float x, float y, MouseButton button)
    {
        if (button != MouseButton.Left)
            return ScreenEvent.None;

        if (_session?.StationTravelPending == true) return ScreenEvent.None;
        RefreshRouteSelection(_buffer?.Latest?.Snapshot);
        var voyage = _buffer?.Latest?.Snapshot.Voyage;
        var rows = RouteRows;
        if (voyage is { Phase: VoyagePhases.Docked })
            for (int i = 0; i < Math.Min(StationLayout.VisibleRouteRows, rows.Length - _routeScroll); i++)
            {
                if (!RouteRect(i).Contains(x, y)) continue;
                var row = rows[i + _routeScroll];
                if (row.IsEnabled) _selectedDestinationId = row.DestinationStationObjectId;
                return ScreenEvent.None;
            }
        for (int i = 0; i < 4; i++)
        {
            var rect = DistrictRect(i);
            if (rect.Contains(x, y))
                return _buffer?.Latest?.Snapshot.CurrentStationDistrict == (StationDistrict)i
                    ? ScreenEvent.None : (ScreenEvent)((int)ScreenEvent.TravelDock + i);
        }

        var hit = StationLayout.HitTest(x, y, _screenWidth, _screenHeight);
        if (hit == StationButton.Trade)
            return HasValidVisit ? ScreenEvent.OpenTrade : ScreenEvent.None;
        if (hit == StationButton.Hire)
            return ScreenEvent.OpenHire;
        if (hit == StationButton.Finance)
            return ScreenEvent.OpenFinance;
        if (hit == StationButton.Contracts)
            return ScreenEvent.OpenContracts;
        if (hit == StationButton.Undock)
            return _buffer is null || SelectedDestinationId is not null ? ScreenEvent.Undock : ScreenEvent.None;

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
        if (_hoveredButton == StationButton.Trade && !HasValidVisit)
            _hoveredButton = StationButton.None;
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

        return _hoveredButton != StationButton.None || _isExitButtonHovered ||
            Enumerable.Range(0, 4).Any(i => DistrictRect(i).Contains(x, y)) ||
            (_buffer?.Latest?.Snapshot.Voyage is { Phase: VoyagePhases.Docked } voyage &&
             RouteRows.Skip(_routeScroll).Take(StationLayout.VisibleRouteRows).Select((row, i) => (row, i))
                 .Any(entry => entry.row.IsEnabled && RouteRect(entry.i).Contains(x, y)));
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

    public ScreenEvent OnMouseWheel(float x, float y, float delta)
    {
        RefreshRouteSelection(_buffer?.Latest?.Snapshot);
        var viewport = StationLayout.RouteViewportLocalRect();
        if (delta != 0 && float.IsFinite(delta) && x >= StationLayout.PanelLeft(_screenWidth) + viewport.Left &&
            x <= StationLayout.PanelLeft(_screenWidth) + viewport.Right && y >= StationLayout.PanelTop(_screenHeight) + viewport.Top &&
            y <= StationLayout.PanelTop(_screenHeight) + viewport.Bottom)
            _routeScroll = Math.Clamp(_routeScroll + (delta < 0 ? 1 : -1), 0, Math.Max(0, RouteRows.Length - StationLayout.VisibleRouteRows));
        return ScreenEvent.None;
    }

    public void Render(SKCanvas canvas, int width, int height)
    {
        _screenWidth = width;
        _screenHeight = height;

        float pl = StationLayout.PanelLeft(width);
        float pt = StationLayout.PanelTop(height);
        var panelRect = new SKRect(pl, pt, pl + StationLayout.PanelWidth, pt + StationLayout.PanelHeight);
        MenuStyle.DrawPanel(canvas, panelRect);

        var snapshot = _buffer?.Latest?.Snapshot;
        RefreshRouteSelection(snapshot);
        string? stationName = StationToolbar.ResolveDockedStationName(snapshot);
        StationToolbar.Draw(canvas, pl, pt, stationName, isStationHub: true,
            isExitButtonHovered: _isExitButtonHovered,
            foodRationsCount: StationToolbar.ResolveFoodRationsCount(snapshot),
            crewCount: StationToolbar.ResolveCrewCount(snapshot),
            cabinsCount: StationToolbar.ResolveCabinsCount(snapshot),
            creditsCount: StationToolbar.ResolveCreditsCount(snapshot),
            fuelAmountKg: StationToolbar.ResolveFuelAmountKg(snapshot),
            fuelCapacityKg: StationToolbar.ResolveFuelCapacityKg(snapshot), gameTimeMs: snapshot?.GameTimeMs, missingRations: snapshot?.MissingRations ?? 0);

        float cx = pl + StationLayout.PanelWidth / 2f;

        DrawTradeButton(canvas, pl, pt);
        DrawHireButton(canvas, pl, pt);
        DrawFinanceButton(canvas, pl, pt);
        DrawContractsButton(canvas, pl, pt);
        DrawUndockButton(canvas, pl, pt);

        DrawRoutes(canvas, snapshot, pl, pt);

        string[] districts = ["Док", "Рынок", "Жилой район", "Администрация"];
        for (int i = 0; i < districts.Length; i++)
            MenuStyle.DrawButton(canvas, DistrictRect(i), districts[i],
                _session?.StationTravelPending == true || snapshot?.CurrentStationDistrict == (StationDistrict)i
                    ? ButtonState.Disabled : ButtonState.Normal);
        canvas.DrawText("Переход в другой район: 1 час. Возврат также занимает 1 час.",
            cx, pt + 390, MenuStyle.TextStatus);

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

    private SKRect DistrictRect(int index)
    {
        float left = StationLayout.PanelLeft(_screenWidth) + 420 + index * 195;
        float top = StationLayout.PanelTop(_screenHeight) + 320;
        return new SKRect(left, top, left + 185, top + 36);
    }

    private SKRect RouteRect(int index)
    {
        var r = StationLayout.RouteRowLocalRect(index);
        return new SKRect(StationLayout.PanelLeft(_screenWidth) + r.Left, StationLayout.PanelTop(_screenHeight) + r.Top,
            StationLayout.PanelLeft(_screenWidth) + r.Right, StationLayout.PanelTop(_screenHeight) + r.Bottom);
    }

    private void DrawRoutes(SKCanvas canvas, AuthoritativeSnapshot? snapshot, float pl, float pt)
    {
        var rows = RouteRows;
        using var text = MenuStyle.TextStatus.Clone();
        text.TextAlign = SKTextAlign.Left;
        text.TextSize = 12;
        canvas.DrawText("DESTINATION", pl + 400, pt + 430, text);
        var v = StationLayout.RouteViewportLocalRect();
        canvas.Save();
        canvas.ClipRect(new(pl + v.Left, pt + v.Top, pl + v.Right, pt + v.Bottom));
        if (rows.IsDefaultOrEmpty) canvas.DrawText(StationRoutePresentation.EmptyMessage, pl + 410, pt + 465, text);
        for (int i = 0; i < Math.Min(StationLayout.VisibleRouteRows, rows.Length - _routeScroll); i++)
        {
            var row = rows[i + _routeScroll];
            var rect = RouteRect(i);
            canvas.DrawRect(rect, row.DestinationStationObjectId == _selectedDestinationId ? MenuStyle.ButtonFillPressed : MenuStyle.ButtonFillNormal);
            canvas.DrawRect(rect, MenuStyle.ButtonBorder);
            text.TextSize = 12;
            text.Color = !row.IsEnabled ? MenuStyle.ColorTextDim : row.Availability == TradingRouteAvailability.Restricted || row.Risk == TradingRouteRisk.Elevated
                ? new SKColor(255, 190, 80) : MenuStyle.ColorText;
            canvas.DrawText(row.PrimaryText, rect.Left + 8, rect.Top + 15, text);
            canvas.DrawText(row.SecondaryText, rect.Left + 8, rect.Top + 33, text);
            string? blocker = snapshot?.Voyage?.RouteOptions.IsDefaultOrEmpty == false ? snapshot.Voyage.RouteOptions
                .FirstOrDefault(o => o.DestinationStationObjectId == row.DestinationStationObjectId)?.BlockReasonCode : null;
            string? reason = row.ReasonText is null ? null : string.Join("; ", row.ReasonText.Split("; ").Select(Localization.Get));
            string context = string.Join("  ", new[] { reason, blocker is null ? null : DepartureReasonText(blocker) }
                .Where(t => !string.IsNullOrEmpty(t)));
            text.TextSize = 11;
            if (context.Length > 0) canvas.DrawText(context, rect.Left + 8, rect.Top + 49, text);
            if (row.ActiveEventText is { } events) canvas.DrawText(events, rect.Left + 8, rect.Top + 63, text);
        }
        canvas.Restore();
        if (rows.Length > StationLayout.VisibleRouteRows)
            canvas.DrawText($"{_routeScroll + 1}-{Math.Min(_routeScroll + StationLayout.VisibleRouteRows, rows.Length)} / {rows.Length}  Scroll", pl + 400, pt + 745, text);
        if (snapshot?.Voyage?.BlockReasonCode is { } reasonCode)
            canvas.DrawText(DepartureReasonText(reasonCode), pl + 400, pt + 775, text);
    }

    private void DrawTradeButton(SKCanvas canvas, float panelLeft, float panelTop)
    {
        var (left, top, right, bottom) = StationLayout.TradeButtonLocalRect();
        var rect = new SKRect(panelLeft + left, panelTop + top, panelLeft + right, panelTop + bottom);

        MenuStyle.DrawButton(canvas, rect, "TRADE",
            !HasValidVisit ? ButtonState.Disabled :
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

    private void DrawUndockButton(SKCanvas canvas, float panelLeft, float panelTop)
    {
        var (left, top, right, bottom) = StationLayout.UndockButtonLocalRect();
        var rect = new SKRect(panelLeft + left, panelTop + top, panelLeft + right, panelTop + bottom);

        MenuStyle.DrawButton(canvas, rect, "UNDOCK",
            _buffer is not null && SelectedDestinationId is null ? ButtonState.Disabled :
            _hoveredButton == StationButton.Undock ? ButtonState.Hovered : ButtonState.Normal);
    }
}
