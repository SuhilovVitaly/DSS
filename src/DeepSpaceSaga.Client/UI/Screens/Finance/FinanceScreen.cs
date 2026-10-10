using System.Collections.Immutable;
using System.Globalization;
using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Client.UI.Screens.Trade;
using DeepSpaceSaga.Client.UI.Controls;
using DeepSpaceSaga.Client.UI.Screens;
using Silk.NET.Input;
using SkiaSharp;

namespace DeepSpaceSaga.Client.UI.Screens.Finance;

/// <summary>
/// Finance overlay with authoritative voyage finances, player Credits and station availability.
/// Opened via the bottom-center Finance panel
/// button or Ctrl+F; closes via the toolbar's exit-button icon (see StationToolbar),
/// Escape, or a click outside the panel (on the dimmed background).
/// Pause-on-open/resume-on-close is handled generically by SkiaWindow's
/// PushModalAsync/PopModalAsync — this screen has no speed/pause logic of its own.
/// </summary>
public sealed class FinanceScreen : IScreen
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

    public FinanceScreen(SnapshotBuffer? buffer = null)
    {
        _buffer = buffer;
    }

    /// <summary>
    /// Panel background with title bar. The PNG asset is a fixed 1400×900 bitmap;
    /// since the panel standard (ТЗ ScreenCatalog.md) is now 1400×800, it is drawn
    /// stretched to <see cref="FinanceLayout.PanelWidth"/>x<see cref="FinanceLayout.PanelHeight"/>
    /// (see DrawBitmap call below) until the asset itself is redrawn at the new size.
    /// Loaded once and shared by every FinanceScreen instance; falls back to
    /// MenuStyle.DrawPanel's plain fill if the file is missing.
    /// </summary>
    private static readonly SKBitmap? BackgroundImage =
        LoadImage("Images/UI/mechanics-window-background-titlebar-1400x900.png");

    private static SKBitmap? LoadImage(string path)
    {
        try { return File.Exists(path) ? SKBitmap.Decode(path) : null; }
        catch { return null; }
    }

    /// <summary>True if the background PNG file was found and decoded at startup.</summary>
    internal static bool HasLoadedBackground => BackgroundImage is not null;

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

    public ScreenEvent OnKeyDown(Key key)
    {
        if (key == Key.Escape) return ScreenEvent.CloseFinance;
        var reports = Reports;
        if (reports.IsEmpty || key is not (Key.Up or Key.Down)) return ScreenEvent.None;
        int index = Math.Clamp(SelectedIndex(reports) + (key == Key.Up ? -1 : 1), 0, reports.Length - 1);
        SelectReport(reports, index);
        return ScreenEvent.None;
    }

    public ScreenEvent OnMouseDown(float x, float y, MouseButton button)
    {
        if (button != MouseButton.Left)
            return ScreenEvent.None;

        if (IsExitButtonHit(x, y))
            return ScreenEvent.CloseFinance;

        if (IsStationNameHit(x, y))
            return ScreenEvent.NavigateToStation;

        // Click on the dimmed background outside the panel also closes it.
        if (!FinanceLayout.IsInsidePanel(x, y, _screenWidth, _screenHeight))
            return ScreenEvent.CloseFinance;

        var reports = Reports;
        float localX = x - FinanceLayout.PanelLeft(_screenWidth), localY = y - FinanceLayout.PanelTop(_screenHeight);
        if (localX is >= 40 and <= 510 && localY is >= 388 and < 748)
        {
            int index = _listScroll + (int)((localY - 388) / 36);
            if (index < reports.Length) SelectReport(reports, index);
        }
        return ScreenEvent.None;
    }

    /// <summary>Convenience shortcut for a left click — kept for existing call sites/tests.</summary>
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

        float localX = x - FinanceLayout.PanelLeft(_screenWidth), localY = y - FinanceLayout.PanelTop(_screenHeight);
        return _isStationNameHovered || _isExitButtonHovered ||
            !Reports.IsEmpty && localX is >= 40 and <= 510 && localY is >= 388 and < 748 &&
            _listScroll + (int)((localY - 388) / 36) < Reports.Length;
    }

    public ScreenEvent OnMouseWheel(float x, float y, float delta)
    {
        float localX = x - FinanceLayout.PanelLeft(_screenWidth), localY = y - FinanceLayout.PanelTop(_screenHeight);
        if (delta == 0 || localY is < 350 or > 775) return ScreenEvent.None;
        int step = delta > 0 ? -3 : 3;
        if (localX is >= 40 and <= 510)
        {
            _selectedVoyageId ??= Reports.LastOrDefault()?.VoyageId;
            _listScroll = Math.Clamp(_listScroll + step, 0, Math.Max(0, Reports.Length - 10));
        }
        else if (localX is >= 550 and <= 1560) _detailScroll = Math.Clamp(_detailScroll + step, 0, Math.Max(0, VoyageRows.Count - 13));
        return ScreenEvent.None;
    }

    public void Render(SKCanvas canvas, int width, int height)
    {
        _screenWidth = width;
        _screenHeight = height;

        float pl = FinanceLayout.PanelLeft(width);
        float pt = FinanceLayout.PanelTop(height);
        var panelRect = new SKRect(pl, pt, pl + FinanceLayout.PanelWidth, pt + FinanceLayout.PanelHeight);
        if (BackgroundImage is not null)
            canvas.DrawBitmap(BackgroundImage, panelRect);
        else
            MenuStyle.DrawPanel(canvas, panelRect);

        var snapshot = _buffer?.Latest?.Snapshot;
        string? stationName = StationToolbar.ResolveDockedStationName(snapshot);
        StationToolbar.Draw(canvas, pl, pt, stationName, isStationHub: false, isHovered: _isStationNameHovered,
            windowName: "FINANCE", isExitButtonHovered: _isExitButtonHovered,
            foodRationsCount: StationToolbar.ResolveFoodRationsCount(snapshot),
            crewCount: StationToolbar.ResolveCrewCount(snapshot),
            cabinsCount: StationToolbar.ResolveCabinsCount(snapshot),
            creditsCount: StationToolbar.ResolveCreditsCount(snapshot),
            fuelAmountKg: StationToolbar.ResolveFuelAmountKg(snapshot),
            fuelCapacityKg: StationToolbar.ResolveFuelCapacityKg(snapshot), gameTimeMs: snapshot?.GameTimeMs, missingRations: snapshot?.MissingRations ?? 0);

        float cx = pl + FinanceLayout.PanelWidth / 2f;

        canvas.DrawText(snapshot is null ? "Ожидание данных о балансе" : $"Баланс: {snapshot.PlayerCredits:N0} Credits",
            cx, pt + FinanceLayout.BodyStartY, MenuStyle.TextStatus);
        canvas.DrawText(snapshot?.DockedStationTrade is not null ? "Цены и доступные партии — на экране торговли" : "Для торговли пристыкуйтесь к станции",
            cx, pt + FinanceLayout.BodyStartY + FinanceLayout.BodyLineHeight, MenuStyle.TextStatus);

        if (snapshot?.PortFees is { } fees)
        {
            canvas.DrawText("Следующий портовый сбор: " + GameTimeDisplay.Minutes(fees.NextPortFeeDueGameTimeMs),
                cx, pt + 300, MenuStyle.TextStatus);
            canvas.DrawText($"Портовая задолженность: {fees.Debt}", cx, pt + 330, MenuStyle.TextStatus);
        }

        DrawVoyageFinances(canvas, pl, pt);

        // Drawn last: the tooltip hangs below the toolbar into the body area and must
        // stay on top of everything the screen drew.
        StationToolbar.DrawTooltips(canvas, pl, pt,
            isFoodRationsHovered: IsFoodRationsTooltipVisible,
            isCrewHovered: IsCrewTooltipVisible,
            isTokensHovered: IsTokensTooltipVisible,
            isFuelHovered: IsFuelTooltipVisible);
    }

    private string? _selectedVoyageId;
    private int _listScroll, _detailScroll;
    private ImmutableArray<VoyageFinanceSnapshot> Reports
    {
        get
        {
            var reports = _buffer?.Latest?.Snapshot.VoyageFinances ?? default;
            return reports.IsDefaultOrEmpty ? [] : reports.Where(v => v is not null && !string.IsNullOrWhiteSpace(v.VoyageId))
                .DistinctBy(v => v.VoyageId, StringComparer.Ordinal).TakeLast(50).ToImmutableArray();
        }
    }
    private int SelectedIndex(ImmutableArray<VoyageFinanceSnapshot> reports)
    {
        for (int i = 0; i < reports.Length; i++) if (reports[i].VoyageId == _selectedVoyageId) return i;
        return reports.Length - 1;
    }
    private void SelectReport(ImmutableArray<VoyageFinanceSnapshot> reports, int index)
    {
        _selectedVoyageId = reports[index].VoyageId; _detailScroll = 0;
        if (index < _listScroll) _listScroll = index;
        else if (index >= _listScroll + 10) _listScroll = index - 9;
    }
    internal static string MoneyText(long? amount) => amount is { } value
        ? TradeScreen.F("Tokens", value.ToString("N0", CultureInfo.CurrentCulture)) : Localization.Get("Finance.Unavailable");
    internal static string RouteText(VoyageFinanceSnapshot f) => string.Format(CultureInfo.CurrentCulture,
        Localization.Get("Finance.VoyageRoute"), f.OriginStationObjectId, f.DestinationStationObjectId ?? "—");
    internal static string StateText(string state) => Localization.Get(state switch
    {
        VoyageFinanceStates.InTransit => "Finance.VoyageState.InTransit",
        VoyageFinanceStates.AwaitingRealization => "Finance.VoyageState.AwaitingRealization",
        VoyageFinanceStates.Finalized => "Finance.VoyageState.Finalized",
        VoyageFinanceStates.Interrupted => "Finance.VoyageState.Interrupted",
        _ => "Finance.Unavailable"
    });
    internal VoyageFinanceSnapshot? SelectedReport => Reports.IsEmpty ? null : Reports[SelectedIndex(Reports)];
    internal int DetailScroll => _detailScroll;
    internal sealed record FinanceRow(string Label, string Value, bool IsCargo = false);
    internal IReadOnlyList<FinanceRow> VoyageRows
    {
        get
        {
            var reports = Reports;
            if (reports.IsEmpty) return [];
            var f = reports[SelectedIndex(reports)];
            var rows = new List<FinanceRow>();
            void Add(string key, long? value) => rows.Add(new(Localization.Get("Finance." + key), MoneyText(value)));
            Add("GrossSales", f.GrossSalesCredits); Add("CostOfGoodsSold", f.CostOfGoodsSoldCredits);
            Add("RouteFuelCost", f.RouteFuelCostCredits); Add("PortFeesAssessed", f.PortFeesAssessedCredits);
            Add("PortFeesPaid", f.PortFeesPaidCredits); Add("PortFeeDebt", f.OutstandingPortFeeDebtCredits);
            if (f.EventCostsCredits != 0) Add("EventCosts", f.EventCostsCredits);
            if (f.PassengerPayoutCredits != 0) Add("PassengerPayout", f.PassengerPayoutCredits);
            if (f.PassengerPenaltyCredits != 0) Add("PassengerPenalty", f.PassengerPenaltyCredits);
            Add("NetProfit", f.NetProfitCredits);
            if (!f.UnsoldCargo.IsDefaultOrEmpty)
                foreach (var c in f.UnsoldCargo)
                    rows.Add(new(TradeItemPresentation.ItemDisplayName(c.ItemTypeId), string.Format(CultureInfo.CurrentCulture,
                        Localization.Get("Finance.UnsoldCargo"), TradeItemPresentation.ItemDisplayName(c.ItemTypeId),
                        TradeItemPresentation.FormatQuantity(c.ItemTypeId, c.Quantity), MoneyText(c.CostBasisCredits)), true));
            return rows;
        }
    }
    private void DrawVoyageFinances(SKCanvas canvas, float left, float top)
    {
        var reports = Reports;
        canvas.Save(); canvas.Translate(left, top); canvas.ClipRect(new SKRect(20, 350, 1580, 782));
        using var p = new TradePainter(canvas);
        p.Text(Localization.Get("Finance.VoyageTitle"), new(40, 350, 510, 382), 22, bold: true);
        if (reports.IsEmpty)
        {
            p.Text(Localization.Get("Finance.NoVoyages"), new(40, 388, 1560, 428), 18, TradePainter.Muted);
            canvas.Restore(); return;
        }
        int selected = SelectedIndex(reports);
        if (_selectedVoyageId is not null && !reports.Any(v => v.VoyageId == _selectedVoyageId)) _selectedVoyageId = null;
        if (_selectedVoyageId is null) _listScroll = Math.Max(0, selected - 9);
        _listScroll = Math.Clamp(_listScroll, 0, Math.Max(0, reports.Length - 10));
        for (int row = 0; row < 10 && row + _listScroll < reports.Length; row++)
        {
            var report = reports[row + _listScroll];
            var rect = new SKRect(40, 388 + row * 36, 510, 420 + row * 36);
            p.Box(rect, row + _listScroll == selected ? TradePainter.Selected : TradePainter.Surface);
            p.Text(RouteText(report) + " · " + StateText(report.State), new(rect.Left + 8, rect.Top, rect.Right - 8, rect.Bottom), 13);
        }
        var chosen = reports[selected];
        p.Text(RouteText(chosen) + " · " + StateText(chosen.State), new(550, 350, 1560, 382), 18, bold: true);
        var rows = VoyageRows;
        _detailScroll = Math.Clamp(_detailScroll, 0, Math.Max(0, rows.Count - 13));
        for (int row = 0; row < 13 && row + _detailScroll < rows.Count; row++)
        {
            var value = rows[row + _detailScroll];
            float y = 388 + row * 28;
            if (value.IsCargo)
            {
                p.Text(value.Value, new(550, y, 1560, y + 26), 14, TradePainter.Muted);
                continue;
            }
            p.Text(value.Label, new(550, y, 1010, y + 26), 14, TradePainter.Muted);
            SKColor color = value.Label == Localization.Get("Finance.NetProfit") && chosen.NetProfitCredits is > 0
                ? TradePainter.Green : value.Label == Localization.Get("Finance.NetProfit") && chosen.NetProfitCredits is < 0
                    ? TradePainter.Red : TradePainter.TextColor;
            p.Text(value.Value, new(1020, y, 1560, y + 26), 14, color, align: SKTextAlign.Right);
        }
        canvas.Restore();
    }

    /// <summary>True when (x, y) lands on the toolbar's station-name link (see StationToolbar).</summary>
    private bool IsStationNameHit(float x, float y)
    {
        string? stationName = StationToolbar.ResolveDockedStationName(_buffer?.Latest?.Snapshot);
        if (string.IsNullOrEmpty(stationName))
            return false;

        float pl = FinanceLayout.PanelLeft(_screenWidth);
        float pt = FinanceLayout.PanelTop(_screenHeight);
        var local = StationToolbar.NameLocalRect(stationName);
        return x >= pl + local.Left && x <= pl + local.Right && y >= pt + local.Top && y <= pt + local.Bottom;
    }

    /// <summary>True when (x, y) lands on the toolbar's exit-button icon (see StationToolbar).</summary>
    private bool IsExitButtonHit(float x, float y)
    {
        float pl = FinanceLayout.PanelLeft(_screenWidth);
        float pt = FinanceLayout.PanelTop(_screenHeight);
        var local = StationToolbar.ExitButtonLocalRect();
        return x >= pl + local.Left && x <= pl + local.Right && y >= pt + local.Top && y <= pt + local.Bottom;
    }

    /// <summary>True when (x, y) lands on the toolbar's food-rations readout (see StationToolbar).</summary>
    private bool IsFoodRationsHit(float x, float y)
    {
        float pl = FinanceLayout.PanelLeft(_screenWidth);
        float pt = FinanceLayout.PanelTop(_screenHeight);
        var local = StationToolbar.FoodRationsLocalRect();
        return x >= pl + local.Left && x <= pl + local.Right && y >= pt + local.Top && y <= pt + local.Bottom;
    }

    /// <summary>True when (x, y) lands on the toolbar's crew readout (see StationToolbar).</summary>
    private bool IsCrewHit(float x, float y)
    {
        float pl = FinanceLayout.PanelLeft(_screenWidth);
        float pt = FinanceLayout.PanelTop(_screenHeight);
        var local = StationToolbar.CrewLocalRect();
        return x >= pl + local.Left && x <= pl + local.Right && y >= pt + local.Top && y <= pt + local.Bottom;
    }

    /// <summary>True when (x, y) lands on the toolbar's tokens/credits readout (see StationToolbar).</summary>
    private bool IsTokensHit(float x, float y)
    {
        float pl = FinanceLayout.PanelLeft(_screenWidth);
        float pt = FinanceLayout.PanelTop(_screenHeight);
        var local = StationToolbar.TokensLocalRect();
        return x >= pl + local.Left && x <= pl + local.Right && y >= pt + local.Top && y <= pt + local.Bottom;
    }

    /// <summary>True when (x, y) lands on the toolbar's fuel readout (see StationToolbar).</summary>
    private bool IsFuelHit(float x, float y)
    {
        float pl = FinanceLayout.PanelLeft(_screenWidth);
        float pt = FinanceLayout.PanelTop(_screenHeight);
        var local = StationToolbar.FuelLocalRect();
        return x >= pl + local.Left && x <= pl + local.Right && y >= pt + local.Top && y <= pt + local.Bottom;
    }
}
