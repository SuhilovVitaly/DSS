using System.Collections.Immutable;
using DeepSpaceSaga.Contracts;

namespace DeepSpaceSaga.Client.UI.Demo;

public sealed record BattleLootActionRow(string Id, string Label, long Quantity, int? ChanceBasisPoints,
    string? Status, bool Enabled, string? Reason);

/// <summary>Rows for the shared object panel; input uses the same presentation pending gate.</summary>
public sealed class BattleLootConsumablesPanel(BattleLootPresentation presentation)
{
    public ImmutableArray<BattleLootActionRow> Rows(Func<string, string> itemName)
    {
        if (presentation.Selected is not { } source) return [];
        var rows = source.Consumables.Where(c => c.Remaining > 0).Select(c => new BattleLootActionRow(c.ItemTypeId,
            itemName(c.ItemTypeId), c.Remaining, null, null, !presentation.Pending && c.CanCollect, c.Reason)).ToImmutableArray();
        if (source.Scrap is { } scrap) rows = rows.Add(new("scrap", itemName(scrap.ItemTypeId), scrap.Quantity,
            scrap.ChanceBasisPoints, scrap.State, !presentation.Pending && scrap.CanAttempt, scrap.Reason));
        return rows;
    }

    public Task<bool> ActivateAsync(string rowId, long quantity) => rowId == "scrap"
        ? presentation.ScrapAsync() : presentation.CollectAsync(rowId, quantity);
}
