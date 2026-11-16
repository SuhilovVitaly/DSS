using System.Collections.Immutable;
using DeepSpaceSaga.Client.UI.Controls;
using DeepSpaceSaga.Client.UI.Screens.TacticalBattle;
using SkiaSharp;
using static DeepSpaceSaga.Client.UI.Screens.TacticalBattle.BattleDesign;

namespace DeepSpaceSaga.Client.UI.Screens.Tutorial;

/// <summary>
/// Comms log («Журнал связи», H): every officer line heard in this battle, in spoken order, re-read on demand. Texts are
/// resolved from Locale at draw time (current language). A 460x300 panel just above the expanded comms panel; a narrow
/// window shrinks it exactly like the comms panel (<see cref="TutorialCommsPanel.WidthFor"/>), so it never reaches the right
/// column, and its rows wrap to the width actually drawn. It captures clicks and the wheel only inside its own bounds and is
/// available even with hints disabled.
/// </summary>
internal sealed class TutorialJournal
{
    internal const float Width = TutorialCommsPanel.Width, Height = 300;
    internal const float BottomEdge = TutorialCommsPanel.Bottom - TutorialCommsPanel.Height - 6;
    /// <summary>Row width of the full-size log; a narrow window wraps to <see cref="TextWidthFor"/> of its drawn width.</summary>
    internal const float TextWidth = Width - 2 * Pad - 14;
    private const float Pad = 12, HeaderHeight = 34, EntryGap = 8, ScrollStep = 3 * TutorialCommsPanel.RowHeight;

    private bool _stickToEnd;
    private SKRect _close = SKRect.Empty;
    private float _width = Width;

    internal bool IsOpen { get; private set; }
    /// <summary>The open log as last drawn (full width until the first draw).</summary>
    internal SKRect Bounds => IsOpen ? new SKRect(TutorialCommsPanel.Left, BottomEdge - Height, TutorialCommsPanel.Left + _width, BottomEdge) : SKRect.Empty;
    internal float Scroll { get; private set; }
    internal float MaxScroll { get; private set; }
    /// <summary>Wrapped rows of every heard line as last drawn; cached until the heard lines, their texts (language) or the row width change.</summary>
    internal IReadOnlyList<ImmutableArray<string>> Entries => _entries;

    private ImmutableArray<string>[] _entries = [];
    private string[] _texts = [];
    private float _content;
    private float _textWidth = TextWidth;

    /// <summary>Row width of a log drawn <paramref name="width"/> wide (<see cref="TextWidth"/> at full size).</summary>
    internal static float TextWidthFor(float width) => width - 2 * Pad - 14;

    internal void Toggle()
    {
        if (IsOpen) Close();
        else { IsOpen = true; _stickToEnd = true; }
    }

    internal void Close()
    {
        IsOpen = false;
        _close = SKRect.Empty;
    }

    internal void Draw(SKCanvas c, BattleLayout layout, float designWidth, IReadOnlyList<TutorialStep> heard)
    {
        if (!IsOpen) return;
        _width = TutorialCommsPanel.WidthFor(designWidth);
        var r = Bounds;
        layout.Rects["tutorial:journal"] = r;
        PanelBox(c, r);
        T(c, Localization.Get("Tutorial.Journal.Title"), r.Left + Pad, r.Top + 22, 12, Cyan, true);
        string close = Localization.Get("Tutorial.Journal.Close");
        float closeWidth = Measure(ControlText.SafeSymbols(close), 12) + 20;
        _close = new SKRect(r.Right - Pad - closeWidth, r.Top + 8, r.Right - Pad, r.Top + 28);
        layout.Rects["tutorial:journal-close"] = _close;
        Button(c, _close, close);

        var body = new SKRect(r.Left + Pad, r.Top + HeaderHeight, r.Right - Pad, r.Bottom - Pad);
        if (heard.Count == 0)
        {
            T(c, Localization.Get("Tutorial.Journal.Empty"), body.Left, body.Top + 16, TutorialCommsPanel.FontSize, Muted);
            Scroll = MaxScroll = 0;
            return;
        }

        var entries = Rows(heard, TextWidthFor(r.Width));
        float content = _content;
        MaxScroll = content > body.Height ? content - body.Height : 0;
        if (_stickToEnd) Scroll = MaxScroll;
        Scroll = Scroll < 0 ? 0 : Scroll > MaxScroll ? MaxScroll : Scroll;

        c.Save();
        c.ClipRect(body);
        float y = body.Top - Scroll;
        foreach (var rows in entries)
        {
            float top = y;
            foreach (string row in rows)
            {
                if (y + TutorialCommsPanel.RowHeight > body.Top && y < body.Bottom)
                    T(c, row, body.Left + 10, y + 13, TutorialCommsPanel.FontSize, Text);
                y += TutorialCommsPanel.RowHeight;
            }
            if (y > body.Top && top < body.Bottom)
                Stroke(c, new(body.Left + 2, top + 3), new(body.Left + 2, y - 3), PanelEdge.WithAlpha(150), 2);
            y += EntryGap;
        }
        c.Restore();
        if (MaxScroll > 0)
        {
            float track = body.Height, thumb = track * body.Height / (body.Height + MaxScroll);
            float thumbTop = body.Top + (track - thumb) * Scroll / MaxScroll;
            Round(c, SKRect.Create(body.Right + 6, thumbTop, 3, thumb), Muted, radius: 1);
        }
    }

    /// <summary>
    /// Wrapped rows of the heard lines. A frame only resolves the texts (dictionary lookups); wrapping and its paint happen
    /// again only when a line was added, a text changed (game language switched) or the row width changed (window resized).
    /// </summary>
    private ImmutableArray<string>[] Rows(IReadOnlyList<TutorialStep> heard, float textWidth)
    {
        bool same = _texts.Length == heard.Count && _textWidth == textWidth;
        for (int i = 0; same && i < heard.Count; i++) same = string.Equals(_texts[i], Localization.Get(heard[i].LineKey), StringComparison.Ordinal);
        if (same) return _entries;

        var texts = new string[heard.Count];
        var entries = new ImmutableArray<string>[heard.Count];
        float content = 0;
        using var paint = BattleTypography.TextPaint(TutorialCommsPanel.FontSize, false);
        for (int i = 0; i < heard.Count; i++)
        {
            texts[i] = Localization.Get(heard[i].LineKey);
            entries[i] = ControlText.Wrap(texts[i], textWidth, paint);
            content += entries[i].Length * TutorialCommsPanel.RowHeight;
        }
        _texts = texts;
        _textWidth = textWidth;
        _entries = entries;
        _content = content + (entries.Length - 1) * EntryGap;
        return entries;
    }

    /// <summary>True when the press is inside the open log (the close button closes it).</summary>
    internal bool OnMouseDown(SKPoint p)
    {
        if (!Bounds.Contains(p)) return false;
        if (_close.Contains(p)) Close();
        return true;
    }

    /// <summary>Wheel up (positive) shows earlier lines; consumed only inside the open log.</summary>
    internal bool OnMouseWheel(SKPoint p, float delta)
    {
        if (!Bounds.Contains(p) || !float.IsFinite(delta)) return false;
        if (delta == 0) return true;
        _stickToEnd = false;
        float next = Scroll - Math.Sign(delta) * ScrollStep;
        Scroll = next < 0 ? 0 : next > MaxScroll ? MaxScroll : next;
        return true;
    }
}
