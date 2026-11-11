using DeepSpaceSaga.Motion.TacticalBattle;
using SkiaSharp;
using static DeepSpaceSaga.Client.UI.Screens.TacticalBattle.BattleDesign;

namespace DeepSpaceSaga.Client.UI.Screens.TacticalBattle;

internal sealed class PilotCardColumn
{
    private IReadOnlyList<PilotCard> _cards = [];
    private float _scroll;
    private string? _selectedId;
    private float _selectedHeight;
    public PilotColumnLayout Layout { get; private set; } = new([], 0);
    public SKRect Bounds => Layout.Bounds;
    public SKRect Rect(int index) => Layout.Visible(index);

    public string? Hit(SKPoint point)
    {
        for (int i = 0; i < _cards.Count; i++)
            if (!Rect(i).IsEmpty && Rect(i).Contains(point))
                return _cards[i].UnitId;
        return null;
    }

    public bool Scroll(SKPoint point, float delta)
    {
        if (delta == 0 || Layout.MaxScroll == 0 || !PilotColumnLayout.Viewport.Contains(point))
            return false;
        _scroll = Math.Clamp(_scroll - 40 * Math.Sign(delta), 0, Layout.MaxScroll);
        return true;
    }

    public void Arrange(IReadOnlyList<PilotCard> cards, Func<PilotCard, float>? expandedHeight = null)
    {
        int selected = -1;
        var heights = new float[cards.Count];
        for (int i = 0; i < cards.Count; i++)
        {
            heights[i] = PilotColumnLayout.BaseHeight;
            if (!cards[i].Selected) continue;
            selected = i;
            heights[i] += expandedHeight?.Invoke(cards[i]) ?? 0;
        }

        string? selectedId = selected < 0 ? null : cards[selected].UnitId;
        float selectedHeight = selected < 0 ? 0 : heights[selected];
        int reveal = selectedId != _selectedId || selectedHeight != _selectedHeight ? selected : -1;
        Layout = new(heights, _scroll, reveal);
        _scroll = Layout.Scroll;
        _selectedId = selectedId;
        _selectedHeight = selectedHeight;
        _cards = cards;
    }

    public void Draw(SKCanvas c, IReadOnlyList<PilotCard> cards, PilotPortraitCatalog portraits)
    {
        Arrange(cards);
        Draw(c, portraits);
    }

    public void Draw(SKCanvas c, PilotPortraitCatalog portraits)
    {
        c.Save();
        try
        {
            c.ClipRect(PilotColumnLayout.Viewport);
            DrawCards(c, portraits);
        }
        finally { c.Restore(); }

        if (Layout.MaxScroll == 0) return;
        var track = SKRect.Create(198, PilotColumnLayout.Viewport.Top, 2, PilotColumnLayout.Viewport.Height);
        float thumbHeight = Math.Max(24, track.Height * track.Height / Layout.ContentHeight);
        float thumbTop = track.Top + (track.Height - thumbHeight) * Layout.Scroll / Layout.MaxScroll;
        Round(c, track, Track, radius: 1);
        Round(c, SKRect.Create(track.Left, thumbTop, track.Width, thumbHeight), Muted, radius: 1);
    }

    private void DrawCards(SKCanvas c, PilotPortraitCatalog portraits)
    {
        bool segmented = TacticalBattleSkinRenderScope.Has("ui.bar.segments", "image");
        bool selectedArt = TacticalBattleSkinRenderScope.Has("ui.pilot-card.selected.art", "image");
        for (int i = 0; i < _cards.Count; i++)
        {
            var visible = Rect(i);
            if (visible.IsEmpty) continue;
            var card = _cards[i];
            var r = Layout.Cards[i];
            if (Recording is { } layout) layout.Rects["pilot:" + card.UnitId] = visible;
            PanelBox(c, r, card.Selected, 5, card.Selected && selectedArt ? "ui.pilot-card.selected" : "ui.pilot-card");
            Portrait(c, portraits, card.PilotId, card.Name, SKRect.Create(r.Left + 6, r.Top + 5, 24, 24));
            T(c, card.Name, r.Left + 36, r.Top + 16, 11.5f, Text, true);
            T(c, card.Weapon, r.Left + 36, r.Top + 27, 8.5f, Muted);
            if (card.Chip.Length > 0)
            {
                float w = Measure(card.Chip, 9, card.ChipAccent) + 9;
                var chip = new SKRect(r.Right - 6 - w, r.Top + 6, r.Right - 6, r.Top + 19);
                Round(c, chip, card.Evading ? PurpleDark : card.ChipAccent ? YellowDark : Box,
                    card.Evading ? Purple.WithAlpha(200) : card.ChipAccent ? Yellow.WithAlpha(170) : BoxEdge, .8f, role: "ui.pilot-card.chip");
                if (card.Decoy)
                {
                    var icon = new SKRect(chip.Left - 17, chip.Top, chip.Left - 4, chip.Bottom);
                    Round(c, icon, GreenDark, Green.WithAlpha(200), .8f);
                    Diamond(c, new(icon.MidX, icon.MidY), 4, SKColors.Transparent, Green, 1);
                    Circle(c, new(icon.MidX, icon.MidY), 1.2f, Green);
                    if (Recording is { } recorded) recorded.Rects["pilot-decoy:" + card.UnitId] = icon;
                }
                T(c, card.Chip, chip.MidX, chip.MidY + 3.2f, 9, card.Evading ? Purple : card.ChipAccent ? Yellow : Text, card.ChipAccent, SKTextAlign.Center);
            }
            Line(c, r.Left + 6, r.Right - 6, r.Top + 33);
            for (int j = 0; j < card.Stats.Count; j++)
            {
                var stat = card.Stats[j]; float y = r.Top + 44 + 11 * j;
                T(c, stat.Label, r.Left + 7, y, 9, Text);
                T(c, stat.Value, r.Right - 7, y, 9.5f, stat.ValueTone ?? Text, true, SKTextAlign.Right);
                float x0 = r.Left + 60, x1 = r.Right - 41;
                var color = TacticalBattleSkinRenderScope.Color("ui.pilot-card.bar." + stat.Kind, "fill", stat.Tone);
                if (segmented)
                    Bar(c, x0, y - 4.5f, x1 - x0, 3, stat.Fill, color);
                else
                {
                    Stroke(c, new(x0, y - 3), new(x1, y - 3), Track, 3);
                    if (stat.Fill > 0) Stroke(c, new(x0, y - 3), new(x0 + (x1 - x0) * (float)PyMath.Clamp(stat.Fill, 0, 1), y - 3), color, 3);
                }
            }
            float fs = 8.5f;
            float Width() => card.Segments.Sum(s => Measure(s.Text, fs, s.Bold));
            while (fs > 7 && Width() > 176) fs -= .25f;
            float x = r.Left + 7;
            foreach (var segment in card.Segments)
            { T(c, segment.Text, x, r.Top + PilotColumnLayout.BaseHeight - 6, fs, segment.Tone, segment.Bold); x += Measure(segment.Text, fs, segment.Bold); }
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
