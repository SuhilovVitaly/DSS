using System.Collections.Immutable;
using System.Globalization;
using DeepSpaceSaga.Client.UI.Controls;
using DeepSpaceSaga.Client.UI.Screens.TacticalBattle;
using SkiaSharp;
using static DeepSpaceSaga.Client.UI.Screens.TacticalBattle.BattleDesign;

namespace DeepSpaceSaga.Client.UI.Screens.Tutorial;

/// <summary>
/// The «Связь» panel of the battle tutorial at the bottom edge of the field, between the pilot column and the right panel
/// (design coordinates 1280x720). Expanded: officer portrait, «name · role», up to two rows of the current line, «Got it» and
/// «Hints: off». Otherwise a compact chip («COMMS · H — comms log», or «Hints: on» when hints are disabled). Every text is a
/// Locale key resolved at draw time, so the panel always speaks the current game language.
/// </summary>
internal sealed class TutorialCommsPanel
{
    internal const float Width = 460, Height = 100, TextWidth = 352, Left = 206, Bottom = 714;
    internal const float FontSize = 13, RowHeight = 18, ChipWidth = 190, ChipHeight = 30;
    private const float Pad = 8, PortraitSize = 72, TextLeft = Pad + PortraitSize + 10, RightReserve = 246, ButtonHeight = 20;

    internal SKRect Bounds { get; private set; } = SKRect.Empty;
    internal SKRect AcknowledgeBounds { get; private set; } = SKRect.Empty;
    internal SKRect HintsBounds { get; private set; } = SKRect.Empty;
    internal SKRect ChipBounds { get; private set; } = SKRect.Empty;
    internal bool Expanded { get; private set; }
    private bool _hintsEnabled = true;

    /// <summary>Wraps a line into rows of <see cref="TextWidth"/> with the panel font (glyph-safe).</summary>
    internal static ImmutableArray<string> Wrap(string text) => Wrap(text, TextWidth);

    internal static ImmutableArray<string> Wrap(string text, float width)
    {
        using var paint = BattleTypography.TextPaint(FontSize, false);
        return ControlText.Wrap(text, width, paint);
    }

    /// <summary>Rows actually drawn: at most two; a longer line ends the second row with an ellipsis.</summary>
    internal static ImmutableArray<string> Rows(string text, float width = TextWidth)
    {
        var lines = Wrap(text, width);
        if (lines.Length <= 2) return lines;
        using var paint = BattleTypography.TextPaint(FontSize, false);
        return [lines[0], ControlText.Ellipsize(string.Join(' ', lines.Skip(1)), width, paint)];
    }

    internal void Draw(SKCanvas c, BattleLayout layout, float designWidth, TutorialLine? line, bool hintsEnabled, SKBitmap? portrait,
        TutorialOfficer? officer)
    {
        _hintsEnabled = hintsEnabled;
        AcknowledgeBounds = HintsBounds = ChipBounds = SKRect.Empty;
        Expanded = line is not null && hintsEnabled;
        if (Expanded) DrawExpanded(c, layout, designWidth, line!, portrait, officer);
        else DrawChip(c, layout, hintsEnabled);
    }

    /// <summary>"ack" | "hints-off" | "hints-on" | null (inside the panel but not on a control, or outside).</summary>
    internal string? Hit(SKPoint point)
    {
        if (AcknowledgeBounds.Contains(point)) return "ack";
        if (HintsBounds.Contains(point)) return "hints-off";
        if (!_hintsEnabled && ChipBounds.Contains(point)) return "hints-on";
        return null;
    }

    private void DrawExpanded(SKCanvas c, BattleLayout layout, float designWidth, TutorialLine line, SKBitmap? portrait,
        TutorialOfficer? officer)
    {
        float available = designWidth - RightReserve - Left;
        float width = available < Width ? available : Width;
        var r = new SKRect(Left, Bottom - Height, Left + width, Bottom);
        Bounds = r;
        layout.Rects["tutorial:panel"] = r;
        PanelBox(c, r);

        string name = officer is null ? Localization.Get("Tutorial.Panel.Title")
            : Localization.Get(officer.NameKey) + " · " + Localization.Get(officer.RoleKey);
        var face = SKRect.Create(r.Left + Pad, r.Top + Pad + 4, PortraitSize, PortraitSize);
        layout.Rects["tutorial:portrait"] = face;
        DrawPortrait(c, face, portrait, officer is null ? name : Localization.Get(officer.NameKey));

        float x = r.Left + TextLeft, textWidth = r.Right - Pad - 10 - x;
        using (var header = BattleTypography.TextPaint(11, true))
            T(c, ControlText.Ellipsize(name, textWidth, header), x, r.Top + 19, 11, Cyan, true);
        var rows = Rows(Localization.Get(line.Step.LineKey), textWidth);
        for (int i = 0; i < rows.Length; i++) T(c, rows[i], x, r.Top + 39 + i * RowHeight, FontSize, Text);

        float buttonTop = r.Bottom - Pad - ButtonHeight;
        string ack = Localization.Get("Tutorial.Panel.Acknowledge"), off = Localization.Get("Tutorial.Panel.HintsOff");
        float ackWidth = ButtonWidth(ack), offWidth = ButtonWidth(off);
        AcknowledgeBounds = new SKRect(r.Right - Pad - ackWidth, buttonTop, r.Right - Pad, buttonTop + ButtonHeight);
        HintsBounds = new SKRect(AcknowledgeBounds.Left - 6 - offWidth, buttonTop, AcknowledgeBounds.Left - 6, buttonTop + ButtonHeight);
        layout.Rects["tutorial:ack"] = AcknowledgeBounds;
        layout.Rects["tutorial:hints"] = HintsBounds;
        Button(c, HintsBounds, off);
        Button(c, AcknowledgeBounds, ack, big: true);
    }

    private void DrawChip(SKCanvas c, BattleLayout layout, bool hintsEnabled)
    {
        var r = new SKRect(Left, Bottom - ChipHeight, Left + ChipWidth, Bottom);
        Bounds = ChipBounds = r;
        layout.Rects["tutorial:chip"] = r;
        string text = hintsEnabled
            ? Localization.Get("Tutorial.Panel.Title") + " · " + Localization.Get("Tutorial.Panel.JournalHint")
            : Localization.Get("Tutorial.Panel.HintsOn");
        if (hintsEnabled)
        {
            PanelBox(c, r);
            text = ControlText.SafeSymbols(text);
            float size = FitSize(text, 11, r.Width - 16);
            T(c, text, r.Left + 8, r.MidY + size * .36f, size, Muted);
        }
        else Button(c, r, text);
    }

    private static float ButtonWidth(string text)
    {
        float measured = Measure(ControlText.SafeSymbols(text), 12, true) + 20;
        return measured < 64 ? 64 : measured;
    }

    private static void DrawPortrait(SKCanvas c, SKRect face, SKBitmap? portrait, string name)
    {
        Round(c, face, PortraitBg, BoxEdge);
        if (portrait is not null)
        {
            c.Save();
            using var clip = new SKRoundRect(face, 3, 3);
            c.ClipRoundRect(clip, antialias: true);
            using var paint = new SKPaint { IsAntialias = true, FilterQuality = SKFilterQuality.High };
            // The composed bust is 300x300; the square above the shoulders keeps the face readable at 72 px.
            c.DrawBitmap(portrait, new SKRect(45, 10, 255, 220), face, paint);
            c.Restore();
            return;
        }
        string initial = name.Length == 0 ? "?" : StringInfo.GetNextTextElement(name, 0);
        T(c, initial, face.MidX, face.MidY + 10, 28, Muted, true, SKTextAlign.Center);
    }
}
