using DeepSpaceSaga.Client.UI.Controls;
using DeepSpaceSaga.Client.UI.Demo;
using DeepSpaceSaga.Client.UI.Screens.TacticalBattle;
using DeepSpaceSaga.Contracts;
using SkiaSharp;

namespace DeepSpaceSaga.Client.UI.Screens.Hire;

public sealed partial class HireScreen
{
    private readonly Func<DemoCampaignCommand, CancellationToken, ValueTask<DemoCampaignResponse>>? _service;
    private readonly Func<DemoCampaignSnapshot?>? _campaign;
    private RecruitmentSnapshot? _recruitment;
    private long _revision;
    private int _pilotIndex, _droneIndex;
    internal Task? PendingRecruitment { get; private set; }
    internal string RecruitmentStatus { get; private set; } = "";
    private DemoCampaignSnapshot? Campaign => _campaign?.Invoke() ?? _buffer?.Latest?.Snapshot.DemoCampaign;
    private RecruitmentSnapshot? Recruitment => _recruitment ?? Campaign?.Company?.Recruitment;
    internal void Hire(string offerId) => Submit(new HirePilotCommand(Guid.NewGuid().ToString("N"), Math.Max(_revision, Campaign?.Revision ?? 0), offerId));
    internal void Assign(string crewId, string droneId) => Submit(new AssignPilotCommand(Guid.NewGuid().ToString("N"), Math.Max(_revision, Campaign?.Revision ?? 0), crewId, droneId));
    private void Submit(DemoCampaignCommand command)
    {
        if (_service is null || PendingRecruitment is { IsCompleted: false }) return;
        PendingRecruitment = SubmitAsync(command);
    }
    private async Task SubmitAsync(DemoCampaignCommand command)
    {
        RecruitmentStatus = Localization.Get("Demo.Repair.Pending");
        try
        {
            var response = await _service!(command, CancellationToken.None);
            _revision = response.Revision;
            _recruitment = response.Applied ? response.Recruitment : null;
            RecruitmentStatus = response.Applied ? Localization.Get("Demo.Recruitment.Confirmed") : StationRepairPresentation.Reason(response.Reason);
        }
        catch (Exception) { _recruitment = null; RecruitmentStatus = StationRepairPresentation.Reason("unavailable"); }
    }
    private void ClickRecruitment(float x, float y)
    {
        if (Recruitment is not { } state || PendingRecruitment is { IsCompleted: false }) return;
        for (int i = 0; i < state.Offers.Length && i < 3; i++)
            if (x >= 40 + 510 * i && x <= 500 + 510 * i && y >= 375 && y <= 415) { Hire(state.Offers[i].Id); return; }
        if (x < 40 || x > 1000) return;
        if (y >= 485 && y <= 525 && state.Pilots.Length > 0) _pilotIndex = (_pilotIndex + 1) % state.Pilots.Length;
        if (y >= 535 && y <= 575 && state.AvailableDroneIds.Length > 0) _droneIndex = (_droneIndex + 1) % state.AvailableDroneIds.Length;
        if (y >= 585 && y <= 625 && state.Pilots.Length > 0 && state.AvailableDroneIds.Length > 0)
            Assign(state.Pilots[_pilotIndex % state.Pilots.Length].CrewId, state.AvailableDroneIds[_droneIndex % state.AvailableDroneIds.Length]);
    }
    private void DrawRecruitment(SKCanvas canvas, float left, float top)
    {
        using var text = new SKPaint { Color = SKColors.LightGray, TextSize = 17, IsAntialias = true };
        var state = Recruitment;
        canvas.DrawText(Localization.Get("Demo.Recruitment.Title"), left + 40, top + 115, text);
        if (state is null) { canvas.DrawText(Localization.Get("Demo.Recruitment.Empty"), left + 40, top + 150, text); return; }
        canvas.DrawText(string.Format(Localization.Get("Demo.Recruitment.Cabins"), state.FreeCabins), left + 40, top + 145, text);
        bool ready = _service is not null && PendingRecruitment is not { IsCompleted: false };
        var portraits = PilotPortraitCatalog.Default;
        for (int i = 0; i < state.Offers.Length && i < 3; i++)
        {
            var offer = state.Offers[i]; float x = left + 40 + i * 510;
            canvas.Save(); canvas.ClipRect(new(x, top + 160, x + 460, top + 420));
            if (portraits.TryGet(offer.CrewId, offer.Portrait, out var bitmap))
                canvas.DrawBitmap(bitmap, portraits.SourceRect(offer.CrewId, offer.Portrait), new SKRect(x, top + 170, x + 130, top + 300));
            canvas.DrawText(offer.Name, x + 145, top + 210, text);
            canvas.DrawText(string.Format(Localization.Get("Demo.Recruitment.Skills"), offer.Accuracy, offer.Maneuver, offer.Initiative), x, top + 325, text);
            long now = _buffer?.Latest?.Snapshot.GameTimeMs ?? 0;
            canvas.DrawText(string.Format(Localization.Get("Demo.Recruitment.Terms"), offer.DailyWage, Math.Max(0, (offer.ExpiresAt - now) / 3600000)), x, top + 350, text);
            MenuStyle.DrawButton(canvas, new(x, top + 375, x + 460, top + 415), string.Format(Localization.Get("Demo.Recruitment.Hire"), offer.Fee), ready && state.FreeCabins > 0 ? ButtonState.Normal : ButtonState.Disabled);
            canvas.Restore();
        }
        if (state.Offers.IsEmpty) canvas.DrawText(Localization.Get("Demo.Recruitment.Empty"), left + 40, top + 200, text);
        string pilot = state.Pilots.IsEmpty ? Localization.Get("Demo.Recruitment.NoPilot") : string.Format(Localization.Get("Demo.Recruitment.Pilot"), state.Pilots[_pilotIndex % state.Pilots.Length].Name);
        string drone = state.AvailableDroneIds.IsEmpty ? Localization.Get("Demo.Recruitment.NoDrone") : string.Format(Localization.Get("Demo.Recruitment.Drone"), state.AvailableDroneIds[_droneIndex % state.AvailableDroneIds.Length]);
        MenuStyle.DrawButton(canvas, new(left + 40, top + 485, left + 1000, top + 525), pilot, ready ? ButtonState.Normal : ButtonState.Disabled);
        MenuStyle.DrawButton(canvas, new(left + 40, top + 535, left + 1000, top + 575), drone, ready ? ButtonState.Normal : ButtonState.Disabled);
        MenuStyle.DrawButton(canvas, new(left + 40, top + 585, left + 1000, top + 625), Localization.Get("Demo.Recruitment.Assign"), ready && !state.Pilots.IsEmpty && !state.AvailableDroneIds.IsEmpty ? ButtonState.Normal : ButtonState.Disabled);
        canvas.DrawText(RecruitmentStatus, left + 40, top + 675, text);
    }
}
