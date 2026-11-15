namespace DeepSpaceSaga.Client.UI.Screens.Ship;

public enum ShipButton
{
    None,
    Close,
    DroneRow,
    QuantityMinus,
    QuantityPlus,
    ConfirmRearm
}

/// <summary>
/// Layout and hit-test geometry for the Ship overlay panel. 1400×800 — the
/// standard panel size for gameplay-mechanic windows (DB/Documentation/02-FirstRelease/Screens/
/// ScreenCatalog.md), same as Finance.
/// </summary>
public sealed class ShipLayout
{
    public const float PanelWidth = 1400f;
    public const float PanelHeight = 800f;

    public const float TitleY = 50f;
    public const float BodyStartY = 100f;
    public const float BodyLineHeight = 28f;

    public const float CloseButtonSize = 28f;
    public const float CloseButtonMargin = 14f;

    /// <summary>One wing row: 28 px high, inset from both wing panel edges.</summary>
    public const float WingRowHeight = 28f;
    public const float WingRowInset = 12f;
    public const float WingPanelHeaderHeight = 34f;

    /// <summary>Rearm control block: minus/plus/confirm buttons plus preview and status lines.</summary>
    public const float RearmControlHeight = 28f;
    public const float RearmControlWidth = 32f;
    public const float RearmControlGap = 8f;
    public const float RearmControlsHeight = 88f;

    public static float PanelLeft(int screenWidth) => (screenWidth - PanelWidth) / 2f;
    public static float PanelTop(int screenHeight) => (screenHeight - PanelHeight) / 2f;

    private const float SectionGap = 20f;
    private const float SectionTop = 90f;
    private const float SectionBottom = PanelHeight - 20f;
    private const float HalfWidth = PanelWidth / 2f;

    /// <summary>Wing panel rect, local to the panel (add PanelLeft/PanelTop for screen space).</summary>
    public static (float Left, float Top, float Right, float Bottom) WingPanelLocalRect() =>
        (SectionGap, SectionTop, HalfWidth - SectionGap, SectionBottom);

    /// <summary>Crew panel rect, local to the panel (add PanelLeft/PanelTop for screen space).</summary>
    public static (float Left, float Top, float Right, float Bottom) CrewPanelLocalRect() =>
        (HalfWidth + SectionGap, SectionTop, PanelWidth - SectionGap, SectionBottom);

    /// <summary>True when a panel-local rect stays inside the panel bounds (used at 1280×720 and 1920×1080).</summary>
    public static bool FitsInsidePanel((float Left, float Top, float Right, float Bottom) rect) =>
        rect.Left >= 0 && rect.Top >= 0 && rect.Right <= PanelWidth && rect.Bottom <= PanelHeight;

    /// <summary>Close button rect, local to the panel (add PanelLeft/PanelTop for screen space).</summary>
    public static (float Left, float Top, float Right, float Bottom) CloseButtonLocalRect()
    {
        float right = PanelWidth - CloseButtonMargin;
        float left = right - CloseButtonSize;
        float top = CloseButtonMargin;
        float bottom = top + CloseButtonSize;
        return (left, top, right, bottom);
    }

    /// <summary>One drone row in the wing panel, local to the panel.</summary>
    public static (float Left, float Top, float Right, float Bottom) WingRowLocalRect(int index)
    {
        var wing = WingPanelLocalRect();
        float top = wing.Top + WingPanelHeaderHeight + index * WingRowHeight;
        return (wing.Left + WingRowInset, top, wing.Right - WingRowInset, top + WingRowHeight);
    }

    /// <summary>Rearm selection block under the final wing row, local to the panel.</summary>
    public static (float Left, float Top, float Right, float Bottom) RearmControlsLocalRect(int rowCount)
    {
        var wing = WingPanelLocalRect();
        float top = wing.Top + WingPanelHeaderHeight + Math.Max(0, rowCount) * WingRowHeight + 16;
        return (wing.Left + WingRowInset, top, wing.Right - WingRowInset, top + RearmControlsHeight);
    }

    public static (float Left, float Top, float Right, float Bottom) RearmMinusLocalRect(int rowCount)
    {
        var controls = RearmControlsLocalRect(rowCount);
        return (controls.Left, controls.Top, controls.Left + RearmControlWidth, controls.Top + RearmControlHeight);
    }

    public static (float Left, float Top, float Right, float Bottom) RearmPlusLocalRect(int rowCount)
    {
        var minus = RearmMinusLocalRect(rowCount);
        float left = minus.Right + RearmControlGap;
        return (left, minus.Top, left + RearmControlWidth, minus.Bottom);
    }

    public static (float Left, float Top, float Right, float Bottom) RearmConfirmLocalRect(int rowCount)
    {
        var controls = RearmControlsLocalRect(rowCount);
        float right = controls.Right;
        float left = right - 140;
        return (left, controls.Top, right, controls.Top + RearmControlHeight);
    }

    /// <summary>
    /// Hit test for the wing rows and rearm controls. Returns the row index only for
    /// <see cref="ShipButton.DroneRow"/>; every other hit carries -1. Close/background handling stays
    /// with <see cref="HitTest"/> and <see cref="IsInsidePanel"/>.
    /// </summary>
    public static (ShipButton Button, int RowIndex) RearmHitTest(float screenX, float screenY,
        int screenWidth, int screenHeight, int rowCount)
    {
        float lx = screenX - PanelLeft(screenWidth);
        float ly = screenY - PanelTop(screenHeight);
        for (int i = 0; i < rowCount; i++)
            if (Contains(WingRowLocalRect(i), lx, ly))
                return (ShipButton.DroneRow, i);
        if (Contains(RearmMinusLocalRect(rowCount), lx, ly)) return (ShipButton.QuantityMinus, -1);
        if (Contains(RearmPlusLocalRect(rowCount), lx, ly)) return (ShipButton.QuantityPlus, -1);
        if (Contains(RearmConfirmLocalRect(rowCount), lx, ly)) return (ShipButton.ConfirmRearm, -1);
        return (ShipButton.None, -1);
    }

    private static bool Contains((float Left, float Top, float Right, float Bottom) rect, float x, float y) =>
        x >= rect.Left && x <= rect.Right && y >= rect.Top && y <= rect.Bottom;

    /// <summary>True when (screenX, screenY) lands inside the panel rect (screen space).</summary>
    public static bool IsInsidePanel(float screenX, float screenY, int screenWidth, int screenHeight)
    {
        float panelLeft = PanelLeft(screenWidth);
        float panelTop = PanelTop(screenHeight);

        return screenX >= panelLeft && screenX <= panelLeft + PanelWidth
            && screenY >= panelTop && screenY <= panelTop + PanelHeight;
    }

    public static ShipButton HitTest(float screenX, float screenY, int screenWidth, int screenHeight)
    {
        float panelLeft = PanelLeft(screenWidth);
        float panelTop = PanelTop(screenHeight);

        float lx = screenX - panelLeft;
        float ly = screenY - panelTop;

        var (left, top, right, bottom) = CloseButtonLocalRect();
        if (lx >= left && lx <= right && ly >= top && ly <= bottom)
            return ShipButton.Close;

        return ShipButton.None;
    }
}
