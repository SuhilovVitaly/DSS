namespace DeepSpaceSaga.Client.UI.Screens.ScenarioSelect;

/// <summary>Which part of the ScenarioSelect screen a click landed on.</summary>
public enum ScenarioSelectZone
{
    None,
    Back,
    Play
}

/// <summary>
/// Result of a <see cref="ScenarioSelectLayout.HitTest"/>. <see cref="RowIndex"/> is only
/// meaningful for <see cref="ScenarioSelectZone.Play"/> and is relative to the currently
/// visible window (the caller adds the scroll offset to get the absolute scenario index).
/// </summary>
public readonly record struct ScenarioSelectHit(ScenarioSelectZone Zone, int RowIndex = -1)
{
    public static readonly ScenarioSelectHit None = new(ScenarioSelectZone.None);
}

/// <summary>
/// Layout and hit-test geometry for the ScenarioSelect screen (New Game -&gt; pick a
/// scenario, before the game session starts). Pure geometry — no SKCanvas dependency —
/// following the same pattern as <see cref="Load.LoadLayout"/>. A full top-level screen
/// (like MainMenu), not a paused-game overlay, so it owns its own full-screen background
/// rather than dimming an underlying screen. Each row is two lines tall (scenario Name,
/// then a dimmer Description line) to fit the new description field, wider than Load's
/// single-line slot rows.
/// </summary>
public static class ScenarioSelectLayout
{
    public const float PanelWidth = 900f;
    public const float PanelHeight = 620f;

    public const float Margin = 40f;

    public const float TitleY = 50f;

    /// <summary>
    /// The nine-sliced <c>micro-panel.png</c> content panel that holds the scenario list
    /// (Docs request: x=50, y=150, 800×380), positioned relative to the outer PanelLeft/Top.
    /// </summary>
    public const float ContentPanelX = 50f;
    public const float ContentPanelY = 150f;
    public const float ContentPanelWidth = 800f;
    public const float ContentPanelHeight = 380f;

    /// <summary>Breathing room between the content panel's border artwork and the row list/scrollbar it holds.</summary>
    public const float ContentPadding = 20f;

    public const float ListTop = ContentPanelY + ContentPadding;
    public const float RowHeight = 58f;
    public const float RowSpacing = 8f;
    public const int VisibleRows = 6;

    public const float PlayButtonWidth = 120f;
    public const float PlayButtonHeight = 40f;

    public const float BackButtonWidth = 100f;
    public const float BackButtonHeight = 32f;
    public const float BackButtonMargin = 16f;

    public const float ScrollbarWidth = 6f;
    public const float ScrollbarGap = 10f;
    public const float ScrollbarThumbMinHeight = 20f;

    public static float PanelLeft(int screenWidth) => (screenWidth - PanelWidth) / 2f;
    public static float PanelTop(int screenHeight) => (screenHeight - PanelHeight) / 2f;

    public static float ListLeft => ContentPanelX + ContentPadding;
    public static float ListWidth => ContentPanelWidth - 2 * ContentPadding;

    /// <summary>Top-left-relative rect for a visible row (before scroll offset is applied by the caller).</summary>
    public static (float X, float Y, float W, float H) RowRect(int visibleRowIndex)
    {
        float y = ListTop + visibleRowIndex * RowHeight;
        return (ListLeft, y, ListWidth, RowHeight - RowSpacing);
    }

    public static (float X, float Y, float W, float H) PlayButtonRect(int visibleRowIndex)
    {
        var row = RowRect(visibleRowIndex);
        float x = row.X + row.W - PlayButtonWidth;
        float y = row.Y + (row.H - PlayButtonHeight) / 2f;
        return (x, y, PlayButtonWidth, PlayButtonHeight);
    }

    /// <summary>Top-right corner of the panel, matching Load's CLOSE button chrome.</summary>
    public static (float X, float Y, float W, float H) BackButtonRect() =>
        (PanelWidth - BackButtonMargin - BackButtonWidth, BackButtonMargin, BackButtonWidth, BackButtonHeight);

    /// <summary>
    /// Vertical track spanning the full visible row list, in the right margin strip just
    /// past where the row's PLAY button ends. Only meaningful — and only drawn by the
    /// caller — when there are more scenarios than <see cref="VisibleRows"/>.
    /// </summary>
    public static (float X, float Y, float W, float H) ScrollbarTrackRect() =>
        (ListLeft + ListWidth + ScrollbarGap, ListTop, ScrollbarWidth, VisibleRows * RowHeight - RowSpacing);

    public static (float X, float Y, float W, float H) ScrollbarThumbRect(int scrollOffset, int totalScenarioCount)
    {
        var track = ScrollbarTrackRect();
        float thumbHeight = Math.Max(ScrollbarThumbMinHeight, track.H * VisibleRows / totalScenarioCount);

        int maxOffset = totalScenarioCount - VisibleRows;
        float travel = track.H - thumbHeight;
        float thumbY = track.Y + (maxOffset > 0 ? travel * scrollOffset / maxOffset : 0f);

        return (track.X, thumbY, track.W, thumbHeight);
    }

    /// <summary>
    /// Hit-tests a click at screen coordinates. <paramref name="visibleScenarioCount"/> is
    /// the number of rows actually rendered (min(total scenarios, VisibleRows)).
    /// </summary>
    public static ScenarioSelectHit HitTest(
        float screenX, float screenY, int screenWidth, int screenHeight, int visibleScenarioCount)
    {
        float panelLeft = PanelLeft(screenWidth);
        float panelTop = PanelTop(screenHeight);

        float lx = screenX - panelLeft;
        float ly = screenY - panelTop;

        if (IsInRect(lx, ly, BackButtonRect()))
            return new ScenarioSelectHit(ScenarioSelectZone.Back);

        for (int i = 0; i < visibleScenarioCount; i++)
        {
            if (IsInRect(lx, ly, PlayButtonRect(i)))
                return new ScenarioSelectHit(ScenarioSelectZone.Play, i);
        }

        return ScenarioSelectHit.None;
    }

    private static bool IsInRect(float localX, float localY, (float X, float Y, float W, float H) rect) =>
        localX >= rect.X && localX <= rect.X + rect.W
        && localY >= rect.Y && localY <= rect.Y + rect.H;
}
