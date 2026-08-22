namespace DeepSpaceSaga.Client.UI.Screens.ScenarioSelect;

/// <summary>Which part of the ScenarioSelect screen a click landed on.</summary>
public enum ScenarioSelectZone
{
    None,
    Back,
    Play,
    /// <summary>A scenario row — selects it (see <see cref="ScenarioSelectHit.RowIndex"/>); does not play it.</summary>
    Row
}

/// <summary>
/// Result of a <see cref="ScenarioSelectLayout.HitTest"/>. <see cref="RowIndex"/> is only
/// meaningful for <see cref="ScenarioSelectZone.Row"/> and is relative to the currently
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
/// rather than dimming an underlying screen.
///
/// Two side-by-side nine-sliced panels sit on the outer background: a left content panel
/// holding the (row-selectable, button-free) scenario list, and a right action panel
/// holding the shared PLAY/BACK buttons at its bottom — one PLAY for whichever scenario
/// row is currently selected, replacing the old per-row PLAY button, plus BACK moved down
/// from its old top-right spot on the outer panel.
/// </summary>
public static class ScenarioSelectLayout
{
    public const float PanelWidth = 900f;
    public const float PanelHeight = 620f;

    public const float TitleY = 50f;

    /// <summary>Left panel: the nine-sliced scenario list (Docs request: x=50, y=150, 450 wide).</summary>
    public const float ContentPanelX = 50f;
    public const float ContentPanelY = 150f;
    public const float ContentPanelWidth = 450f;
    public const float ContentPanelHeight = 380f;

    /// <summary>Right panel: the nine-sliced action panel (Docs request: x=500, y=150, 350 wide).</summary>
    public const float ActionPanelX = 500f;
    public const float ActionPanelY = 150f;
    public const float ActionPanelWidth = 350f;
    public const float ActionPanelHeight = ContentPanelHeight;

    /// <summary>Breathing room between a panel's border artwork and the content it holds.</summary>
    public const float ContentPadding = 20f;

    public const float ListTop = ContentPanelY + ContentPadding;
    public const float RowHeight = 58f;
    public const float RowSpacing = 8f;
    public const int VisibleRows = 6;

    public const float ActionButtonWidth = 140f;
    public const float ActionButtonHeight = 44f;
    public const float ActionButtonGap = 20f;
    public const float ActionButtonBottomMargin = 24f;

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

    /// <summary>
    /// BACK button rect, local to the action panel (add ActionPanelX/Y for outer-panel-local
    /// coordinates). Sits left of PLAY at the panel's bottom, matching the Cancel/OK
    /// convention (secondary action left, primary action right).
    /// </summary>
    public static (float X, float Y, float W, float H) BackButtonRect()
    {
        float totalWidth = 2 * ActionButtonWidth + ActionButtonGap;
        float x = (ActionPanelWidth - totalWidth) / 2f;
        float y = ActionPanelHeight - ActionButtonBottomMargin - ActionButtonHeight;
        return (x, y, ActionButtonWidth, ActionButtonHeight);
    }

    /// <summary>PLAY button rect, local to the action panel — see <see cref="BackButtonRect"/>.</summary>
    public static (float X, float Y, float W, float H) PlayButtonRect()
    {
        var back = BackButtonRect();
        return (back.X + ActionButtonWidth + ActionButtonGap, back.Y, ActionButtonWidth, ActionButtonHeight);
    }

    /// <summary>
    /// Vertical track spanning the full visible row list, in the content panel's right
    /// margin strip. Only meaningful — and only drawn by the caller — when there are more
    /// scenarios than <see cref="VisibleRows"/>.
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

        float alx = lx - ActionPanelX;
        float aly = ly - ActionPanelY;
        if (IsInRect(alx, aly, BackButtonRect()))
            return new ScenarioSelectHit(ScenarioSelectZone.Back);
        if (IsInRect(alx, aly, PlayButtonRect()))
            return new ScenarioSelectHit(ScenarioSelectZone.Play);

        for (int i = 0; i < visibleScenarioCount; i++)
        {
            if (IsInRect(lx, ly, RowRect(i)))
                return new ScenarioSelectHit(ScenarioSelectZone.Row, i);
        }

        return ScenarioSelectHit.None;
    }

    private static bool IsInRect(float localX, float localY, (float X, float Y, float W, float H) rect) =>
        localX >= rect.X && localX <= rect.X + rect.W
        && localY >= rect.Y && localY <= rect.Y + rect.H;
}
