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
    public bool Slim { get; }
    public SKRect Panel { get; }
    public SKRect VisiblePanel => SKRect.Intersect(Panel, new SKRect(Viewport.Left, Viewport.Top, 202, Viewport.Bottom));

    public PilotColumnLayout(IReadOnlyList<float> heights, float scroll, int revealIndex = -1,
        bool slim = false, int panelIndex = -1, float panelHeight = 0)
    {
        Slim = slim;
        var cards = new SKRect[heights.Count];
        SKRect panel = default;
        float top = Viewport.Top;
        for (int i = 0; i < cards.Length; i++)
        {
            cards[i] = SKRect.Create(Viewport.Left, top, slim ? 150 : Viewport.Width, slim ? 40 : heights[i]);
            top = cards[i].Bottom + Gap;
            if (slim && i == panelIndex && panelHeight > 0)
            {
                panel = SKRect.Create(cards[i].Left + 45, cards[i].Bottom + 3, 150, panelHeight);
                top = panel.Bottom + 6;
            }
        }

        ContentHeight = cards.Length == 0 ? 0 : Math.Max(cards[^1].Bottom, panel.Bottom) - Viewport.Top;
        if (revealIndex >= 0)
        {
            var selected = cards[revealIndex];
            if (revealIndex == panelIndex && !panel.IsEmpty) selected.Bottom = panel.Bottom;
            // An oversized panel cannot fit completely; show its pilot and top first.
            float lower = Math.Min(selected.Bottom - Viewport.Bottom, selected.Top - Viewport.Top);
            scroll = Math.Clamp(scroll, lower, selected.Top - Viewport.Top);
        }
        Scroll = Math.Clamp(scroll, 0, MaxScroll);
        for (int i = 0; i < cards.Length; i++)
            cards[i].Offset(0, -Scroll);
        Cards = cards;
        if (!panel.IsEmpty) panel.Offset(0, -Scroll);
        Panel = panel;
        Bounds = cards.Length == 0 ? SKRect.Empty :
            new(Viewport.Left, Viewport.Top, slim ? Math.Max(cards[^1].Right, panel.Right) : Viewport.Right,
                Math.Min(Viewport.Bottom, Math.Max(cards[^1].Bottom, panel.Bottom)));
    }

    public SKRect Visible(int index) => SKRect.Intersect(Cards[index], Viewport);

    public static float CompactBottom(int count, bool slim = false) => count == 0 ? 0 :
        Math.Min(Viewport.Bottom, Viewport.Top + count * ((slim ? 40 : BaseHeight) + Gap) - Gap);
}
