using System.Collections.Immutable;
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
    private readonly List<(SKRect Bounds, string ObjectId, LootRow Row, long? Quantity)> _rowHits = [];
    private SKRect _retryBounds;
    private SKRect _collectAllBounds;
    internal SKRect CollectAllBounds => _collectAllBounds;
    private bool HasLootActivity => Flow.Pending || _lootPresentation.Pending || _lootPresentation.RetryCommand is not null;
    private bool LootInputBlocked => HasLootActivity || SaveLoadInProgress;
    private bool ExitBlocked => LootInputBlocked || CurrentLoot?.Operations.IsEmpty == false;
    internal IReadOnlyList<(SKRect Bounds, string ObjectId, LootRow Row, long? Quantity)> LootActionHits => _rowHits;
    internal Task<bool>? LastLootAction { get; private set; }

    internal enum RowKind { Consumable, Scrap, Module, Pilot }
    internal sealed record LootRow(string Id, RowKind Kind, string Name, string Category, long? Quantity,
        long? Value, long? Mass, int? Chance, string? State, bool Enabled, string? Reason, long? OriginalQuantity = null);

    internal static List<LootRow> LootRows(LootObjectSnapshot source)
    {
        var rows = new List<LootRow>();
        foreach (var item in source.Consumables)
            rows.Add(new(item.ItemTypeId, RowKind.Consumable, ItemName(item.ItemTypeId), "consumable", item.Remaining,
                null, item.UnitMassKg, null, item.Remaining == 0 ? LootAttemptStates.Collected : null, item.CanCollect,
                item.Reason, item.OriginalQuantity));
        if (source.Scrap is { } scrap)
            rows.Add(new(scrap.ItemTypeId, RowKind.Scrap, ItemName(scrap.ItemTypeId), "scrap", scrap.Quantity, scrap.EstimatedValueTokens,
                scrap.RequiredMassKg, scrap.ChanceBasisPoints, scrap.State, scrap.CanClaimExtracted || scrap.CanAttempt ||
                scrap.State == LootAttemptStates.NotAttempted && scrap.Reason == BattleLootReasons.CargoFull, scrap.Reason));
        foreach (var item in source.Modules)
            rows.Add(new(item.InstanceId, RowKind.Module, TradeItemPresentation.ItemDisplayName(item.ItemTypeId), item.Category, null,
                item.EstimatedValueTokens, item.RequiredMassKg, item.ChanceBasisPoints, item.State, item.CanClaimExtracted || item.CanAttempt ||
                item.State == LootAttemptStates.NotAttempted && item.Reason == BattleLootReasons.CargoFull, item.Reason));
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

    private void DrawLootRow(SKCanvas canvas, string objectId, LootRow row, SKRect rect, LootSessionSnapshot loot)
    {
        Round(canvas, rect, Panel, BoxEdge);
        var icon = new SKRect(rect.Left + 10, rect.Top + 10, rect.Left + 42, rect.Bottom - 10);
        Round(canvas, icon, Box, BoxEdge);
        var tone = row.Category switch { "weapon" => Yellow, "shield" => Cyan, "armor" => Purple, "pilot" => Green, _ => Muted };
        Diamond(canvas, new(icon.MidX, icon.MidY), 7, tone);
        float buttonWidth = Math.Min(188, rect.Width * .24f);
        var button = new SKRect(rect.Right - buttonWidth - 30, rect.Top + 8, rect.Right - 30, rect.Bottom - 8);
        var key = MaterialRow(objectId, row);
        var operation = key is null ? null : loot.Operations.FirstOrDefault(op => op.Row == key);
        bool selectQuantity = row.Kind == RowKind.Consumable && row.Quantity > 1 && operation is null;
        var one = new SKRect(button.Left - buttonWidth - 8, button.Top, button.Left - 8, button.Bottom);
        float labelWidth = Math.Max(4, (selectQuantity ? one.Left : button.Left) - icon.Right - 24);
        T(canvas, row.Name, icon.Right + 12, rect.Top + 22, FitSize(row.Name, 16, labelWidth), Text, true);
        var details = new List<string>(4);
        if (row.Quantity is { } quantity) details.Add(ResultsText("Quantity", quantity));
        if (row.Value is { } value) details.Add(ResultsText("Price", value));
        bool extracted = row.State == LootAttemptStates.ExtractedWaiting;
        if (!extracted && row.Chance is { } chance)
            details.Add(ResultsText("Chance", ModuleSalvagePresentation.ChanceText(chance, CultureInfo.CurrentCulture)));
        if (row.Mass is { } mass) details.Add(ResultsText("Mass", mass));
        if (extracted) details.Add(ResultsText("Recovered"));
        if (row.Kind == RowKind.Consumable && row.OriginalQuantity is { } original && row.Quantity is > 0 && row.Quantity < original)
            details.Add(ResultsText("Partial", original - row.Quantity.Value, original));
        string caption = string.Join("   ·   ", details);
        T(canvas, caption, icon.Right + 12, rect.Top + 42, FitSize(caption, 13, labelWidth), Muted);

        string label = operation is not null ? ResultsText("Working") :
            extracted ? ResultsText("Collect") :
            row.State is { } state and not LootAttemptStates.NotAttempted ? ResultsText(state) :
            row.Kind switch { RowKind.Pilot => ResultsText("Arrest"), RowKind.Consumable => ResultsText("Collect"), _ => ResultsText("Dismantle") };
        bool enabled = row.Enabled && !LootInputBlocked && operation is null;
        Button(canvas, button, label, pending: !enabled);
        if (operation is not null)
        {
            var track = new SKRect(button.Right + 5, button.Top, button.Right + 14, button.Bottom);
            Round(canvas, track, Box, BoxEdge);
            var fill = ProgressFill(track, Progress(operation));
            if (!fill.IsEmpty) Round(canvas, fill, Yellow, radius: 1);
        }
        var hit = SKRect.Intersect(button, Contents);
        if (enabled && !hit.IsEmpty) _rowHits.Add((hit, objectId, row, null));
        if (selectQuantity)
        {
            Button(canvas, one, ResultsText("CollectOne"), pending: !enabled);
            var oneHit = SKRect.Intersect(one, Contents);
            if (enabled && !oneHit.IsEmpty) _rowHits.Add((oneHit, objectId, row, 1));
        }
    }

    private void DrawLootFooter(SKCanvas canvas)
    {
        _retryBounds = default;
        _collectAllBounds = default;
        if (CurrentLoot is { } loot)
        {
            _collectAllBounds = new(Contents.Left, ExitBounds.Top, Contents.Left + 200, ExitBounds.Bottom);
            MenuStyle.DrawButton(canvas, _collectAllBounds, ResultsText("CollectAll"),
                CanCollectAll(loot) ? ButtonState.Normal : ButtonState.Disabled);
        }
        if (_lootPresentation.RetryCommand is not null)
        {
            _retryBounds = new(ExitBounds.Left - 220, ExitBounds.Top, ExitBounds.Left - 10, ExitBounds.Bottom);
            MenuStyle.DrawButton(canvas, _retryBounds, ResultsText("Retry"), Flow.Pending || _lootPresentation.Pending
                ? ButtonState.Disabled : ButtonState.Normal);
        }
        if (_lootPresentation.LastReason is { } reason and not (BattleLootReasons.OperationNotDue or DemoCampaignReasons.StaleRevision))
            T(canvas, Localization.Get("loot." + reason), Contents.Left, ExitBounds.Top - 35, 14, Red);
    }

    private bool ClickLootAction(float x, float y)
    {
        if (_retryBounds.Contains(x, y) && !Flow.Pending && !_lootPresentation.Pending && !SaveLoadInProgress)
        {
            LastLootAction = _lootPresentation.RetryCommand is StartLootOperationsCommand { Row: null }
                ? CollectAllAsync(retry: true) : _lootPresentation.RetryAsync();
            return true;
        }
        if (_collectAllBounds.Contains(x, y) && CurrentLoot is { } loot && CanCollectAll(loot))
        {
            LastLootAction = CollectAllAsync();
            return true;
        }
        if (LootInputBlocked || !Contents.Contains(x, y)) return false;
        foreach (var hit in _rowHits)
        {
            if (!hit.Bounds.Contains(x, y)) continue;
            LastLootAction = SubmitLootRow(hit.ObjectId, hit.Row, hit.Quantity);
            return true;
        }
        return false;
    }

    private static LootRowKey? MaterialRow(string objectId, LootRow row) => row.Kind switch
    {
        RowKind.Consumable => new(objectId, LootRowKind.Consumable, row.Id),
        RowKind.Scrap => new(objectId, LootRowKind.Scrap, row.Id),
        RowKind.Module => new(objectId, LootRowKind.Module, row.Id),
        _ => null
    };

    private Task<bool> SubmitLootRow(string objectId, LootRow row, long? quantity) => _lootPresentation.SubmitAsync(
        (id, revision, battle, _) => MaterialRow(objectId, row) is { } key
            ? row.State == LootAttemptStates.ExtractedWaiting ? new ClaimExtractedLootCommand(id, revision, battle, key)
                : new StartLootOperationsCommand(id, revision, battle, key) { RequestedQuantity = quantity }
            : new ArrestPilotCommand(id, revision, battle, objectId), needsSource: false);

    private bool CanCollectAll(LootSessionSnapshot loot)
    {
        if (LootInputBlocked) return false;
        var running = loot.Operations.Select(op => op.Row).ToHashSet();
        return loot.Objects.Any(source => LootRows(source).Any(row => row.State is null or LootAttemptStates.NotAttempted &&
            MaterialRow(source.ObjectId, row) is { } key && !running.Contains(key)));
    }

    private async Task<bool> CollectAllAsync(bool retry = false)
    {
        bool applied = retry ? await _lootPresentation.RetryAsync() : await _lootPresentation.SubmitAsync(
            (id, revision, battle, _) => new StartLootOperationsCommand(id, revision, battle), needsSource: false);
        if (applied && CurrentLoot is { } loot)
            ImmutableInterlocked.Update(ref _expandedSources, set => set.Union(loot.Operations.Select(op => op.Row.ObjectId)));
        return applied;
    }
}
