using SkiaSharp;
using static DeepSpaceSaga.Client.UI.Screens.TacticalBattle.BattleDesign;

namespace DeepSpaceSaga.Client.UI.Screens.TacticalBattle;

internal sealed class PilotCardColumn
{
    private IReadOnlyList<PilotCard> _cards = [];
    public SKRect Bounds => _cards.Count == 0 ? SKRect.Empty : new(6, 6, 196, 88 + 87 * (_cards.Count - 1));
    public static SKRect Rect(int index) => new(6, 6 + 87 * index, 196, 88 + 87 * index);
    public string? Hit(SKPoint p)
    { for (int i = 0; i < _cards.Count; i++) if (Rect(i).Contains(p)) return _cards[i].UnitId; return null; }
    public void Draw(SKCanvas c, IReadOnlyList<PilotCard> cards, PilotPortraitCatalog portraits)
    {
        _cards = cards;
        for (int i = 0; i < cards.Count; i++)
        {
            var card = cards[i]; var r = Rect(i);
            Recording!.Rects["pilot:" + card.UnitId] = r;
            PanelBox(c, r, card.Selected, 5, "ui.pilot-card");
            Portrait(c, portraits, card.PilotId, card.Name, SKRect.Create(r.Left + 6, r.Top + 5, 24, 24));
            T(c, card.Name, r.Left + 36, r.Top + 16, 11.5f, Text, true);
            T(c, card.Weapon, r.Left + 36, r.Top + 27, 8.5f, Muted);
            if (card.Chip.Length > 0)
            {
                float w = Measure(card.Chip, 9, card.ChipAccent) + 9;
                var chip = new SKRect(r.Right - 6 - w, r.Top + 6, r.Right - 6, r.Top + 19);
                Round(c, chip, card.ChipAccent ? YellowDark : Box, card.ChipAccent ? Yellow.WithAlpha(170) : BoxEdge, .8f, role: "ui.pilot-card.chip");
                T(c, card.Chip, chip.MidX, chip.MidY + 3.2f, 9, card.ChipAccent ? Yellow : Text, card.ChipAccent, SKTextAlign.Center);
            }
            Line(c, r.Left + 6, r.Right - 6, r.Top + 33);
            for (int j = 0; j < card.Stats.Count; j++)
            {
                var stat = card.Stats[j]; float y = r.Top + 44 + 11 * j;
                T(c, stat.Label, r.Left + 7, y, 9, Text);
                T(c, stat.Value, r.Right - 7, y, 9.5f, Text, true, SKTextAlign.Right);
                float x0 = r.Left + 60, x1 = r.Right - 41;
                Stroke(c, new(x0, y - 3), new(x1, y - 3), Track, 3);
                var color = TacticalBattleSkinRenderScope.Color("ui.pilot-card.bar." + stat.Kind, "fill", stat.Tone);
                if (stat.Fill > 0) Stroke(c, new(x0, y - 3), new(x0 + (x1 - x0) * (float)Math.Clamp(stat.Fill, 0, 1), y - 3), color, 3);
            }
            float fs = 8.5f;
            float Width() => card.Segments.Sum(s => Measure(s.Text, fs, s.Bold));
            while (fs > 7 && Width() > 176) fs -= .25f;
            float x = r.Left + 7;
            foreach (var segment in card.Segments)
            { T(c, segment.Text, x, r.Bottom - 6, fs, segment.Tone, segment.Bold); x += Measure(segment.Text, fs, segment.Bold); }
            // Retain the complete semantic line as well as painted colored runs.
            Recording?.Texts.Add(new(card.PhaseText, Muted));
        }
    }
    internal static void Portrait(SKCanvas c, PilotPortraitCatalog catalog, string id, string name, SKRect r)
    {
        Round(c, r, PortraitBg);
        if (catalog.TryGet(id, out var image))
        {
            c.Save(); using var clip = new SKRoundRect(r, 3, 3); c.ClipRoundRect(clip, antialias: true);
            using var p = new SKPaint { IsAntialias = true, FilterQuality = SKFilterQuality.High };
            c.DrawBitmap(image, new SKRect(170, 70, 854, 754), r, p); c.Restore();
        }
        else T(c, string.Concat(name.Split(' ', StringSplitOptions.RemoveEmptyEntries).Select(s => s[..1])), r.MidX, r.MidY + 4, 11, Text, true, SKTextAlign.Center);
    }
}
