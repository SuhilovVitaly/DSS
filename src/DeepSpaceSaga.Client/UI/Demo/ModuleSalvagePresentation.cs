using System.Collections.Immutable;
using System.Globalization;
using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Client.UI.Screens.Trade;

namespace DeepSpaceSaga.Client.UI.Demo;

public sealed record ModuleSalvageRow(string InstanceId, string Name, string Condition, string EstimatedValue,
    string Chance, string Status, bool Enabled, string? Reason);

public sealed class ModuleSalvagePresentation(BattleLootPresentation presentation)
{
    public static string ChanceText(int basisPoints, CultureInfo culture) => (basisPoints / 100m).ToString("0.##", culture);

    public ImmutableArray<ModuleSalvageRow> Rows(CultureInfo culture)
    {
        if (presentation.Selected is not { } source) return [];
        return source.Modules.Select(m => new ModuleSalvageRow(m.InstanceId,
            TradeItemPresentation.ItemDisplayName(m.ItemTypeId), Localization.Get("loot.intact"),
            string.Format(culture, Localization.Get("loot.estimated_value"), m.EstimatedValueTokens),
            string.Format(culture, Localization.Get("loot.chance"), ChanceText(m.ChanceBasisPoints, culture)),
            Localization.Get("loot.state." + m.State), !presentation.Pending && m.CanAttempt,
            m.Reason is null ? null : Localization.Get("loot." + m.Reason))).ToImmutableArray();
    }

    public Task<bool> AttemptAsync(string instanceId) => presentation.SubmitAsync((id, revision, battle, source) =>
        new AttemptModuleSalvageCommand(id, revision, battle, source, instanceId));
}
