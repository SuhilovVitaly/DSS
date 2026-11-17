using DeepSpaceSaga.Client.UI.Demo;
using DeepSpaceSaga.Contracts;
using SkiaSharp;

namespace DeepSpaceSaga.Client.UI.Screens.Ship;

public sealed partial class ShipScreen
{
    private DecoySupplyQuote? _decoyQuote;
    internal string SupplyStatus { get; private set; } = "";
    internal void RequestDecoys(string droneId)
    {
        if (_service is null || PendingService is { IsCompleted: false } || IsPending) return;
        _repairState = null;
        PendingService = SubmitSupply(new DecoyQuoteRequest(_commandId(),
            Math.Max(_serviceRevision ?? 0, _campaign?.Invoke()?.Revision ?? 0), droneId, 1));
    }
    internal void ConfirmDecoys()
    {
        if (_service is null || PendingService is { IsCompleted: false } || IsPending || _decoyQuote is not { } quote) return;
        PendingService = SubmitSupply(new ResupplyDecoyCommand(_commandId(), quote.Revision, quote.DroneId, quote.Id, quote.Quantity));
    }
    private async Task SubmitSupply(DemoCampaignCommand command)
    {
        SupplyStatus = Localization.Get("Demo.Repair.Pending");
        try
        {
            var response = await _service!(command, CancellationToken.None);
            _serviceRevision = response.Revision;
            _decoyQuote = response.Applied ? response.Supply?.Quote : null;
            SupplyStatus = !response.Applied ? StationRepairPresentation.Reason(response.Reason) :
                _decoyQuote is { } q ? string.Format(Localization.Get("Demo.Supply.Price"), q.Quantity, q.Total) :
                Localization.Get("Demo.Supply.Confirmed");
        }
        catch (Exception) { _decoyQuote = null; SupplyStatus = StationRepairPresentation.Reason("unavailable"); }
    }
    private void DrawSupply(SKCanvas canvas, float left, float top)
    {
        // A quote is command state, never a cache of current cargo or magazines.
        var supply = _campaign?.Invoke()?.Company?.Supply;
        using var text = new SKPaint { Color = SKColors.LightGray, TextSize = 16, IsAntialias = true };
        canvas.Save();
        canvas.ClipRect(new(left + 730, top + 570, left + 1370, top + 790));
        if (supply is not null)
        {
            canvas.DrawText(string.Format(Localization.Get("Demo.Supply.Stock"), supply.Stock, supply.MaximumShots), left + 730, top + 590, text);
            var drone = _selectedRow < 0 ? null : supply.Drones.FirstOrDefault(d => d.DroneId == _drones[_selectedRow].Id);
            if (drone is not null)
                canvas.DrawText(string.Format(Localization.Get("Demo.Supply.Drone"), drone.AmmoCurrent, drone.AmmoCapacity, drone.DecoyCharges, drone.DecoyCapacity), left + 730, top + 615, text);
        }
        canvas.DrawText(SupplyStatus, left + 730, top + 640, text);
        bool ready = PendingService is not { IsCompleted: false } && !IsPending;
        DrawLocalButton(canvas, left, top, (730, 650, 1340, 682), Localization.Get("Demo.Supply.Quote"), ready && _selectedRow >= 0 && supply?.DecoyUnitPrice is not null);
        DrawLocalButton(canvas, left, top, (730, 690, 1340, 722), Localization.Get("Demo.Supply.Confirm"), ready && _decoyQuote is not null);
        canvas.DrawText(Localization.Get("Demo.Supply.Market"), left + 730, top + 748, text);
        canvas.Restore();
    }
}
