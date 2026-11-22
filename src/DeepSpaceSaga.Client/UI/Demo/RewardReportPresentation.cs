using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Client.UI.Screens.Trade;

namespace DeepSpaceSaga.Client.UI.Demo;

/// <summary>Read-only ledger view. Taken goods are historical receipts, not current stock or profit.</summary>
public static class RewardReportPresentation
{
    public static string[] Lines(DemoCampaignSnapshot? campaign)
    {
        string L(string key, params object[] values) => string.Format(Localization.Get("reward_report." + key), values);
        var lines = new List<string> { L("title") };
        var loot = campaign?.Rewards?.Loot;
        var settlement = campaign?.Company?.Roundtrip?.Settlement;
        string? battleId = settlement?.BattleId ?? loot?.BattleId;
        if (loot?.BattleId != battleId) loot = null;
        if (battleId is null) return [L("title"), L("empty")];
        lines.Add(battleId);
        foreach (var payment in campaign?.Rewards?.BountyPayment?.Entries ?? [])
            if (payment.VictoryBattleId == battleId)
                lines.Add(L("bounty", payment.BountyId, payment.DueTokens, payment.PaidTokens));
        lines.Add(L("notice"));
        if (settlement?.BattleId == battleId)
        {
            lines.Add(L("losses", settlement.LostDroneIds.Length, settlement.ConsumedTorpedoes));
            foreach (var id in settlement.LostDroneIds) lines.Add("  " + id);
        }
        foreach (var pilot in campaign?.Company?.PilotLoss?.Memorial ?? [])
            if (pilot.BattleId == battleId) lines.Add(L("lost_pilot", pilot.Pilot.DisplayName));
        if (loot is null) return lines.ToArray();
        lines.Add(L(loot.Phase == LootPhases.Closed ? "discarded" : "remaining"));
        foreach (var source in loot.Objects)
        {
            lines.Add(source.ObjectId);
            foreach (var item in source.Consumables)
                lines.Add(L("consumable", TradeItemPresentation.ItemDisplayName(item.ItemTypeId),
                    item.OriginalQuantity is { } original ? (original - item.Remaining).ToString() : "?", item.Remaining));
            if (source.Scrap is { } scrap)
                lines.Add(L("item", TradeItemPresentation.ItemDisplayName(scrap.ItemTypeId), scrap.Quantity, State(scrap.State, loot.Phase)));
            foreach (var module in source.Modules)
                lines.Add(L("module", TradeItemPresentation.ItemDisplayName(module.ItemTypeId), State(module.State, loot.Phase), module.EstimatedValueTokens));
            if (source.Pilot is { } pilot)
                lines.Add(L(pilot.Captured ? "captured" : "left_pilot", pilot.DisplayName));
        }
        return lines.ToArray();
    }

    private static string State(string state, string phase) =>
        phase == LootPhases.Closed && state is LootAttemptStates.NotAttempted or LootAttemptStates.ExtractedWaiting
            ? Localization.Get("loot.state." + LootAttemptStates.Discarded)
            : state == LootAttemptStates.ExtractedWaiting ? Localization.Get("BattleResults.Recovered")
            : Localization.Get("loot.state." + state);
}
