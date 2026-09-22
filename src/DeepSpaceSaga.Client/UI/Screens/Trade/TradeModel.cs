using DeepSpaceSaga.Contracts;

namespace DeepSpaceSaga.Client.UI.Screens.Trade;

internal enum TradeMode { Buy, Sell, Refuel }
internal enum TradeFilter { All, Resources, Goods }
internal enum TradeSort { Name, Price, StationStock, Cargo }

internal readonly record struct TradeQuote(long Maximum, long Total, long CargoQuantity,
    long AmountBefore, long AmountAfter, long BalanceAfter, string? DisabledReason, string LimitReason)
{
    internal static TradeQuote Calculate(StationInventoryItemSnapshot? item, InstalledModuleSnapshot? module,
        TradeMode mode, long quantity, long credits)
    {
        TradeQuote Disabled(string reason) => new(0, 0, 0, 0, 0, credits, reason, reason);
        if (item is null) return Disabled("SelectItem");
        if (module is null) return Disabled(mode == TradeMode.Refuel ? "NoTank" : "NoContainer");
        if (module.PowerState != "On" || module.OperationalState != "Ready" || module.StructurePoints <= 0)
            return Disabled("ModuleUnavailable");
        if (item.UnitPriceCredits <= 0 || item.UnitMassKg < 0 || credits < 0) return Disabled("InvalidData");
        // A malformed authoritative capacity must never widen the Engine's MaxSellableQuantity.
        if (mode == TradeMode.Sell && item.FreeStockCapacity is < 0) return Disabled("InvalidData");
        long cargo = module.Cargo.IsDefaultOrEmpty ? 0 : module.Cargo.FirstOrDefault(c => c.ItemTypeId == item.ItemTypeId)?.Quantity ?? 0;
        long available = Math.Max(0, module.AvailableCapacityKg ?? 0);
        long before = mode == TradeMode.Refuel ? module.FuelAmountKg ?? 0 : available;
        long maximum;
        string limit;
        if (mode == TradeMode.Sell)
        {
            maximum = Math.Min(cargo, item.MaxSellableQuantity);
            // MaxSellableQuantity already carries both station bounds; FreeStockCapacity only names the
            // cause. On a budget/storage tie the storage reason wins (presentation tie-break only).
            limit = cargo <= item.MaxSellableQuantity ? "CargoLimit"
                : item.FreeStockCapacity is { } free && free <= item.MaxSellableQuantity ? "StationStorageLimit"
                : "StationBudgetLimit";
            long balanceLimit = (long.MaxValue - credits) / item.UnitPriceCredits;
            if (maximum > balanceLimit) { maximum = balanceLimit; limit = "BalanceLimit"; }
        }
        else
        {
            maximum = Math.Max(0, item.StockQuantity);
            limit = "StockLimit";
            long affordable = credits / item.UnitPriceCredits;
            if (affordable < maximum) { maximum = affordable; limit = "MoneyLimit"; }
            long space = mode == TradeMode.Refuel
                ? Math.Max(0, (module.FuelCapacityKg ?? 0) - before)
                : item.UnitMassKg == 0 ? long.MaxValue : available / item.UnitMassKg;
            if (space < maximum) { maximum = space; limit = mode == TradeMode.Refuel ? "TankLimit" : "CapacityLimit"; }
        }
        maximum = Math.Max(0, maximum);
        string? reason = quantity <= 0 ? "EnterQuantity" : quantity > maximum ? limit : null;
        try
        {
            long total = checked(quantity * item.UnitPriceCredits);
            long delta = checked(quantity * (mode == TradeMode.Refuel ? 1 : item.UnitMassKg));
            long after = mode == TradeMode.Buy ? checked(before - delta) : checked(before + delta);
            long balance = mode == TradeMode.Sell ? checked(credits + total) : checked(credits - total);
            return new(maximum, total, cargo, before, after, balance, reason, limit);
        }
        catch (OverflowException) { return new(maximum, 0, cargo, before, before, credits, "ValueOverflow", limit); }
    }
}

/// <summary>Per-session bounded receipts survive closing and reopening the trade window.</summary>
internal sealed class TradeJournal
{
    internal sealed record Entry(string CommandId, string ItemId, string ModuleId, TradeMode Mode,
        long RequestedQuantity, long UnitPrice, CommandResult? Result = null, string ModuleLabel = "");
    private readonly List<Entry> _entries = new();
    internal IReadOnlyList<Entry> Entries => _entries;
    internal Entry? Latest => _entries.LastOrDefault();
    internal bool IsPending => _entries.Any(e => e.Result is null);
    internal void Track(Entry entry)
    {
        _entries.Add(entry);
        if (_entries.Count > 50) _entries.RemoveAt(0);
    }
    internal void Refresh(SnapshotBuffer? buffer)
    {
        if (buffer is null) return;
        for (int i = 0; i < _entries.Count; i++)
        {
            var entry = _entries[i];
            if (entry.Result is not null) continue;
            if (buffer.FindCommandResult(entry.CommandId) is { Status: not CommandResultStatus.Deferred } result)
                _entries[i] = entry with { Result = result };
        }
    }
}

/// <summary>Selection and cached market projection. No rendering or engine dependencies.</summary>
internal sealed class TradeModel
{
    internal const string FuelId = "item.fuel";
    private AuthoritativeSnapshot? _snapshot;
    private bool _dirty = true;
    private string _query = "";
    internal string Query { get => _query; set { _query = value; _dirty = true; } }
    internal TradeFilter Filter { get; private set; }
    internal TradeSort Sort { get; private set; }
    internal bool Descending { get; private set; }
    internal bool CargoOnly { get; private set; }
    internal TradeMode Mode { get; private set; }
    internal bool FuelMode => Mode == TradeMode.Refuel;
    internal string? SelectedItemId { get; private set; }
    internal string? SelectedModuleId { get; private set; }
    internal long Quantity { get; set; } = 1;
    internal StationInventoryItemSnapshot[] Rows { get; private set; } = [];
    internal InstalledModuleSnapshot[] Modules { get; private set; } = [];
    internal InstalledModuleSnapshot? Module => Modules.FirstOrDefault(m => m.ModuleId == SelectedModuleId);
    internal StationInventoryItemSnapshot? Item => Rows.FirstOrDefault(i => i.ItemTypeId == SelectedItemId);
    internal TradeQuote Quote => TradeQuote.Calculate(Item, Module, Mode, Quantity, _snapshot?.PlayerCredits ?? 0);

    internal void Refresh(AuthoritativeSnapshot? snapshot)
    {
        if (!ReferenceEquals(snapshot, _snapshot)) { _snapshot = snapshot; _dirty = true; }
        if (!_dirty) return;
        _dirty = false;
        string command = Mode switch { TradeMode.Sell => TradeCommandTypes.Sell, TradeMode.Refuel => TradeCommandTypes.Refuel, _ => TradeCommandTypes.Buy };
        Modules = snapshot is null || snapshot.InstalledModules.IsDefaultOrEmpty ? [] : snapshot.InstalledModules
            .Where(m => !m.CommandTypeIds.IsDefaultOrEmpty && m.CommandTypeIds.Contains(command)).OrderBy(m => m.Position).ToArray();
        if (!Modules.Any(m => m.ModuleId == SelectedModuleId)) SelectedModuleId = Modules.FirstOrDefault()?.ModuleId;
        var items = snapshot?.DockedStationTrade?.Items;
        IEnumerable<StationInventoryItemSnapshot> rows = items is null || items.Value.IsDefaultOrEmpty ? [] : items.Value;
        rows = rows.Where(i => (i.ItemTypeId == FuelId) == FuelMode);
        if (!FuelMode)
        {
            rows = rows.Where(i => Filter == TradeFilter.All || i.Category == (Filter == TradeFilter.Resources ? TradeItemCategories.Resource : TradeItemCategories.Good));
            if (CargoOnly) rows = rows.Where(i => Cargo(i.ItemTypeId) > 0);
            if (!string.IsNullOrWhiteSpace(Query)) rows = rows.Where(i => TradeItemPresentation.ItemDisplayName(i.ItemTypeId).Contains(Query, StringComparison.CurrentCultureIgnoreCase)
                || i.ItemTypeId.Contains(Query, StringComparison.OrdinalIgnoreCase));
        }
        Rows = rows.ToArray();
        Array.Sort(Rows, (a, b) =>
        {
            int comparison = Sort switch
            {
                TradeSort.Price => a.UnitPriceCredits.CompareTo(b.UnitPriceCredits),
                TradeSort.StationStock => a.StockQuantity.CompareTo(b.StockQuantity),
                TradeSort.Cargo => Cargo(a.ItemTypeId).CompareTo(Cargo(b.ItemTypeId)),
                _ => StringComparer.CurrentCultureIgnoreCase.Compare(TradeItemPresentation.ItemDisplayName(a.ItemTypeId), TradeItemPresentation.ItemDisplayName(b.ItemTypeId))
            };
            if (comparison == 0) comparison = StringComparer.Ordinal.Compare(a.ItemTypeId, b.ItemTypeId);
            return Descending ? -comparison : comparison;
        });
        if (FuelMode) SelectedItemId = Rows.FirstOrDefault()?.ItemTypeId;
        else if (SelectedItemId is not null && !Rows.Any(i => i.ItemTypeId == SelectedItemId)) SelectedItemId = null;
    }
    internal long Cargo(string itemId) => Module?.Cargo.IsDefaultOrEmpty == false
        ? Module.Cargo.FirstOrDefault(c => c.ItemTypeId == itemId)?.Quantity ?? 0 : 0;
    internal void Select(string itemId) { SelectedItemId = itemId; Quantity = 1; }
    internal void SelectModule(string id) { SelectedModuleId = id; _dirty = true; Refresh(_snapshot); }
    internal void SetMode(TradeMode mode)
    {
        bool changedTab = (mode == TradeMode.Refuel) != FuelMode;
        Mode = mode; Quantity = 1;
        if (changedTab) { SelectedItemId = null; SelectedModuleId = null; }
        _dirty = true; Refresh(_snapshot);
    }
    internal void SetFilter(TradeFilter filter) { Filter = filter; _dirty = true; Refresh(_snapshot); }
    internal void ToggleCargoOnly() { CargoOnly = !CargoOnly; _dirty = true; Refresh(_snapshot); }
    internal void SetSort(TradeSort sort) { Descending = Sort == sort && !Descending; Sort = sort; _dirty = true; Refresh(_snapshot); }
    internal void FillTank(int percent)
    {
        if (!FuelMode || Module is not { FuelCapacityKg: { } capacity } module) return;
        long target = (long)((decimal)capacity * percent / 100);
        Quantity = Math.Min(Quote.Maximum, Math.Max(0, target - (module.FuelAmountKg ?? 0)));
    }
}
