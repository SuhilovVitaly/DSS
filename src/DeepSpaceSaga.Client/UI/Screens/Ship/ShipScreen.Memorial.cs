using System.Collections.Immutable;
using DeepSpaceSaga.Client.UI.Controls;
using DeepSpaceSaga.Client.UI.Demo;
using DeepSpaceSaga.Client.UI.Screens.TacticalBattle;
using DeepSpaceSaga.Contracts;
using SkiaSharp;

namespace DeepSpaceSaga.Client.UI.Screens.Ship;

public sealed partial class ShipScreen
{
    private int _memorialScroll;
    internal ImmutableArray<PilotMemorialEntry> Memorial => _campaign?.Invoke()?.Company?.PilotLoss?.Memorial ?? [];
    internal int MemorialScroll => _memorialScroll;
    private const int VisibleMemorialRows = 4;

    private void DrawMemorial(SKCanvas canvas, float left, float top)
    {
        var entries = Memorial;
        if (entries.IsDefault) return;
        _memorialScroll = Math.Clamp(_memorialScroll, 0, Math.Max(0, entries.Length - VisibleMemorialRows));
        using var text = new SKPaint { Color = MenuStyle.TextStatus.Color, TextSize = 17, IsAntialias = true };
        canvas.DrawText(Localization.Get("Demo.PilotLoss.Title"), left + 32, top + 510, text);
        canvas.DrawText(Localization.Get(entries.IsEmpty ? "Demo.PilotLoss.Empty" : "Demo.PilotLoss.Help"), left + 32, top + 535, text);
        canvas.Save();
        canvas.ClipRect(new(left + 28, top + 540, left + ShipLayout.PanelWidth - 28, top + 780));
        for (int i = 0; i < VisibleMemorialRows && i + _memorialScroll < entries.Length; i++)
        {
            var entry = entries[i + _memorialScroll];
            float y = top + 550 + i * 56;
            var portraits = PilotPortraitCatalog.Default;
            if (portraits.TryGet(entry.Pilot.Id, entry.Pilot.PortraitAssetId, out var bitmap))
                canvas.DrawBitmap(bitmap, portraits.SourceRect(entry.Pilot.Id, entry.Pilot.PortraitAssetId), new SKRect(left + 32, y, left + 76, y + 44));
            canvas.DrawText(PilotLossPresentation.Entry(entry), left + 86, y + 25, text);
        }
        canvas.Restore();
    }
}
