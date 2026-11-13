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
    public SKRect VisiblePanel => ClipPanel(Panel);
    private readonly SKRect[] _panels;
    public SKRect PanelAt(int index) => _panels[index];
    public SKRect VisiblePanelAt(int index) => ClipPanel(_panels[index]);
    private static SKRect ClipPanel(SKRect panel) => SKRect.Intersect(panel, new(Viewport.Left, Viewport.Top, 202, Viewport.Bottom));

    public PilotColumnLayout(IReadOnlyList<float> heights, float scroll, int revealIndex = -1,
        bool slim = false, int panelIndex = -1, float panelHeight = 0, IReadOnlyList<float>? panelHeights = null)
    {
        Slim = slim;
        var cards = new SKRect[heights.Count];
        _panels = new SKRect[cards.Length];
        float top = Viewport.Top;
        for (int i = 0; i < cards.Length; i++)
        {
            cards[i] = SKRect.Create(Viewport.Left, top, slim ? 150 : Viewport.Width, slim ? 40 : heights[i]);
            top = cards[i].Bottom + Gap;
            float height = panelHeights?[i] ?? (i == panelIndex ? panelHeight : 0);
            if (slim && height > 0)
            {
                _panels[i] = SKRect.Create(cards[i].Left + 45, cards[i].Bottom + 3, 150, height);
                top = _panels[i].Bottom + 6;
            }
        }

        ContentHeight = cards.Length == 0 ? 0 : Math.Max(cards[^1].Bottom, _panels[^1].Bottom) - Viewport.Top;
        if (revealIndex >= 0)
        {
            var selected = cards[revealIndex];
            if (!_panels[revealIndex].IsEmpty) selected.Bottom = _panels[revealIndex].Bottom;
            // An oversized panel cannot fit completely; show its pilot and top first.
            float lower = Math.Min(selected.Bottom - Viewport.Bottom, selected.Top - Viewport.Top);
            scroll = Math.Clamp(scroll, lower, selected.Top - Viewport.Top);
        }
        Scroll = Math.Clamp(scroll, 0, MaxScroll);
        for (int i = 0; i < cards.Length; i++)
        {
            cards[i].Offset(0, -Scroll);
            if (!_panels[i].IsEmpty) _panels[i].Offset(0, -Scroll);
        }
        Cards = cards;
        Panel = panelIndex < 0 ? SKRect.Empty : _panels[panelIndex];
        Bounds = cards.Length == 0 ? SKRect.Empty :
            new(Viewport.Left, Viewport.Top, slim ? Math.Max(cards[^1].Right, _panels.Max(panel => panel.Right)) : Viewport.Right,
                Math.Min(Viewport.Bottom, Math.Max(cards[^1].Bottom, _panels[^1].Bottom)));
    }

    public SKRect Visible(int index) => SKRect.Intersect(Cards[index], Viewport);

    public static float CompactBottom(int count, bool slim = false) => count == 0 ? 0 :
        Math.Min(Viewport.Bottom, Viewport.Top + count * ((slim ? 40 : BaseHeight) + Gap) - Gap);
}
