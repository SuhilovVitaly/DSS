using System.Collections.Immutable;
using DeepSpaceSaga.Client.UI.Demo;
using DeepSpaceSaga.Contracts;
using SkiaSharp;

namespace DeepSpaceSaga.Client.UI.Screens.Ship;

public sealed partial class ShipScreen
{
    private bool _shipyard;
    private int _slotSelection;
    private ReplacementSnapshot? _replacement;
    internal string ReplacementStatus { get; private set; } = "";
    private ReplacementSnapshot? Replacement => _replacement ?? _campaign?.Invoke()?.Company?.Replacement;
    private ImmutableArray<ReplacementSlot> AvailableSlots => Replacement?.Offers.Where(o => !o.Equipment)
        .SelectMany(o => o.CompatibleSlots).Distinct().ToImmutableArray() ?? [];

    internal void PurchaseReplacement(ReplacementOffer offer, ReplacementSlot slot)
    {
        if (_service is null || PendingService is { IsCompleted: false } || IsPending) return;
        PendingService = SubmitReplacement(new PurchaseDroneCommand(_commandId(),
            Math.Max(_serviceRevision ?? 0, _campaign?.Invoke()?.Revision ?? 0), offer.Id, slot.HangarModuleId, slot.SlotIndex));
    }
    internal void EquipReplacement(ReplacementOffer offer, string droneId)
    {
        if (_service is null || PendingService is { IsCompleted: false } || IsPending) return;
        PendingService = SubmitReplacement(new EquipDroneCommand(_commandId(),
            Math.Max(_serviceRevision ?? 0, _campaign?.Invoke()?.Revision ?? 0), droneId, offer.Id));
    }
    private async Task SubmitReplacement(DemoCampaignCommand command)
    {
        _repairState = null; _supply = null;
        ReplacementStatus = Localization.Get("Demo.Repair.Pending");
        try
        {
            var response = await _service!(command, CancellationToken.None);
            _serviceRevision = response.Revision;
            _replacement = response.Applied ? response.Replacement : null;
            ReplacementStatus = response.Applied ? Localization.Get("Demo.Replacement.Confirmed") : StationRepairPresentation.Reason(response.Reason);
        }
        catch (Exception) { _replacement = null; ReplacementStatus = StationRepairPresentation.Reason("unavailable"); }
    }
    private void ClickShipyard(float x, float y)
    {
        var slots = AvailableSlots;
        if (y >= 300 && y <= 332) { if (slots.Length > 0) _slotSelection = (_slotSelection + 1) % slots.Length; return; }
        if (Replacement is not { } state) return;
        for (int i = 0; i < state.Offers.Length && i < 4; i++)
        {
            if (y < 380 + i * 78 || y > 410 + i * 78) continue;
            var offer = state.Offers[i];
            if (offer.Reason is not null || offer.Stock <= 0) return;
            if (offer.Equipment && _selectedRow >= 0) EquipReplacement(offer, _drones[_selectedRow].Id);
            else if (!offer.Equipment && slots.Length > 0) PurchaseReplacement(offer, slots[_slotSelection % slots.Length]);
        }
    }
    private void DrawShipyard(SKCanvas canvas, float left, float top)
    {
        using var text = new SKPaint { Color = SKColors.LightGray, TextSize = 15, IsAntialias = true };
        canvas.Save(); canvas.ClipRect(new(left + 730, top + 300, left + 1370, top + 790));
        var slots = AvailableSlots;
        var slot = slots.Length == 0 ? null : slots[_slotSelection % slots.Length];
        DrawLocalButton(canvas, left, top, (730, 300, 1340, 332), slot is null ? Localization.Get("Demo.Replacement.Empty") :
            string.Format(Localization.Get("Demo.Replacement.Slot"), slot.HangarModuleId, slot.SlotIndex + 1), slots.Length > 0);
        if (Replacement is { } state)
            for (int i = 0; i < state.Offers.Length && i < 4; i++)
            {
                var offer = state.Offers[i]; float y = 365 + i * 78;
                canvas.DrawText(string.Format(Localization.Get("Demo.Replacement.Offer"), Localization.Get("Demo.Replacement." + offer.Id), offer.Total, offer.Stock), left + 730, top + y, text);
                DrawLocalButton(canvas, left, top, (730, y + 15, 1340, y + 45), Localization.Get(offer.Equipment ? "Demo.Replacement.Equip" : "Demo.Replacement.Buy"),
                    PendingService is not { IsCompleted: false } && !IsPending && offer.Reason is null && (offer.Equipment ? _selectedRow >= 0 : slot is not null));
            }
        canvas.DrawText(ReplacementStatus, left + 730, top + 690, text);
        canvas.DrawText(Localization.Get("Demo.Replacement.Help"), left + 730, top + 720, text);
        canvas.Restore();
    }
}
