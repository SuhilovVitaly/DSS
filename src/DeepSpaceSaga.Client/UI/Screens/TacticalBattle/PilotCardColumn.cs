using DeepSpaceSaga.Motion.TacticalBattle;
using SkiaSharp;
using static DeepSpaceSaga.Client.UI.Screens.TacticalBattle.BattleDesign;

namespace DeepSpaceSaga.Client.UI.Screens.TacticalBattle;

internal readonly record struct PilotCardCommand(string UnitId, string Action, SKRect Bounds, SKRect Visible);

internal sealed class PilotCardColumn
{
    private IReadOnlyList<PilotCard> _cards = [];
    private readonly List<PilotCardCommand> _commands = [];
    private PilotCardCommand? _slider;
    private readonly PilotCardFireBlock _fireBlock = new();
    private TacticalBattleFrame? _frame;
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

    public void RegisterCommand(string unitId, string action, SKRect bounds)
    {
        if (unitId != _selectedId) return;
        var visible = SKRect.Intersect(bounds, PilotColumnLayout.Viewport);
        if (visible.IsEmpty) return;
        _commands.Add(new(unitId, action, bounds, visible));
        if (Recording is { } layout) layout.Rects["card-command:" + action] = visible;
    }

    public PilotCardCommand? HitCommand(SKPoint point)
    {
        foreach (var command in _commands)
            if (command.Visible.Contains(point))
                return command;
        return null;
    }

    public SKRect CommandBounds(string action) =>
        _commands.Find(command => command.Action == action).Visible;

    public bool BeginSlider(PilotCardCommand command, SKPoint point, TacticalBattleOrders orders)
    {
        if (command.Action is not ("slider:speed" or "slider:turn")) return false;
        _slider = command;
        return DragSlider(point, orders);
    }

    public bool DragSlider(SKPoint point, TacticalBattleOrders orders)
    {
        if (_slider is not { } command) return false;
        if (orders.Selected != command.UnitId)
        {
            EndSlider();
            return false;
        }
        ApplySlider(orders, command.Action == "slider:speed", point.X, command.Bounds.Left + 7, command.Bounds.Right - 7);
        return true;
    }

    public void EndSlider() => _slider = null;

    internal static void ApplySlider(TacticalBattleOrders orders, bool speed, float x, float start, float end)
    {
        if (orders.Capture().SelectedOrder is not { } row) return;
        double fraction = Math.Clamp((x - (double)start) / (end - start), 0, 1);
        if (speed) orders.SetSpeed(row.MinimumSpeed + fraction * (row.MaximumSpeed - row.MinimumSpeed));
        else orders.SetTurn((2 * fraction - 1) * row.MaximumTurn);
    }

    public bool ScrollTargets(string? selectedId, SKPoint point, float delta) =>
        selectedId is not null && selectedId == _selectedId && _fireBlock.Scroll(point, delta);

    public bool Scroll(SKPoint point, float delta)
    {
        if (!float.IsFinite(delta) || delta == 0 || Layout.MaxScroll == 0 || !PilotColumnLayout.Viewport.Contains(point))
            return false;
        _scroll = Math.Clamp(_scroll - 40 * Math.Sign(delta), 0, Layout.MaxScroll);
        return true;
    }

    public void Arrange(TacticalBattleFrame frame, IReadOnlyList<PilotCard> cards)
    {
        _frame = frame;
        _fireBlock.Observe(frame.FirePlan?.Selected);
        Arrange(cards, card => ExpandedHeight(frame, card));
    }

    private static float ExpandedHeight(TacticalBattleFrame frame, PilotCard card)
    {
        if (frame.Movement?.Selected == card.UnitId) return 10 + PilotCardMoveBlock.Height;
        if (frame.FirePlan?.Selected is { } fire && fire.Drone.Id == card.UnitId)
            return 10 + PilotCardFireBlock.Height(fire);
        return 0;
    }

    public void Arrange(IReadOnlyList<PilotCard> cards, Func<PilotCard, float>? expandedHeight = null)
    {
        _commands.Clear();
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
        for (int i = 0; i < _cards.Count; i++)
        {
            var visible = Rect(i);
            if (visible.IsEmpty) continue;
            var card = _cards[i];
            var r = Layout.Cards[i];
            if (Recording is { } layout) layout.Rects["pilot:" + card.UnitId] = visible;
            PanelBox(c, r, card.Selected, 5, card.Selected ? "ui.pilot-card.selected" : "ui.pilot-card");
            Portrait(c, portraits, card.PilotId, card.Name, SKRect.Create(r.Left + 6, r.Top + 6, 34, 34));
            DrawChip(c, card, r);
            T(c, BuildCardModel.Ellipsize(card.Name, 108, 11.5f), r.Left + 6, r.Top + 51, 11.5f, Text, true);
            T(c, BuildCardModel.Ellipsize(card.Weapon, 108, 8.5f, false), r.Left + 6, r.Top + 62, 8.5f, Muted);
            for (int j = 0; j < card.Stats.Count; j++)
                DrawGauge(c, card.Stats[j], r.Left + 129 + 22 * j, r.Top, segmented);
            DrawPhase(c, card, r);
            if (card.Selected && r.Height > PilotColumnLayout.BaseHeight)
                DrawExpanded(c, card, r);
        }
    }

    private void DrawExpanded(SKCanvas c, PilotCard card, SKRect r)
    {
        float x0 = r.Left + 6, x1 = r.Right - 6;
        Stroke(c, new(x0, r.Top + 83), new(x1, r.Top + 83), BoxEdge, 1, [2, 2]);
        float top = r.Top + 88;
        if (_frame?.Movement?.SelectedOrder is { } move)
            PilotCardMoveBlock.Draw(c, this, card.UnitId, move, x0, x1, top);
        else if (_frame?.FirePlan?.Selected is { } fire)
            _fireBlock.Draw(c, this, _frame.Battle, fire, x0, x1, top);
    }

    internal static void CommandButton(SKCanvas c, SKRect bounds, string label, bool active = false, float size = 9.5f)
    {
        Round(c, bounds, active ? YellowDark : ButtonFill, active ? Yellow : BoxEdge, .8f, 2.5f, "ui.button");
        T(c, BuildCardModel.Ellipsize(label, bounds.Width - 4, size, active), bounds.MidX,
            bounds.MidY + size * .36f, size, active ? Yellow : Text, active, SKTextAlign.Center, "ui.button");
    }

    internal static void FittedText(SKCanvas canvas, string text, float x, float y, float size,
        float width, SKColor tone, bool bold = false, SKTextAlign align = SKTextAlign.Left)
    {
        while (size > 6.5f && Measure(text, size, bold) > width) size -= .25f;
        string fitted = BuildCardModel.Ellipsize(text, width, size, bold);
        T(canvas, fitted, x, y, size, tone, bold, align);
        if (fitted != text) Recording?.Texts.Add(new(text, tone));
    }

    private static void DrawChip(SKCanvas c, PilotCard card, SKRect r)
    {
        if (card.Chip.Length == 0) return;
        float maxWidth = card.Decoy ? 51 : 68;
        float fontSize = 9;
        while (fontSize > 7 && Measure(card.Chip, fontSize, card.ChipAccent) > maxWidth - 9)
            fontSize -= .25f;
        string text = BuildCardModel.Ellipsize(card.Chip, maxWidth - 9, fontSize, card.ChipAccent);
        float width = Measure(text, fontSize, card.ChipAccent) + 9;
        var chip = SKRect.Create(r.Left + 46, r.Top + 6, width, 13);
        Round(c, chip, card.Evading ? PurpleDark : card.ChipAccent ? YellowDark : Box,
            card.Evading ? Purple.WithAlpha(200) : card.ChipAccent ? Yellow.WithAlpha(170) : BoxEdge,
            .8f, role: "ui.pilot-card.chip");
        var tone = card.Evading ? Purple : card.ChipAccent ? Yellow : Text;
        T(c, text, chip.MidX, chip.MidY + 3.2f, fontSize, tone, card.ChipAccent, SKTextAlign.Center);
        if (text != card.Chip) Recording?.Texts.Add(new(card.Chip, tone));
        if (!card.Decoy) return;

        var icon = SKRect.Create(chip.Right + 4, chip.Top, 13, 13);
        Round(c, icon, GreenDark, Green.WithAlpha(200), .8f);
        Diamond(c, new(icon.MidX, icon.MidY), 4, SKColors.Transparent, Green, 1);
        Circle(c, new(icon.MidX, icon.MidY), 1.2f, Green);
        var visible = SKRect.Intersect(icon, PilotColumnLayout.Viewport);
        if (!visible.IsEmpty && Recording is { } recorded)
            recorded.Rects["pilot-decoy:" + card.UnitId] = visible;
    }

    private static void DrawGauge(SKCanvas c, PilotStat stat, float x, float top, bool segmented)
    {
        int separator = stat.Value.IndexOf('/');
        string value = separator < 0 ? stat.Value : stat.Value[..separator];
        T(c, value, x, top + 12, 8.5f, stat.ValueTone ?? Text, true, SKTextAlign.Center);
        var track = SKRect.Create(x - 4, top + 16, 8, 40);
        Round(c, track, Track, radius: 2, role: "ui.pilot-card.gauge.track");
        string role = "ui.pilot-card.bar." + stat.Kind;
        var color = TacticalBattleSkinRenderScope.Color(role, "fill", stat.Tone);
        float height = 40 * (float)PyMath.Clamp(stat.Fill, 0, 1);
        if (height > 0)
        {
            var fill = SKRect.Create(track.Left, track.Bottom - height, track.Width, height);
            Round(c, fill, stat.Tone, radius: 2, role: role);
            if (segmented)
            {
                using var clip = new SKRoundRect(fill, 2, 2);
                c.Save();
                c.ClipRoundRect(clip, antialias: true);
                TacticalBattleSkinRenderScope.Image(c, "ui.bar.segments", track);
                c.Restore();
            }
        }
        PilotCardIcons.StatIcon(c, stat.Kind, new(x, top + 65), color);
    }

    private static void DrawPhase(SKCanvas c, PilotCard card, SKRect r)
    {
        float size = 8.5f;
        const float width = 178;
        float Width() => card.Segments.Sum(segment => Measure(segment.Text, size, segment.Bold));
        while (size > 6.5f && Width() > width) size -= .25f;
        float x = r.Left + 6;
        float right = x + width;
        foreach (var segment in card.Segments)
        {
            string text = BuildCardModel.Ellipsize(segment.Text, right - x, size, segment.Bold);
            if (text.Length > 0)
                T(c, text, x, r.Top + 76, size, segment.Tone, segment.Bold);
            if (text != segment.Text) break;
            x += Measure(text, size, segment.Bold);
        }
        Recording?.Texts.Add(new(card.PhaseText, Muted));
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
