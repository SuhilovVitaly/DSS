using SkiaSharp;

namespace DeepSpaceSaga.Client.UI.Screens.DockingConfirm;

public enum DockingConfirmButton
{
    None,
    Confirm
}

/// <summary>
/// Layout and hit-test geometry for the docking-confirmation modal. Reuses the standard
/// 1400x800 gameplay-mechanic panel size (same as ShipLayout/StationLayout).
/// </summary>
public static class DockingConfirmLayout
{
    public const float PanelWidth = 1400f;
    public const float PanelHeight = 800f;

    public const float TitleY = 60f;

    /// <summary>
    /// Portrait size for <see cref="Portraits.PortraitComposer.ComposeBodyAndHeadPortrait"/>'s
    /// 300×300 docking-point crop (background-less pipeline). Temporary — replaces the
    /// 250×250 cropped-portrait size used by the production Compose() path until that path
    /// adopts the docking point too.
    /// </summary>
    public const float PortraitPreviewWidth = 300f;
    public const float PortraitPreviewHeight = 300f;
    public const float PortraitTop = 160f;
    public const float PortraitSideMargin = 180f;
    public const float PortraitNameGap = 20f;

    public const float ConfirmButtonWidth = 320f;
    public const float ConfirmButtonHeight = 56f;
    public const float ConfirmButtonBottomMargin = 90f;

    public static float PanelLeft(int screenWidth) => (screenWidth - PanelWidth) / 2f;
    public static float PanelTop(int screenHeight) => (screenHeight - PanelHeight) / 2f;

    /// <summary>Dock operator portrait rect, local to the panel (left side).</summary>
    public static SKRect DockOperatorPortraitLocalRect() =>
        new(PortraitSideMargin, PortraitTop,
            PortraitSideMargin + PortraitPreviewWidth, PortraitTop + PortraitPreviewHeight);

    /// <summary>Captain portrait rect, local to the panel (right side).</summary>
    public static SKRect CaptainPortraitLocalRect() =>
        new(PanelWidth - PortraitSideMargin - PortraitPreviewWidth, PortraitTop,
            PanelWidth - PortraitSideMargin, PortraitTop + PortraitPreviewHeight);

    /// <summary>"Стыковка" confirm button rect, local to the panel (bottom, centered).</summary>
    public static SKRect ConfirmButtonLocalRect()
    {
        float left = (PanelWidth - ConfirmButtonWidth) / 2f;
        float top = PanelHeight - ConfirmButtonBottomMargin - ConfirmButtonHeight;
        return new SKRect(left, top, left + ConfirmButtonWidth, top + ConfirmButtonHeight);
    }

    /// <summary>True when (screenX, screenY) lands inside the panel rect (screen space).</summary>
    public static bool IsInsidePanel(float screenX, float screenY, int screenWidth, int screenHeight)
    {
        float panelLeft = PanelLeft(screenWidth);
        float panelTop = PanelTop(screenHeight);

        return screenX >= panelLeft && screenX <= panelLeft + PanelWidth
            && screenY >= panelTop && screenY <= panelTop + PanelHeight;
    }

    public static DockingConfirmButton HitTest(float screenX, float screenY, int screenWidth, int screenHeight)
    {
        float panelLeft = PanelLeft(screenWidth);
        float panelTop = PanelTop(screenHeight);

        float lx = screenX - panelLeft;
        float ly = screenY - panelTop;

        var confirm = ConfirmButtonLocalRect();
        if (lx >= confirm.Left && lx <= confirm.Right && ly >= confirm.Top && ly <= confirm.Bottom)
            return DockingConfirmButton.Confirm;

        return DockingConfirmButton.None;
    }
}
