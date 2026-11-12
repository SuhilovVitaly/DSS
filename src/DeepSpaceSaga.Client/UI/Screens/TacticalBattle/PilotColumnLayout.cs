using SkiaSharp;

namespace DeepSpaceSaga.Client.UI.Screens.TacticalBattle;

internal sealed class PilotColumnLayout
{
    public const float BaseHeight = 82;
    private const float Gap = 5;
    public static SKRect Viewport => new(6, 6, 196, 714);

    public IReadOnlyList<SKRect> Cards { get; }
    public float Scroll { get; }
    public float ContentHeight { get; }
    public float MaxScroll => Math.Max(0, ContentHeight - Viewport.Height);
    public SKRect Bounds { get; }

    public PilotColumnLayout(IReadOnlyList<float> heights, float scroll, int revealIndex = -1)
    {
        var cards = new SKRect[heights.Count];
        float top = Viewport.Top;
        for (int i = 0; i < cards.Length; i++)
        {
            cards[i] = SKRect.Create(Viewport.Left, top, Viewport.Width, heights[i]);
            top = cards[i].Bottom + Gap;
        }

        ContentHeight = cards.Length == 0 ? 0 : cards[^1].Bottom - Viewport.Top;
        if (revealIndex >= 0)
        {
            var selected = cards[revealIndex];
            scroll = Math.Clamp(scroll, selected.Bottom - Viewport.Bottom, selected.Top - Viewport.Top);
        }
        Scroll = Math.Clamp(scroll, 0, MaxScroll);
        for (int i = 0; i < cards.Length; i++)
            cards[i].Offset(0, -Scroll);
        Cards = cards;
        Bounds = cards.Length == 0 ? SKRect.Empty :
            new(Viewport.Left, Viewport.Top, Viewport.Right, Math.Min(Viewport.Bottom, cards[^1].Bottom));
    }

    public SKRect Visible(int index) => SKRect.Intersect(Cards[index], Viewport);

    public static float CompactBottom(int count) => count == 0 ? 0 :
        Math.Min(Viewport.Bottom, Viewport.Top + count * (BaseHeight + Gap) - Gap);
}
