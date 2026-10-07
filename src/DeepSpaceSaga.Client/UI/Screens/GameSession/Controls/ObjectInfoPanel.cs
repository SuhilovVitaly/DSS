using DeepSpaceSaga.Client.UI.Controls;
using DeepSpaceSaga.Client.UI.Screens.Trade;
using DeepSpaceSaga.Contracts;
using SkiaSharp;

namespace DeepSpaceSaga.Client.UI.Screens.GameSession.Controls;

/// <summary>
/// Object Info Panel (top-right) — mirrors the Commands Panel (top-left) chrome:
/// a Caption (360×32) with a Hide/Show toggle button (26×26), followed by two
/// fixed info rows — "Player Ship" and "Selected Object" — each a caption
/// (360×36) over a body showing an object's image, name, speed and direction
/// or authoritative resource survey knowledge. Long bodies scroll within the viewport.
/// The "Selected Object" row shows whichever object is currently hovered
/// (ActiveObjectId) or, absent a hover, last clicked (SelectedObjectId) — the
/// caller resolves that priority and passes the result in.
/// </summary>
public sealed class ObjectInfoPanel
{
    private const string XenonAssetsPath = "Images/UI/Themes/Xenon/GameSession/CommandPanels";
    private const string ObjectImageAssetsPath = "Images/UI/GameSessionScreenUI/object-info";

    /// <summary>Sprite frame for a row's image — landscape 4:3, not the old 64×64 square.</summary>
    private const float ImageWidth = 200f;
    private const float ImageHeight = 150f;

    /// <summary>
    /// Wider than <see cref="CommandsPanel.PanelWidth"/> by exactly how much
    /// <see cref="ImageWidth"/> grew over the previous 64px square image, so the text
    /// column and margins keep their prior absolute size while the panel grows just
    /// enough to fit the larger image.
    /// </summary>
    public const float PanelWidth = CommandsPanel.PanelWidth + (ImageWidth - 64f);
    public const float CaptionHeight = CommandsPanel.CaptionHeight;
    public const float RowCaptionHeight = CommandsPanel.PanelCaptionHeight;

    /// <summary>Minimum body height for an info row: padding + image + border.</summary>
    public const float RowBodyHeight = ImageHeight + 2 * Padding;

    private const float Margin = 8f;
    private const float Padding = 6f;
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
    private readonly float[] _rowScrollOffsets = new float[RowNames.Length];
    private readonly float[] _rowScrollLimits = new float[RowNames.Length];
    private readonly string?[] _rowObjectIds = new string?[RowNames.Length];

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
    /// <c>Images/UI/GameSessionScreenUI/object-info/&lt;type-lowercase&gt;.png</c>. Used
    /// only as a fallback when <see cref="ObjectInfoPanelData.Image"/> is null or its file
    /// is missing — no files exist yet at this path, so this fallback currently always
    /// misses. A missing file is cached as null so the disk is only probed once per type.
    /// </summary>
    private readonly Dictionary<string, SKBitmap?> _objectImagesByType = new(StringComparer.Ordinal);

    /// <summary>
    /// Lazily-resolved per-object image, keyed by <see cref="ObjectInfoPanelData.Image"/>
    /// (the object's own resolved sprite path, e.g. an asteroid/ship sprite under
    /// <c>Images/CelestialObjects</c>). A missing file is cached as null so the disk is
    /// only probed once per path.
    /// </summary>
    private readonly Dictionary<string, SKBitmap?> _objectImagesByPath = new(StringComparer.Ordinal);

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
            lines.Add(("Name", d.Survey is not null ? d.ObjectId : d.DisplayName ?? d.ObjectId));
            lines.Add(("Speed", $"{d.SpeedKmS:0.###} km/s"));
            if (d.Countermeasure is { } countermeasure)
            {
                lines.AddRange(CountermeasureLines(countermeasure));
            }
            else if (d.Torpedo is { } torpedo)
            {
                lines.Add(("Target", torpedo.Target));
                lines.Add(("Travelled", $"{torpedo.TravelledKm:0.###} km"));
                lines.Add(("ETA", torpedo.EtaSeconds is { } eta ? $"{eta:0.###} s" : "—"));
                lines.Add(("Hit chance", $"{torpedo.HitChancePercent}%"));
            }
            else if (d.Survey is { } survey)
            {
                lines.Add(("Mass", $"{survey.MassKg} kg"));
                lines.Add(("Composition", survey.CompositionKnown ? survey.CompositionType ?? "Unknown" : "Unknown"));
                if (survey.CompositionKnown && !survey.Resources.IsDefaultOrEmpty)
                {
                    foreach (var resource in survey.Resources.OrderBy(r => r.ItemTypeId, StringComparer.Ordinal))
                        lines.Add((TradeItemPresentation.ItemDisplayName(resource.ItemTypeId), $"{resource.Permille / 10m:0.#}%"));
                }
            }
            else
            {
                lines.Add(("Direction", $"{d.Direction:F0}°"));
                if (d.RenderObjectType == SpaceObjectType.NpcShip && d.CaptainDisplayName is { } captain)
                    lines.Add(("Captain", captain));
            }
            if (d.DistanceKm is { } distanceKm)
                lines.Add(("Distance", TacticalMapSettings.FormatDistance(distanceKm * 1000)));
            if (d.ClusterName is { } cluster)
            {
                lines.Add(("Cluster", cluster));
                lines.Add(("Profile", d.ClusterProfile ?? "—"));
                lines.Add(("Straight flight estimate", d.StraightFlightDays is { } days ? $"{days:0.###} calendar days" : "Unavailable"));
                lines.Add(("Estimate epoch", $"T+{d.EstimateMotionTimeMs} simulation ms"));
                lines.Add(("Potential cargo", d.ClusterDirections ?? "—"));
            }
            if (d.ResourceCluster is { } resourceCluster)
            {
                lines.Add(("Resource cluster", resourceCluster));
                lines.Add(("Resource owner", d.ResourceOwner ?? "—"));
            }
            if (d.RenderObjectType == SpaceObjectType.Station && d.MarketKnowledge is { } market &&
                string.Equals(market.StationObjectId, d.ObjectId, StringComparison.Ordinal))
            {
                lines.Add(("Role", market.StationRole));
                lines.Add(("Market", $"{(market.IsAvailable ? "Available" : "Unavailable")} / {(market.IsStale ? "STALE" : "FRESH")}"));
                lines.Add(("Observed", $"T+{market.ObservedAtGameTimeMs} ms"));
                foreach (var band in new[] { StationMarketStockState.Shortage, StationMarketStockState.Normal, StationMarketStockState.Surplus })
                {
                    var items = market.StockBands.IsDefaultOrEmpty ? [] : market.StockBands
                        .Where(b => b.StockState == band).Select(b => b.ItemTypeId).Order(StringComparer.Ordinal).ToArray();
                    lines.Add((band.ToString(), items.Length == 0 ? "—" : string.Join(", ", items)));
                }
            }
        }
        else
        {
            lines.Add(("Name", "—"));
            lines.Add(("Speed", "—"));
            lines.Add(("Direction", "—"));
        }

        return lines;
    }

    public static List<(string Label, string Value)> CountermeasureLines(CountermeasureSnapshot flight)
    {
        var defense = flight.RatingBreakdown.DefenseOperator;
        var attack = flight.RatingBreakdown.TorpedoOperator;
        return [
            ("Цель", flight.TargetTorpedoId),
            ("Состояние", flight.Phase == CountermeasurePhase.Guiding ? "Наведение" : "Промах"),
            ("Шанс", $"{flight.FrozenChanceTenths / 10m:0.0}%"),
            ("Оператор ПР", defense.DisplayName),
            ("База / навык", $"{defense.BaseRating:0.##} / {defense.Skill}"),
            ("Рейтинг ПР", $"{defense.EffectiveRating:0.##}"),
            ("Торпедист", attack?.DisplayName ?? "Legacy"),
            ("База / навык", attack is null ? "—" : $"{attack.BaseRating:0.##} / {attack.Skill}"),
            ("Рейтинг цели", $"{flight.RatingBreakdown.TorpedoRating:0.##}"),
            ("Формула", "clamp(50 + Rпр − Rт, 0, 100)")
        ];
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

    public bool Scroll(float x, float y, float delta)
    {
        if (!float.IsFinite(delta) || delta == 0) return false;
        for (int i = 0; i < _rowBodyRects.Length; i++)
        {
            if (!_rowBodyRects[i].Contains(x, y)) continue;
            _rowScrollOffsets[i] = Math.Clamp(_rowScrollOffsets[i] - Math.Sign(delta) * 3 * LineHeight, 0, _rowScrollLimits[i]);
            return true;
        }
        return false;
    }

    internal float ScrollOffset(int row) => _rowScrollOffsets[row];

    // ── Render ──────────────────────────────────────────────────

    /// <param name="viewportWidth">Logical (unscaled) viewport width — the panel is right-aligned against it.</param>
    /// <param name="top">
    /// Logical-space y of the panel's own top edge — the caller positions this below
    /// whatever else already occupies the top-right corner (the Speed/Scale panels),
    /// since a fixed <see cref="Margin"/> from the top would overlap them.
    /// </param>
    public void Render(SKCanvas canvas, float viewportWidth, float top, ObjectInfoPanelData? playerShip, ObjectInfoPanelData? selectedOrActive,
        float viewportHeight = float.PositiveInfinity)
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
                float bodyHeight = Math.Max(RowBodyHeight, 2 * Padding + BuildRenderLines(rowData[i]).Count * LineHeight);
                float fullHeight = bodyHeight;
                bodyHeight = Math.Min(bodyHeight, Math.Max(0, viewportHeight - Margin - rowY - RowCaptionHeight));
                if (_rowObjectIds[i] != rowData[i]?.ObjectId)
                {
                    _rowObjectIds[i] = rowData[i]?.ObjectId;
                    _rowScrollOffsets[i] = 0;
                }
                _rowScrollLimits[i] = Math.Max(0, fullHeight - bodyHeight);
                _rowScrollOffsets[i] = Math.Clamp(_rowScrollOffsets[i], 0, _rowScrollLimits[i]);

                var captionRect = new SKRect(left, rowY, left + PanelWidth, rowY + RowCaptionHeight);
                var bodyRect = opened
                    ? new SKRect(left, captionRect.Bottom, left + PanelWidth, captionRect.Bottom + bodyHeight)
                    : SKRect.Empty;

                _rowCaptionRects[i] = captionRect;
                _rowBodyRects[i] = bodyRect;

                DrawBeveledCaption(canvas, captionRect, _rowCaptionBgPaint);
                canvas.DrawText(RowNames[i], captionRect.Left + Padding, captionRect.MidY + _rowTitlePaint.TextSize / 3f, _rowTitlePaint);

                if (opened)
                    DrawRowBody(canvas, bodyRect, rowData[i], _rowScrollOffsets[i], _rowScrollLimits[i]);

                rowY += opened ? (RowCaptionHeight + bodyHeight) : RowCaptionHeight;
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

    private float ValueOffset(ObjectInfoPanelData? data, List<(string Label, string Value)> lines) =>
        data?.Survey is not null || data?.Torpedo is not null || data?.Countermeasure is not null || data?.MarketKnowledge is not null || data?.ClusterName is not null
            ? Math.Max(62f, lines.Max(line => _labelPaint.MeasureText(line.Label)) + Padding) : 62f;

    /// <summary>Wrap market values using the same measured text width used for rendering and body height.</summary>
    internal List<(string Label, string Value)> BuildRenderLines(ObjectInfoPanelData? data)
    {
        var source = BuildLines(data);
        if (data?.MarketKnowledge is null && data?.ClusterName is null) return source;
        float width = PanelWidth - ImageWidth - 4 * Padding - ValueOffset(data, source);
        var result = new List<(string Label, string Value)>();
        foreach (var (label, value) in source)
        {
            string remaining = value;
            bool first = true;
            while (_valuePaint.MeasureText(remaining) > width)
            {
                int length = Math.Max(1, (int)_valuePaint.BreakText(remaining, width));
                int space = remaining.LastIndexOf(' ', length - 1, length);
                if (space > 0) length = space;
                result.Add((first ? label : "", remaining[..length].TrimEnd()));
                remaining = remaining[length..].TrimStart();
                first = false;
            }
            result.Add((first ? label : "", remaining));
        }
        return result;
    }

    private void DrawRowBody(SKCanvas canvas, SKRect bodyRect, ObjectInfoPanelData? data, float scrollOffset, float scrollLimit)
    {
        canvas.DrawRect(bodyRect, _panelBgPaint);
        canvas.DrawRect(bodyRect, _panelBorderPaint);

        canvas.Save();
        canvas.ClipRect(bodyRect);

        float imgX = bodyRect.Left + Padding;
        float imgY = bodyRect.Top + Padding - scrollOffset;
        var imageRect = new SKRect(imgX, imgY, imgX + ImageWidth, imgY + ImageHeight);

        var image = data is { } d ? ResolveObjectImage(d) : null;
        if (image is not null)
        {
            canvas.Save();
            if (data is { RenderObjectType: SpaceObjectType.NpcShip, RelationToPlayer: PlayerRelation.Enemy })
                canvas.Scale(-1, 1, imageRect.MidX, imageRect.MidY);
            canvas.DrawBitmap(image, imageRect, _imagePaint);
            canvas.Restore();
        }
        else
            canvas.DrawRect(imageRect, _imagePlaceholderPaint);
        canvas.DrawRect(imageRect, _panelBorderPaint);

        float textX = imageRect.Right + Padding;
        float textY = imgY + LineHeight - 3f;
        var lines = BuildRenderLines(data);
        float valueOffset = ValueOffset(data, BuildLines(data));
        foreach (var (label, value) in lines)
        {
            canvas.DrawText(label, textX, textY, _labelPaint);
            canvas.DrawText(value, textX + valueOffset, textY, _valuePaint);
            textY += LineHeight;
        }
        canvas.Restore();
        if (scrollLimit > 0 && bodyRect.Height > 0)
        {
            float thumbHeight = Math.Max(12, bodyRect.Height * bodyRect.Height / (bodyRect.Height + scrollLimit));
            float thumbTop = bodyRect.Top + (bodyRect.Height - thumbHeight) * scrollOffset / scrollLimit;
            canvas.DrawRect(new SKRect(bodyRect.Right - 4, thumbTop, bodyRect.Right - 1, thumbTop + thumbHeight), _labelPaint);
        }
    }

    /// <summary>
    /// Resolves the bitmap to draw for one row: the object's own resolved sprite
    /// (<see cref="ObjectInfoPanelData.Image"/>) when present and loadable, otherwise the
    /// generic per-type icon, otherwise null (caller draws the placeholder rect).
    /// </summary>
    private SKBitmap? ResolveObjectImage(ObjectInfoPanelData data)
    {
        if (data.Image is { Length: > 0 } imagePath)
        {
            if (!_objectImagesByPath.TryGetValue(imagePath, out var byPath))
            {
                byPath = LoadImage(imagePath);
                _objectImagesByPath[imagePath] = byPath;
            }

            if (byPath is not null)
                return byPath;
        }

        if (data.Survey is not null || data.RenderObjectType is not { } renderObjectType)
            return null;

        if (_objectImagesByType.TryGetValue(renderObjectType, out var byType))
            return byType;

        var fallback = LoadImage($"{ObjectImageAssetsPath}/{renderObjectType.ToLowerInvariant()}.png");
        _objectImagesByType[renderObjectType] = fallback;
        return fallback;
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

/// <summary>Snapshot of one object's info-panel content, including optional authoritative survey knowledge.</summary>
public readonly record struct ObjectInfoPanelData(
    string ObjectId,
    string? DisplayName,
    double SpeedKmS,
    double Direction,
    string? RenderObjectType,
    string? Image = null,
    AsteroidSurveySnapshot? Survey = null,
    string? CaptainDisplayName = null,
    string? RelationToPlayer = null,
    double? DistanceKm = null,
    TorpedoInspectionData? Torpedo = null, CountermeasureSnapshot? Countermeasure = null,
    StationMarketKnowledgeSnapshot? MarketKnowledge = null,
    string? ClusterName = null, string? ClusterProfile = null, string? ClusterDirections = null,
    string? ResourceCluster = null, string? ResourceOwner = null,
    double? StraightFlightDays = null, long? EstimateMotionTimeMs = null);

/// <summary>Presentation of confirmed flight and shared motion extrapolation.</summary>
public sealed record TorpedoInspectionData(string Target, double TravelledKm, double? EtaSeconds, int HitChancePercent);
