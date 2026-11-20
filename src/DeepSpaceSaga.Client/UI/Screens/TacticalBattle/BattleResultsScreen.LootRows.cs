using System.Globalization;
using DeepSpaceSaga.Client.UI.Controls;
using DeepSpaceSaga.Client.UI.Demo;
using DeepSpaceSaga.Client.UI.Screens.Trade;
using DeepSpaceSaga.Contracts;
using SkiaSharp;
using static DeepSpaceSaga.Client.UI.Screens.TacticalBattle.BattleDesign;

namespace DeepSpaceSaga.Client.UI.Screens.TacticalBattle;

internal sealed partial class BattleResultsScreen
{
    private readonly List<(SKRect Bounds, string ObjectId, LootRow Row)> _rowHits = [];
    private SKRect _retryBounds;
    private BattleLootPresentation? _presentation;
    private BattleLootPresentation _lootPresentation => _presentation ??=
        new(command => Flow.Session.ExecuteDemoCampaignAsync(command).AsTask());
    private bool LootInputBlocked => Flow.Pending || _lootPresentation.Pending || _lootPresentation.RetryCommand is not null;
    internal Task<bool>? LastLootAction { get; private set; }

    internal enum RowKind { Consumable, Scrap, Module, Pilot }
    internal sealed record LootRow(string Id, RowKind Kind, string Name, string Category, long? Quantity,
        long? Value, long? Mass, int? Chance, string? State, bool Enabled, string? Reason);

    internal static List<LootRow> LootRows(LootObjectSnapshot source)
    {
        var rows = new List<LootRow>();
        foreach (var item in source.Consumables)
            rows.Add(new(item.ItemTypeId, RowKind.Consumable, ItemName(item.ItemTypeId), "consumable", item.Remaining,
                null, item.UnitMassKg, null, item.Remaining == 0 ? LootAttemptStates.Collected : null, item.CanCollect, item.Reason));
        if (source.Scrap is { } scrap)
            rows.Add(new(scrap.ItemTypeId, RowKind.Scrap, ItemName(scrap.ItemTypeId), "scrap", scrap.Quantity, scrap.EstimatedValueTokens,
                scrap.RequiredMassKg, scrap.ChanceBasisPoints, scrap.State, scrap.CanAttempt, scrap.Reason));
        foreach (var item in source.Modules)
            rows.Add(new(item.InstanceId, RowKind.Module, TradeItemPresentation.ItemDisplayName(item.ItemTypeId), item.Category, null,
                item.EstimatedValueTokens, item.RequiredMassKg, item.ChanceBasisPoints, item.State, item.CanAttempt, item.Reason));
        if (source.Pilot is { Alive: true } pilot)
            rows.Add(new(pilot.SourcePilotId, RowKind.Pilot, pilot.DisplayName, "pilot", null,
                null, null, null, pilot.Captured ? "Captured" : null, !pilot.Captured && pilot.CaptureReason is null, pilot.CaptureReason));
        return rows;
    }

    private static string ItemName(string itemTypeId)
    {
        string key = "loot.item." + itemTypeId;
        string name = Localization.Get(key);
        return name == key ? TradeItemPresentation.ItemDisplayName(itemTypeId) : name;
    }

    private static string ResultsText(string key, params object[] args) => string.Format(
        CultureInfo.CurrentCulture, Localization.Get("BattleResults." + key), args);

    private void DrawLootRow(SKCanvas canvas, string objectId, LootRow row, SKRect rect)
    {
        Round(canvas, rect, Panel, BoxEdge);
        var icon = new SKRect(rect.Left + 10, rect.Top + 10, rect.Left + 42, rect.Bottom - 10);
        Round(canvas, icon, Box, BoxEdge);
        var tone = row.Category switch { "weapon" => Yellow, "shield" => Cyan, "armor" => Purple, "pilot" => Green, _ => Muted };
        Diamond(canvas, new(icon.MidX, icon.MidY), 7, tone);
        float buttonWidth = Math.Min(188, rect.Width * .3f);
        var button = new SKRect(rect.Right - buttonWidth - 12, rect.Top + 8, rect.Right - 12, rect.Bottom - 8);
        float labelWidth = Math.Max(4, button.Left - icon.Right - 24);
        T(canvas, row.Name, icon.Right + 12, rect.Top + 22, FitSize(row.Name, 16, labelWidth), Text, true);
        var details = new List<string>(4);
        if (row.Quantity is { } quantity) details.Add(ResultsText("Quantity", quantity));
        if (row.Value is { } value) details.Add(ResultsText("Price", value));
        if (row.Chance is { } chance) details.Add(ResultsText("Chance", ModuleSalvagePresentation.ChanceText(chance, CultureInfo.CurrentCulture)));
        if (row.Mass is { } mass) details.Add(ResultsText("Mass", mass));
        string caption = string.Join("   ·   ", details);
        T(canvas, caption, icon.Right + 12, rect.Top + 42, FitSize(caption, 13, labelWidth), Muted);

        string label = row.State is { } state and not LootAttemptStates.NotAttempted ? ResultsText(state) :
            row.Kind switch { RowKind.Pilot => ResultsText("Arrest"), RowKind.Consumable => ResultsText("Collect"), _ => ResultsText("Dismantle") };
        bool enabled = row.Enabled && !LootInputBlocked;
        Button(canvas, button, label, pending: !enabled);
        var hit = SKRect.Intersect(button, Contents);
        if (enabled && !hit.IsEmpty) _rowHits.Add((hit, objectId, row));
    }

    private void DrawLootFooter(SKCanvas canvas)
    {
        _retryBounds = default;
        if (_lootPresentation.RetryCommand is not null)
        {
            _retryBounds = new(ExitBounds.Left - 220, ExitBounds.Top, ExitBounds.Left - 10, ExitBounds.Bottom);
            MenuStyle.DrawButton(canvas, _retryBounds, ResultsText("Retry"), Flow.Pending || _lootPresentation.Pending
                ? ButtonState.Disabled : ButtonState.Normal);
        }
        if (_lootPresentation.LastReason is { } reason)
            T(canvas, Localization.Get("loot." + reason), Contents.Left, ExitBounds.Top - 12, 14, Red);
    }

    private bool ClickLootAction(float x, float y)
    {
        if (_retryBounds.Contains(x, y) && !Flow.Pending && !_lootPresentation.Pending)
        {
            LastLootAction = _lootPresentation.RetryAsync();
            return true;
        }
        if (LootInputBlocked || !Contents.Contains(x, y)) return false;
        foreach (var hit in _rowHits)
        {
            if (!hit.Bounds.Contains(x, y)) continue;
            LastLootAction = SubmitLootRow(hit.ObjectId, hit.Row);
            return true;
        }
        return false;
    }

    private Task<bool> SubmitLootRow(string objectId, LootRow row) => _lootPresentation.SubmitAsync(
        (id, revision, battle, _) => row.Kind switch
        {
            RowKind.Consumable => new CollectLootConsumablesCommand(id, revision, battle, objectId, row.Id, row.Quantity!.Value),
            RowKind.Scrap => new AttemptScrapCommand(id, revision, battle, objectId),
            RowKind.Module => new AttemptModuleSalvageCommand(id, revision, battle, objectId, row.Id),
            RowKind.Pilot => new ArrestPilotCommand(id, revision, battle, objectId),
            _ => throw new ArgumentOutOfRangeException(nameof(row))
        }, needsSource: false);
}
