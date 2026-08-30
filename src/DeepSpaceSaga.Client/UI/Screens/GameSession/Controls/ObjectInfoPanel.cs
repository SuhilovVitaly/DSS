using DeepSpaceSaga.Client.UI.Controls;
using SkiaSharp;

namespace DeepSpaceSaga.Client.UI.Screens.GameSession.Controls;

/// <summary>
/// Object Info Panel (top-right) — mirrors the Commands Panel (top-left) chrome:
/// a Caption (360×32) with a Hide/Show toggle button (26×26), followed by two
/// fixed info rows — "Player Ship" and "Selected Object" — each a caption
/// (360×36) over a fixed-height body showing an object's image placeholder,
/// name, speed and direction.
/// The "Selected Object" row shows whichever object is currently hovered
/// (ActiveObjectId) or, absent a hover, last clicked (SelectedObjectId) — the
/// caller resolves that priority and passes the result in.
/// </summary>
public sealed class ObjectInfoPanel
{
    private const string XenonAssetsPath = "Images/UI/Themes/Xenon/GameSession/CommandPanels";
    private const string ObjectImageAssetsPath = "Images/UI/GameSessionScreenUI/object-info";

    public const float PanelWidth = CommandsPanel.PanelWidth;
    public const float CaptionHeight = CommandsPanel.CaptionHeight;
    public const float RowCaptionHeight = CommandsPanel.PanelCaptionHeight;

    /// <summary>Fixed body height for every info row: padding + 64px image + border.</summary>
    public const float RowBodyHeight = 76f;

    private const float Margin = 8f;
    private const float Padding = 6f;
    private const float ImageSize = 64f;
    private const float LineHeight = 16f;
    private const float FontSize = 12f;
    private const float CaptionTitleFontSize = 16f;
    private const float RowTitleFontSize = 14f;

    public const float ButtonSize = 26f;
    private const float ButtonLeftPadding = 2f;

    private static readonly string[] RowNames = { "Player Ship", "Selected Object" };

    private SKRect _hideShowButtonRect;
    private SKRect _captionRect;
    private SKRect _bodyRect;
    private readonly SKRect[] _rowCaptionRects = new SKRect[RowNames.Length];
    private readonly SKRect[] _rowBodyRects = new SKRect[RowNames.Length];

    private int _hoveredButtonIndex = -1; // 0 = toggle, -1 = none
    private int _pressedButtonIndex = -1;

    private ObjectInfoPanelState _state = ObjectInfoPanelState.Open;

    /// <summary>Per-row open/closed state (in-memory, per session) — click a row's own caption to toggle it, exactly like Commands Panel's per-panel groups.</summary>
    private readonly Dictionary<string, bool> _rowOpenedByName = new(StringComparer.Ordinal);

    // ── Paints ──────────────────────────────────────────────────
    private readonly SKPaint _mainCaptionBgPaint;
    private readonly SKPaint _rowCaptionBgPaint;
    private readonly SKPaint _captionHighlightPaint;
    private readonly SKPaint _captionShadowPaint;
    private readonly SKPaint _titlePaint;
    private readonly SKPaint _rowTitlePaint;
    private readonly SKPaint _panelBgPaint;
    private readonly SKPaint _panelBorderPaint;
    private readonly SKPaint _labelPaint;
    private readonly SKPaint _valuePaint;
    private readonly SKPaint _imagePlaceholderPaint;
    private readonly SKPaint _imagePaint;

    private readonly SKPaint _btnNormalPaint;
    private readonly SKPaint _btnHoverPaint;
    private readonly SKPaint _btnPressedPaint;
    private readonly SKPaint _btnBorderPaint;

    // ── Images ──────────────────────────────────────────────────
    private readonly SKBitmap? _hideImage;
    private readonly SKBitmap? _hideHoverImage;
    private readonly SKBitmap? _hidePressedImage;
    private readonly SKBitmap? _showImage;
    private readonly SKBitmap? _showHoverImage;
    private readonly SKBitmap? _showPressedImage;

    /// <summary>
    /// Lazily-resolved per-type object image, keyed by RenderObjectType (see
    /// <see cref="SpaceObjectType"/>), loaded from
    /// <c>Images/UI/GameSessionScreenUI/object-info/&lt;type-lowercase&gt;.png</c>.
    /// No files exist yet — every lookup falls back to <see cref="_imagePlaceholderPaint"/>
    /// until real icons are added; a missing file is cached as null so the disk is only
    /// probed once per type.
    /// </summary>
    private readonly Dictionary<string, SKBitmap?> _objectImages = new(StringComparer.Ordinal);

    public ObjectInfoPanel()
    {
        var typeface = XenonStyle.TypefaceRegular;

        _mainCaptionBgPaint = new SKPaint { Color = new SKColor(6, 30, 43), Style = SKPaintStyle.Fill, BlendMode = SKBlendMode.Src };
        _rowCaptionBgPaint = new SKPaint { Color = new SKColor(8, 35, 50), Style = SKPaintStyle.Fill, BlendMode = SKBlendMode.Src };
        _captionHighlightPaint = new SKPaint { Color = new SKColor(24, 76, 96), Style = SKPaintStyle.Stroke, StrokeWidth = 1f, IsAntialias = false };
        _captionShadowPaint = new SKPaint { Color = new SKColor(0, 6, 11), Style = SKPaintStyle.Stroke, StrokeWidth = 1f, IsAntialias = false };
        _titlePaint = new SKPaint { Color = XenonStyle.CyanBright, TextSize = CaptionTitleFontSize, IsAntialias = true, Typeface = XenonStyle.TypefaceSemibold };
        _rowTitlePaint = new SKPaint { Color = XenonStyle.CyanBright, TextSize = RowTitleFontSize, IsAntialias = true, Typeface = XenonStyle.TypefaceSemibold };

        _panelBgPaint = new SKPaint { Color = new SKColor(2, 16, 24), Style = SKPaintStyle.Fill, BlendMode = SKBlendMode.Src };
        _panelBorderPaint = new SKPaint { Color = new SKColor(42, 42, 42), Style = SKPaintStyle.Stroke, StrokeWidth = 1f };
        _labelPaint = new SKPaint { Color = new SKColor(140, 140, 140), TextSize = FontSize, IsAntialias = true, Typeface = typeface };
        _valuePaint = new SKPaint { Color = new SKColor(200, 200, 200), TextSize = FontSize, IsAntialias = true, Typeface = typeface };
        _imagePlaceholderPaint = new SKPaint { Color = new SKColor(30, 30, 30), Style = SKPaintStyle.Fill };
        _imagePaint = new SKPaint { IsAntialias = true, FilterQuality = SKFilterQuality.High };

        _btnNormalPaint = new SKPaint { Color = new SKColor(35, 35, 35, 220), Style = SKPaintStyle.Fill };
        _btnHoverPaint = new SKPaint { Color = new SKColor(55, 55, 55, 230), Style = SKPaintStyle.Fill };
        _btnPressedPaint = new SKPaint { Color = new SKColor(70, 70, 70, 240), Style = SKPaintStyle.Fill };
        _btnBorderPaint = new SKPaint { Color = new SKColor(80, 80, 80), Style = SKPaintStyle.Stroke, StrokeWidth = 1f };

        _hideImage = LoadImage($"{XenonAssetsPath}/collapse-normal.png");
        _hideHoverImage = LoadImage($"{XenonAssetsPath}/collapse-hover.png");
        _hidePressedImage = LoadImage($"{XenonAssetsPath}/collapse-pressed.png");
        _showImage = LoadImage($"{XenonAssetsPath}/expand-normal.png");
        _showHoverImage = LoadImage($"{XenonAssetsPath}/expand-hover.png");
        _showPressedImage = LoadImage($"{XenonAssetsPath}/expand-pressed.png");
    }

    private static SKBitmap? LoadImage(string path)
    {
        try { return File.Exists(path) ? SKBitmap.Decode(path) : null; }
        catch { return null; }
    }

    // ── Test seams ──────────────────────────────────────────────

    public ObjectInfoPanelState State => _state;
    public SKRect CaptionRect => _captionRect;
    public SKRect BodyRect => _bodyRect;
    public SKRect HideShowButtonRect => _hideShowButtonRect;
    public IReadOnlyList<SKRect> RowCaptionRects => _rowCaptionRects;
    public IReadOnlyList<SKRect> RowBodyRects => _rowBodyRects;
    public int HoveredButtonIndex => _hoveredButtonIndex;
    public int PressedButtonIndex => _pressedButtonIndex;

    /// <summary>True (default) unless the row at <paramref name="index"/> was clicked closed.</summary>
    public bool IsRowOpen(int index) => !_rowOpenedByName.TryGetValue(RowNames[index], out bool opened) || opened;

    /// <summary>Pure formatting for one row's content — used directly by tests and by <see cref="Render"/>.</summary>
    public static List<(string Label, string Value)> BuildLines(ObjectInfoPanelData? data)
    {
        var lines = new List<(string Label, string Value)>(3);

        if (data is { } d)
        {
            lines.Add(("Name", d.DisplayName ?? d.ObjectId));
            lines.Add(("Speed", $"{d.SpeedKmS:0.###} km/s"));
            lines.Add(("Direction", $"{d.Direction:F0}°"));
        }
        else
        {
            lines.Add(("Name", "—"));
            lines.Add(("Speed", "—"));
            lines.Add(("Direction", "—"));
        }

        return lines;
    }

    // ── Input ───────────────────────────────────────────────────

    public bool OnMouseDown(float x, float y)
    {
        if (_hideShowButtonRect.Contains(x, y))
        {
            _pressedButtonIndex = 0;
            _state = _state == ObjectInfoPanelState.Closed ? ObjectInfoPanelState.Open : ObjectInfoPanelState.Closed;
            return true;
        }

        // Row captions — toggle per-row Opened/Closed, same as Commands Panel's groups.
        for (int i = 0; i < RowNames.Length; i++)
        {
            if (_rowCaptionRects[i].Contains(x, y))
            {
                string name = RowNames[i];
                bool wasOpened = !_rowOpenedByName.TryGetValue(name, out bool o) || o;
                _rowOpenedByName[name] = !wasOpened;
                return true;
            }
        }

        return _captionRect.Contains(x, y) || _bodyRect.Contains(x, y);
    }

    public bool OnMouseMove(float x, float y)
    {
        _hoveredButtonIndex = _hideShowButtonRect.Contains(x, y) ? 0 : -1;
        return _hoveredButtonIndex >= 0;
    }

    public void OnMouseUp(float x, float y) => _pressedButtonIndex = -1;

    // ── Render ──────────────────────────────────────────────────

    /// <param name="viewportWidth">Logical (unscaled) viewport width — the panel is right-aligned against it.</param>
    /// <param name="top">
    /// Logical-space y of the panel's own top edge — the caller positions this below
    /// whatever else already occupies the top-right corner (the Speed/Scale panels),
    /// since a fixed <see cref="Margin"/> from the top would overlap them.
    /// </param>
    public void Render(SKCanvas canvas, float viewportWidth, float top, ObjectInfoPanelData? playerShip, ObjectInfoPanelData? selectedOrActive)
    {
        float left = viewportWidth - Margin - PanelWidth;

        _captionRect = new SKRect(left, top, left + PanelWidth, top + CaptionHeight);
        _hideShowButtonRect = new SKRect(
            left + ButtonLeftPadding, top + 2f,
            left + ButtonLeftPadding + ButtonSize, top + 2f + ButtonSize);

        DrawBeveledCaption(canvas, _captionRect, _mainCaptionBgPaint);
        DrawButton(canvas, _hideShowButtonRect, ResolveHideShowImage());

        float titleX = _hideShowButtonRect.Right + Padding + 2f;
        float titleY = _captionRect.MidY + _titlePaint.TextSize / 3f;
        canvas.DrawText("Object Info", titleX, titleY, _titlePaint);

        float rowY = _captionRect.Bottom;

        if (_state != ObjectInfoPanelState.Closed)
        {
            rowY += CommandsPanel.MainCaptionToPanelsGap;
            var rowData = new[] { playerShip, selectedOrActive };

            for (int i = 0; i < RowNames.Length; i++)
            {
                bool opened = IsRowOpen(i);

                var captionRect = new SKRect(left, rowY, left + PanelWidth, rowY + RowCaptionHeight);
                var bodyRect = opened
                    ? new SKRect(left, captionRect.Bottom, left + PanelWidth, captionRect.Bottom + RowBodyHeight)
                    : SKRect.Empty;

                _rowCaptionRects[i] = captionRect;
                _rowBodyRects[i] = bodyRect;

                DrawBeveledCaption(canvas, captionRect, _rowCaptionBgPaint);
                canvas.DrawText(RowNames[i], captionRect.Left + Padding, captionRect.MidY + _rowTitlePaint.TextSize / 3f, _rowTitlePaint);

                if (opened)
                    DrawRowBody(canvas, bodyRect, rowData[i]);

                rowY += opened ? (RowCaptionHeight + RowBodyHeight) : RowCaptionHeight;
                if (!opened && i < RowNames.Length - 1)
                    rowY += CommandsPanel.CollapsedPanelGap;
            }
        }
        else
        {
            Array.Clear(_rowCaptionRects);
            Array.Clear(_rowBodyRects);
        }

        _bodyRect = new SKRect(left, _captionRect.Bottom, left + PanelWidth, rowY);
    }

    private void DrawRowBody(SKCanvas canvas, SKRect bodyRect, ObjectInfoPanelData? data)
    {
        canvas.DrawRect(bodyRect, _panelBgPaint);
        canvas.DrawRect(bodyRect, _panelBorderPaint);

        float imgX = bodyRect.Left + Padding;
        float imgY = bodyRect.Top + Padding;
        var imageRect = new SKRect(imgX, imgY, imgX + ImageSize, imgY + ImageSize);

        var image = data is { RenderObjectType: { } type } ? ResolveObjectImage(type) : null;
        if (image is not null)
            canvas.DrawBitmap(image, imageRect, _imagePaint);
        else
            canvas.DrawRect(imageRect, _imagePlaceholderPaint);
        canvas.DrawRect(imageRect, _panelBorderPaint);

        float textX = imageRect.Right + Padding;
        float textY = imgY + LineHeight - 3f;
        foreach (var (label, value) in BuildLines(data))
        {
            canvas.DrawText(label, textX, textY, _labelPaint);
            canvas.DrawText(value, textX + 62f, textY, _valuePaint);
            textY += LineHeight;
        }
    }

    private SKBitmap? ResolveObjectImage(string renderObjectType)
    {
        if (_objectImages.TryGetValue(renderObjectType, out var cached))
            return cached;

        var bitmap = LoadImage($"{ObjectImageAssetsPath}/{renderObjectType.ToLowerInvariant()}.png");
        _objectImages[renderObjectType] = bitmap;
        return bitmap;
    }

    private void DrawButton(SKCanvas canvas, SKRect rect, SKBitmap? image)
    {
        if (image is not null)
        {
            canvas.DrawBitmap(image, rect);
            return;
        }

        SKPaint fill = _pressedButtonIndex == 0 && _hoveredButtonIndex == 0
            ? _btnPressedPaint
            : _hoveredButtonIndex == 0 ? _btnHoverPaint : _btnNormalPaint;

        canvas.DrawRect(rect, fill);
        canvas.DrawRect(rect, _btnBorderPaint);
    }

    private SKBitmap? ResolveHideShowImage()
    {
        bool pressed = _pressedButtonIndex == 0 && _hoveredButtonIndex == 0;
        bool hovered = _hoveredButtonIndex == 0;

        if (_state == ObjectInfoPanelState.Closed)
            return pressed ? _showPressedImage : hovered ? _showHoverImage : _showImage;

        return pressed ? _hidePressedImage : hovered ? _hideHoverImage : _hideImage;
    }

    private void DrawBeveledCaption(SKCanvas canvas, SKRect rect, SKPaint backgroundPaint)
    {
        canvas.DrawRect(rect, backgroundPaint);

        float left = rect.Left + 0.5f;
        float top = rect.Top + 0.5f;
        float right = rect.Right - 0.5f;
        float bottom = rect.Bottom - 0.5f;
        canvas.DrawLine(left, top, right, top, _captionHighlightPaint);
        canvas.DrawLine(left, top, left, bottom, _captionHighlightPaint);
        canvas.DrawLine(right, top, right, bottom, _captionShadowPaint);
        canvas.DrawLine(left, bottom, right, bottom, _captionShadowPaint);
    }
}

public enum ObjectInfoPanelState
{
    Open,
    Closed,
}

/// <summary>Snapshot of one object's info-panel content — image lookup key, name, speed, direction.</summary>
public readonly record struct ObjectInfoPanelData(
    string ObjectId,
    string? DisplayName,
    double SpeedKmS,
    double Direction,
    string? RenderObjectType);
