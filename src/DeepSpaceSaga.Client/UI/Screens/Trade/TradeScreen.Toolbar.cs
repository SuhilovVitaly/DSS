using DeepSpaceSaga.Client.UI.Controls;
using SkiaSharp;

namespace DeepSpaceSaga.Client.UI.Screens.Trade;

public sealed partial class TradeScreen
{
    private bool _stationHovered, _exitHovered;
    private readonly long?[] _toolbarHoverStarted = new long?[4];
    private static SKRect ToolbarInfoRect(int index) => index switch
    {
        0 => StationToolbar.FoodRationsLocalRect(), 1 => StationToolbar.CrewLocalRect(),
        2 => StationToolbar.TokensLocalRect(), _ => StationToolbar.FuelLocalRect()
    };
    private bool TooltipVisible(int index) => _toolbarHoverStarted[index] is { } start &&
        Environment.TickCount64 - start >= MenuStyle.TooltipHoverDelaySeconds * 1000;
    internal bool IsFoodRationsTooltipVisible => TooltipVisible(0);
    internal bool IsCrewTooltipVisible => TooltipVisible(1);
    internal bool IsTokensTooltipVisible => TooltipVisible(2);
    internal bool IsFuelTooltipVisible => TooltipVisible(3);
    private SKRect StationNameRect => StationToolbar.ResolveDockedStationName(_buffer?.Latest?.Snapshot) is { Length: > 0 } name
        ? StationToolbar.NameLocalRect(name) : SKRect.Empty;

    private void DrawToolbar(SKCanvas canvas, float pl, float pt)
    {
        var snapshot = _buffer?.Latest?.Snapshot;
        StationToolbar.Draw(canvas, pl, pt, StationToolbar.ResolveDockedStationName(snapshot),
            isStationHub: false, isHovered: _stationHovered, windowName: "TRADE", isExitButtonHovered: _exitHovered,
            foodRationsCount: StationToolbar.ResolveFoodRationsCount(snapshot),
            crewCount: StationToolbar.ResolveCrewCount(snapshot), cabinsCount: StationToolbar.ResolveCabinsCount(snapshot),
            creditsCount: StationToolbar.ResolveCreditsCount(snapshot),
            fuelAmountKg: StationToolbar.ResolveFuelAmountKg(snapshot), fuelCapacityKg: StationToolbar.ResolveFuelCapacityKg(snapshot), gameTimeMs: snapshot?.GameTimeMs);
    }
    private void DrawToolbarTooltips(SKCanvas canvas, float pl, float pt) => StationToolbar.DrawTooltips(canvas, pl, pt,
        IsFoodRationsTooltipVisible, IsCrewTooltipVisible, IsTokensTooltipVisible, IsFuelTooltipVisible);
    private void UpdateToolbarHover(SKPoint point)
    {
        _stationHovered = StationNameRect.Contains(point);
        _exitHovered = StationToolbar.ExitButtonLocalRect().Contains(point);
        for (int i = 0; i < _toolbarHoverStarted.Length; i++)
            _toolbarHoverStarted[i] = ToolbarInfoRect(i).Contains(point) ? _toolbarHoverStarted[i] ?? Environment.TickCount64 : null;
    }
}
