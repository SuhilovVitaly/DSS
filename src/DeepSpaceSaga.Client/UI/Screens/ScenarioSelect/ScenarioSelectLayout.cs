using SkiaSharp;

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
/// meaningful for <see cref="ScenarioSelectZone.Row"/> and is already the absolute index
/// into the full scenario list (the caller's <see cref="ScenarioSelectLayout.VisibleRow"/>
/// entries carry that, not a scroll-relative position — unlike the old fixed-row-height
/// design, row heights vary with wrapped description length, so the caller must resolve
/// positions itself instead of Layout deriving them from a bare visible-row count).
/// </summary>
public readonly record struct ScenarioSelectHit(ScenarioSelectZone Zone, int RowIndex = -1)
{
    public static readonly ScenarioSelectHit None = new(ScenarioSelectZone.None);
}

/// <summary>
/// Layout and hit-test geometry for the ScenarioSelect screen (New Game -&gt; pick a
/// scenario, before the game session starts). Pure geometry — no SKCanvas/SKPaint
/// dependency — following the same pattern as <see cref="Load.LoadLayout"/>.
///
/// Two side-by-side nine-sliced panels sit on the outer background: a left content panel
/// holding the (row-selectable, button-free) scenario list, and a right action panel
/// holding the shared PLAY/BACK buttons at its bottom.
///
/// Row height is not fixed: each row's description word-wraps to however many lines its
/// text needs at the list's width, and the row grows to fit them (<see cref="RowHeightFor"/>).
/// Wrapping itself needs font metrics, so it's the caller's job (ScenarioSelectScreen, which
/// owns the SKPaint) — Layout only turns already-known per-row heights into positions and
/// hit-test rects via <see cref="VisibleRow"/>.
/// </summary>
public static class ScenarioSelectLayout
{
    public const float PanelWidth = 900f;
    public const float PanelHeight = 620f;

    /// <summary>Left panel: the nine-sliced scenario list (x=40, y=125, 450 wide, 475 tall).</summary>
    public const float ContentPanelX = 40f;
    public const float ContentPanelY = 125f;
    public const float ContentPanelWidth = 450f;
    public const float ContentPanelHeight = 475f;

    /// <summary>Right panel: the nine-sliced action panel (x=490, y=125, 370 wide, 475 tall).</summary>
    public const float ActionPanelX = 490f;
    public const float ActionPanelY = 125f;
    public const float ActionPanelWidth = 370f;
    public const float ActionPanelHeight = ContentPanelHeight;

    /// <summary>Breathing room between a panel's border artwork and the content it holds.</summary>
    public const float ContentPadding = 20f;

    public const float ListTop = ContentPanelY + ContentPadding;
    /// <summary>Total vertical budget available for stacked scenario rows.</summary>
    public const float ListHeight = ContentPanelHeight - 2 * ContentPadding;

    public const float RowSpacing = 8f;
    /// <summary>
    /// Single-line-description row height (50) plus RowSpacing (8) reproduces the old
    /// fixed 58px row step exactly — six single-line rows still fill ListHeight (340) with
    /// no gap; only a description that wraps to more lines grows the row beyond this floor.
    /// </summary>
    public const float RowMinHeight = 50f;
    /// <summary>Baseline offset (from the row's top) of the bold scenario name line.</summary>
    public const float RowNameBaselineY = 22f;
    /// <summary>Baseline offset (from the row's top) of the first wrapped description line.</summary>
    public const float RowDescriptionFirstBaselineY = 42f;
    /// <summary>Baseline-to-baseline spacing between wrapped description lines.</summary>
    public const float RowDescriptionLineHeight = 16f;
    /// <summary>Space kept below the last description line before the row's border.</summary>
    public const float RowBottomPadding = 8f;

    /// <summary>Baseline of the selected scenario's name heading at the top of the action panel.</summary>
    public const float InfoNameBaselineY = 42f;

    /// <summary>Baseline of the first LABEL | VALUE info row.</summary>
    public const float InfoRowsTopY = 110f;

    /// <summary>Baseline-to-baseline spacing between info rows.</summary>
    public const float InfoRowSpacing = 44f;

    /// <summary>Left/right margin from the action panel's edges for an info row's label/value.</summary>
    public const float InfoSideMargin = 30f;

    /// <summary>How far (above ascent, below descent) the separator line extends, so it reads taller than the text next to it.</summary>
    public const float InfoSeparatorOverhang = 4f;

    public const float InfoSeparatorStrokeWidth = 2f;

    public const float ActionButtonWidth = 160f;
    public const float ActionButtonHeight = 44f;
    public const float ActionButtonGap = 20f;
    public const float ActionButtonBottomMargin = 24f;

    public const float ScrollbarWidth = 6f;
    public const float ScrollbarGap = 10f;
    public const float ScrollbarThumbMinHeight = 20f;

    public static float PanelLeft(int screenWidth) => (screenWidth - PanelWidth) / 2f;
    public static float PanelTop(int screenHeight) => (screenHeight - PanelHeight) / 2f;

    public static SKRect PanelRect(int screenWidth, int screenHeight)
    {
        float left = PanelLeft(screenWidth);
        float top = PanelTop(screenHeight);
        return new SKRect(left, top, left + PanelWidth, top + PanelHeight);
    }

    public static float ListLeft => ContentPanelX + ContentPadding;
    public static float ListWidth => ContentPanelWidth - 2 * ContentPadding;

    /// <summary>
    /// Row height for a description wrapped into <paramref name="descriptionLineCount"/>
    /// lines (treated as at least 1) — grows by RowDescriptionLineHeight per extra line.
    /// </summary>
    public static float RowHeightFor(int descriptionLineCount)
    {
        int lines = Math.Max(1, descriptionLineCount);
        float height = RowDescriptionFirstBaselineY + (lines - 1) * RowDescriptionLineHeight + RowBottomPadding;
        return Math.Max(RowMinHeight, height);
    }

    public static (float X, float Y, float W, float H) RowRect(float y, float height) =>
        (ListLeft, y, ListWidth, height);

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
    /// Vertical track spanning the content panel's full row-list budget, in its right
    /// margin strip. Only meaningful — and only drawn by the caller — when the scenario
    /// list overflows what's currently visible.
    /// </summary>
    public static (float X, float Y, float W, float H) ScrollbarTrackRect() =>
        (ListLeft + ListWidth + ScrollbarGap, ListTop, ScrollbarWidth, ListHeight);

    public static (float X, float Y, float W, float H) ScrollbarThumbRect(int scrollOffset, int totalScenarioCount, int visibleRowCount)
    {
        var track = ScrollbarTrackRect();
        float thumbHeight = Math.Max(ScrollbarThumbMinHeight, track.H * visibleRowCount / totalScenarioCount);

        int maxOffset = Math.Max(0, totalScenarioCount - visibleRowCount);
        float travel = track.H - thumbHeight;
        float thumbY = track.Y + (maxOffset > 0 ? travel * scrollOffset / maxOffset : 0f);

        return (track.X, thumbY, track.W, thumbHeight);
    }

    /// <summary>One currently-visible scenario row's already-computed position, for hit-testing.</summary>
    public readonly record struct VisibleRow(int AbsoluteIndex, float Y, float Height);

    /// <summary>
    /// Hit-tests a click at screen coordinates. <paramref name="visibleRows"/> must be the
    /// caller's current visible-row list (position/height already resolved from wrapping).
    /// </summary>
    public static ScenarioSelectHit HitTest(
        float screenX, float screenY, int screenWidth, int screenHeight, IReadOnlyList<VisibleRow> visibleRows)
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

        foreach (var row in visibleRows)
        {
            if (IsInRect(lx, ly, RowRect(row.Y, row.Height)))
                return new ScenarioSelectHit(ScenarioSelectZone.Row, row.AbsoluteIndex);
        }

        return ScenarioSelectHit.None;
    }

    private static bool IsInRect(float localX, float localY, (float X, float Y, float W, float H) rect) =>
        localX >= rect.X && localX <= rect.X + rect.W
        && localY >= rect.Y && localY <= rect.Y + rect.H;
}
