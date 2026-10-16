using System.Collections.Immutable;
using DeepSpaceSaga.Contracts;
using SkiaSharp;

namespace DeepSpaceSaga.Client.UI.Screens.GameSession.Controls;

internal sealed record CombatReportCardLayout(BattleReportSnapshot Report, CombatReportPresentation Text,
    SKRect Header, ImmutableArray<string> TitleLines, SKRect Illustration, SKRect Portrait,
    ImmutableArray<string> ReporterLines, ImmutableArray<string> BodyLines, float BodyTop, float Bottom, bool Expanded);

internal sealed record CombatReportPanelLayout(SKRect Bounds, SKRect Viewport,
    ImmutableArray<CombatReportCardLayout> Cards, float MaxScroll)
{
    internal const float CaptionHeight = 32;
    internal static CombatReportPanelLayout Create(SKRect bounds, CombatReportStore store, CombatReportFormatter formatter,
        SKPaint title, SKPaint body)
    {
        var viewport = new SKRect(bounds.Left, Math.Min(bounds.Bottom, bounds.Top + CaptionHeight), bounds.Right, bounds.Bottom);
        if (bounds.Width <= 16 || bounds.Height <= 0) return new(bounds, viewport, [], 0);
        var cards = ImmutableArray.CreateBuilder<CombatReportCardLayout>();
        float width = Math.Max(1, bounds.Width - 16), y = viewport.Top + 4;
        foreach (var report in store.Reports)
        {
            var text = formatter.Format(report);
            var titles = Wrap(text.Title, width - 20, title);
            var header = new SKRect(bounds.Left + 4, y, bounds.Right - 4, y + 28 + titles.Length * 18);
            bool expanded = store.ExpandedSequence == report.Sequence;
            SKRect illustration = SKRect.Empty, portrait = SKRect.Empty;
            ImmutableArray<string> reporters = [], lines = [];
            float bodyTop = header.Bottom, bottom = header.Bottom;
            if (expanded)
            {
                illustration = new(bounds.Left + 8, header.Bottom + 4, bounds.Right - 8, header.Bottom + 4 + width * 9 / 16);
                portrait = new(bounds.Left + 8, illustration.Bottom + 8, bounds.Left + 56, illustration.Bottom + 56);
                reporters = Wrap(text.Reporter, Math.Max(1, width - 58), body);
                bodyTop = Math.Max(portrait.Bottom, portrait.Top + reporters.Length * 17) + 8;
                lines = Wrap(text.Message + (text.DamageDetails is null ? "" : "\n" + text.DamageDetails), width, body);
                bottom = bodyTop + lines.Length * 17 + 10;
            }
            cards.Add(new(report, text, header, titles, illustration, portrait, reporters, lines, bodyTop, bottom, expanded));
            y = bottom + 5;
        }
        return new(bounds, viewport, cards.ToImmutable(), Math.Max(0, y - viewport.Bottom));
    }

    internal static ImmutableArray<string> Wrap(string text, float width, SKPaint paint)
    {
        var lines = ImmutableArray.CreateBuilder<string>();
        foreach (string paragraph in text.Split('\n'))
        {
            string rest = paragraph;
            while (rest.Length > 0)
            {
                int count = Math.Clamp((int)paint.BreakText(rest, Math.Max(1, width)), 1, rest.Length);
                if (count < rest.Length)
                {
                    int space = rest.LastIndexOf(' ', count - 1, count);
                    if (space > 0) count = space;
                    if (count > 0 && char.IsHighSurrogate(rest[count - 1]) && count < rest.Length && char.IsLowSurrogate(rest[count])) count++;
                }
                lines.Add(rest[..count].Trim());
                rest = rest[count..].TrimStart();
            }
        }
        return lines.ToImmutable();
    }
}
