using DeepSpaceSaga.Client.UI.Controls;
using DeepSpaceSaga.Client.UI.Demo;
using DeepSpaceSaga.Contracts;
using SkiaSharp;

namespace DeepSpaceSaga.Client.UI.Screens.Finance;

public sealed partial class FinanceScreen
{
    private readonly Func<DemoCampaignCommand, CancellationToken, ValueTask<DemoCampaignResponse>>? _service;
    private readonly Func<DemoCampaignSnapshot?>? _campaign;
    private CrewUpkeepSnapshot? _upkeep;
    private long _upkeepRevision;
    private long _debtAmount = 10;
    internal Task? PendingDebt { get; private set; }
    internal string DebtStatus { get; private set; } = "";
    private DemoCampaignSnapshot? Campaign => _campaign?.Invoke() ?? _buffer?.Latest?.Snapshot.DemoCampaign;
    internal CrewUpkeepSnapshot? Upkeep => _upkeep ?? Campaign?.Company?.Upkeep;
    internal void PayDebt(long amount)
    {
        if (_service is null || PendingDebt is { IsCompleted: false } || amount <= 0) return;
        PendingDebt = PayDebtAsync(new PayCrewDebtCommand(Guid.NewGuid().ToString("N"), Math.Max(_upkeepRevision, Campaign?.Revision ?? 0), amount));
    }
    private async Task PayDebtAsync(PayCrewDebtCommand command)
    {
        DebtStatus = Localization.Get("Demo.Repair.Pending");
        try
        {
            var response = await _service!(command, CancellationToken.None);
            _upkeepRevision = response.Revision;
            _upkeep = response.Applied ? response.Upkeep : null;
            DebtStatus = response.Applied ? Localization.Get("Demo.Upkeep.Confirmed") : StationRepairPresentation.Reason(response.Reason);
        }
        catch (Exception) { _upkeep = null; DebtStatus = StationRepairPresentation.Reason("unavailable"); }
    }
    private void ClickUpkeep(float x, float y)
    {
        if (y < 235 || y > 270 || Upkeep is not { } state || PendingDebt is { IsCompleted: false }) return;
        if (x >= 40 && x <= 85) _debtAmount = Math.Max(1, _debtAmount - 10);
        else if (x >= 95 && x <= 140) _debtAmount = Math.Min(state.Outstanding, _debtAmount > long.MaxValue - 10 ? long.MaxValue : _debtAmount + 10);
        else if (x >= 150 && x <= 510) PayDebt(Math.Min(_debtAmount, state.Outstanding));
    }
    private void DrawUpkeep(SKCanvas canvas, float left, float top)
    {
        if (Upkeep is not { } state) return;
        using var text = new SKPaint { Color = SKColors.LightGray, TextSize = 15, IsAntialias = true };
        canvas.Save(); canvas.ClipRect(new(left + 35, top + 85, left + 550, top + 292));
        canvas.DrawText(Localization.Get("Demo.Upkeep.Title"), left + 40, top + 105, text);
        canvas.DrawText(string.Format(Localization.Get("Demo.Upkeep.Totals"), state.Accrued, state.Paid, state.Outstanding), left + 40, top + 130, text);
        canvas.DrawText(string.Format(Localization.Get("Demo.Upkeep.Food"), state.PeopleFed, state.FoodRemaining, state.MissingRations), left + 40, top + 155, text);
        long now = _buffer?.Latest?.Snapshot.GameTimeMs ?? 0;
        canvas.DrawText(string.Format(Localization.Get("Demo.Upkeep.Next"), Math.Max(0, (state.NextDueAt - now) / 3600000)), left + 40, top + 180, text);
        canvas.DrawText(DebtStatus.Length == 0 ? Localization.Get("Demo.Upkeep.Help") : DebtStatus, left + 40, top + 210, text);
        bool ready = _service is not null && PendingDebt is not { IsCompleted: false } && state.Outstanding > 0;
        var button = ready ? ButtonState.Normal : ButtonState.Disabled;
        MenuStyle.DrawButton(canvas, new(left + 40, top + 235, left + 85, top + 270), "−", button);
        MenuStyle.DrawButton(canvas, new(left + 95, top + 235, left + 140, top + 270), "+", button);
        MenuStyle.DrawButton(canvas, new(left + 150, top + 235, left + 510, top + 270), string.Format(Localization.Get("Demo.Upkeep.Pay"), Math.Min(_debtAmount, state.Outstanding)), button);
        canvas.Restore();
    }
}
