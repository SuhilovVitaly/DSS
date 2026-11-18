using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Client.UI.Screens.Trade;
using SkiaSharp;

namespace DeepSpaceSaga.Client.UI.Demo;

internal sealed class LootJettisonPanel(BattleLootPresentation presentation)
{
    private readonly List<(SKRect Bounds, Action Action)> _hits = [];
    private string? _selected;
    private long _quantity;
    internal bool Confirming { get; private set; }
    internal Task<bool>? PendingTask { get; private set; }
    internal long Quantity => _quantity;
    internal void Select(LootCargoSnapshot cargo) { if (presentation.Pending) return; _selected = cargo.ItemTypeId; _quantity = cargo.QuantityStep; Confirming = false; }
    internal void Change(long delta)
    {
        if (presentation.Pending || Selected is not { } item) return;
        long max = item.Quantity / item.QuantityStep * item.QuantityStep;
        _quantity = delta > 0 ? Math.Min(max, _quantity > max - item.QuantityStep ? max : _quantity + item.QuantityStep) : Math.Max(item.QuantityStep, _quantity - item.QuantityStep);
        Confirming = false;
    }
    private LootCargoSnapshot? Selected => presentation.State?.Cargo.FirstOrDefault(c => c.ItemTypeId == _selected);
    internal async Task<bool> ConfirmAsync()
    {
        if (!Confirming || Selected is not { } item || _quantity <= 0 || _quantity > item.Quantity || presentation.Pending) return false;
        bool result = await presentation.SubmitAsync((id, revision, battle, _) => new JettisonCargoCommand(id, revision, battle, item.ItemTypeId, _quantity), false);
        if (result) { Confirming = false; _selected = null; }
        return result;
    }
    internal void RequestConfirmation() { if (Selected is { } item && _quantity > 0 && _quantity <= item.Quantity && !presentation.Pending) Confirming = true; }
    internal bool Click(SKPoint point) { foreach (var hit in _hits) if (hit.Bounds.Contains(point)) { hit.Action(); return true; } return false; }
    internal float Draw(SKCanvas canvas, SKRect clip, float top, bool flowPending)
    {
        _hits.Clear(); float y = top;
        using var text = new SKPaint { Color = SKColors.White, TextSize = 13, IsAntialias = true };
        void Button(SKRect rect, string label, Action action, bool enabled = true)
        {
            using var paint = new SKPaint { Color = enabled && !presentation.Pending && !flowPending ? new(38, 83, 97) : new(35, 45, 50) };
            canvas.DrawRoundRect(rect, 4, 4, paint); canvas.DrawText(label, rect.Left + 6, rect.MidY + 5, text);
            if (enabled && !presentation.Pending && !flowPending && rect.Top >= clip.Top && rect.Bottom <= clip.Bottom) _hits.Add((rect, action));
        }
        canvas.DrawText(Localization.Get("loot.jettison.title"), clip.Left + 4, y += 20, text);
        if (presentation.State?.Cargo is not { IsEmpty: false } cargo) { canvas.DrawText(Localization.Get("loot.jettison.empty"), clip.Left + 4, y += 28, text); return y - top + 20; }
        foreach (var item in cargo)
        {
            y += 12;
            Button(new(clip.Left + 4, y, clip.Right - 4, y + 32), TradeItemPresentation.ItemDisplayName(item.ItemTypeId) + " · " + item.Quantity + " · " + item.UnitMassKg + " kg", () => Select(item), item.Quantity >= item.QuantityStep);
            y += 38;
            if (_selected != item.ItemTypeId) continue;
            Button(new(clip.Left + 4, y, clip.Left + 44, y + 30), "−", () => Change(-1));
            canvas.DrawText(_quantity.ToString(System.Globalization.CultureInfo.InvariantCulture), clip.Left + 58, y + 21, text);
            Button(new(clip.Left + 118, y, clip.Left + 158, y + 30), "+", () => Change(1));
            Button(new(clip.Left + 170, y, clip.Right - 4, y + 30), Localization.Get("loot.jettison.action"), RequestConfirmation);
            y += 40;
            if (!Confirming) continue;
            foreach (string line in Localization.Get("loot.jettison.confirm").Split('|')) { canvas.DrawText(line, clip.Left + 4, y += 19, text); }
            y += 10;
            Button(new(clip.Left + 4, y, clip.Left + 200, y + 32), Localization.Get("loot.jettison.action"), () => PendingTask = ConfirmAsync());
            Button(new(clip.Left + 212, y, clip.Right - 4, y + 32), Localization.Get("loot.cancel"), () => Confirming = false);
            y += 42;
        }
        return y - top + 15;
    }
}
