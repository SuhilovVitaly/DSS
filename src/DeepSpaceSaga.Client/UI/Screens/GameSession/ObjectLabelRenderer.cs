using DeepSpaceSaga.Client.UI;
using DeepSpaceSaga.Contracts;
using SkiaSharp;

namespace DeepSpaceSaga.Client.UI.Screens.GameSession;

/// <summary>
/// Draws compact object labels on the tactical map:
/// leader line → dark plaque → bottom accent stripe → status square → text.
/// Uses orbit-based layout and per-object smoothing for plaque position.
/// </summary>
internal sealed class ObjectLabelRenderer
{
    private readonly CombatVisualSettings _combatSettings;
    internal IReadOnlyDictionary<string, ObjectLabelGeometry> Geometries => _geometries;
    internal static bool HasModuleBar(ObjectMotionSnapshot source) => ModuleBarFraction(source) is not null;

    internal static double? ModuleBarFraction(ObjectMotionSnapshot source)
    {
        if (source.IsDestroyed || source.RenderObjectType is not (SpaceObjectType.PlayerShip or SpaceObjectType.NpcShip) ||
            source.ModuleHealth.IsDefaultOrEmpty) return null;
        long maximum = source.ModuleHealth.Sum(m => (long)m.ArmorMax + m.StructureMax);
        if (maximum <= 0) return null;
        long current = source.ModuleHealth.Sum(m => (long)m.ArmorCurrent + m.StructureCurrent);
        return Math.Clamp((double)current / maximum, 0, 1);
    }
    private readonly SKPaint _leaderLinePaint;
    private readonly SKPaint _plaqueBgPaint;
    private readonly SKPaint _plaqueBorderPaint;
    private readonly SKPaint _textPaint;
    private readonly SKPaint _unknownTextPaint;
    private readonly SKPaint _statusSquarePaint;
    private readonly SKPaint _stripePaint;

    /// <summary>Per-object smoothed visible positions.</summary>
    private readonly ObjectLabelSmoother _smoother = new();

    /// <summary>
    /// Geometries computed during the last <see cref="ComputeGeometries"/> call,
    /// keyed by object ID — reused between leader and plaque passes so both see
    /// the same smoothed position.
    /// </summary>
    private readonly Dictionary<string, ObjectLabelGeometry> _geometries = new(StringComparer.Ordinal);

    /// <summary>Active object IDs from the current frame.</summary>
    private readonly HashSet<string> _activeIds = new(StringComparer.Ordinal);
    private readonly Dictionary<string, byte> _opacity = new(StringComparer.Ordinal);
    private readonly Dictionary<string, LabelMetrics> _labels = new(StringComparer.Ordinal);
    private readonly List<string> _staleLabels = new();
    private readonly List<SKRect> _occupiedPlaques = new();
    private readonly record struct LabelMetrics(string? RenderType, string? Name, string Text, float Width, float MaximumWidth);

    public ObjectLabelRenderer(CombatVisualSettings? combatSettings = null)
    {
        _combatSettings = combatSettings ?? CombatVisualSettings.Default;
        var typeface = SKTypeface.FromFamilyName("Consolas") ?? SKTypeface.Default;

        _leaderLinePaint = new SKPaint
        {
            Color = new SKColor(32, 32, 32),
            Style = SKPaintStyle.Stroke,
            StrokeWidth = 1f,
            IsAntialias = true
        };

        _plaqueBgPaint = new SKPaint
        {
            Color = TacticalMapPalette.LabelBackground,
            Style = SKPaintStyle.Fill
        };

        _plaqueBorderPaint = new SKPaint
        {
            Color = new SKColor(32, 32, 32),
            Style = SKPaintStyle.Stroke,
            StrokeWidth = 1f
        };

        _textPaint = new SKPaint
        {
            Color = new SKColor(200, 200, 200),
            TextSize = 12f,
            IsAntialias = true,
            Typeface = typeface
        };

        _unknownTextPaint = new SKPaint
        {
            Color = new SKColor(136, 136, 136),
            TextSize = 12f,
            IsAntialias = true,
            Typeface = typeface
        };

        _statusSquarePaint = new SKPaint
        {
            Style = SKPaintStyle.Fill,
            IsAntialias = true
        };

        _stripePaint = new SKPaint
        {
            Style = SKPaintStyle.Fill
        };
    }

    /// <summary>
    /// Compute smoothed label geometries for all visible objects.
    /// Must be called once per frame before DrawLeaders/DrawPlaques.
    /// </summary>
    public void ComputeGeometries(
        IReadOnlyList<ObjectRenderState> renderStates,
        double deltaSeconds,
        int viewportW,
        int viewportH,
        CameraState camera,
        bool resetSmoothing = false,
        TacticalMapSettings? mapSettings = null,
        Func<string, bool>? isImportant = null,
        IReadOnlySet<string>? clusteredIds = null,
        SKRect? availableMap = null,
        string? selectedObjectId = null,
        string? hoveredObjectId = null)
    {
        _geometries.Clear();
        _activeIds.Clear();
        _opacity.Clear();
        _occupiedPlaques.Clear();

        if (viewportW <= 0 || viewportH <= 0) return;

        if (resetSmoothing)
            _smoother.ResetAll();

        var viewport = new SKSize(viewportW, viewportH);

        // Reserve space for the player and explicit targets before secondary labels.
        for (int pass = 0; pass < 2; pass++)
            for (int i = 0; i < renderStates.Count; i++)
            {
                var state = renderStates[i];
                var predicted = state.Pose;
                string objectId = predicted.ObjectId;
                if (clusteredIds?.Contains(objectId) == true) continue;
                bool focused = objectId == selectedObjectId || objectId == hoveredObjectId;
                if (predicted.RenderObjectType is SpaceObjectType.Missile or SpaceObjectType.Countermeasure && !focused)
                    continue;
                bool important = focused || state.IsPlayerShip || HasModuleBar(state.Source) || isImportant?.Invoke(objectId) == true ||
                    predicted is { RenderObjectType: SpaceObjectType.NpcShip, RelationToPlayer: PlayerRelation.Enemy };
                if (important != (pass == 0)) continue;
                if (mapSettings is not null && !important && _occupiedPlaques.Count >= mapSettings.MaximumLabels) continue;
                if (mapSettings is not null && !important && camera.PixelsPerWorldUnit < mapSettings.LabelDetailPpu * .5) continue;
                _opacity[objectId] = mapSettings is null || important ? (byte)255 :
                    (byte)(255 * Math.Clamp((camera.PixelsPerWorldUnit / mapSettings.LabelDetailPpu - .5) * 2, 0, 1));

                var (objSx, objSy) = camera.WorldToScreen(predicted.X, predicted.Y, viewportW, viewportH);

                // Visibility filter: skip objects whose marker/glyph is fully outside viewport.
                // Marker radius from the shared policy (player ship included) so the
                // viewport culling matches the drawn marker size.
                float markerRadius = GameSessionScreen.HasCombatMarker(state.Source) ? GameSessionScreen.CombatMarkerRadius : TacticalMapMarkerPolicy.GetMarkerRadiusPx(
                    state.IsPlayerShip ? SpaceObjectType.PlayerShip : predicted.RenderObjectType);
                if (objSx < -markerRadius || objSx > viewportW + markerRadius ||
                    objSy < -markerRadius || objSy > viewportH + markerRadius)
                    continue;

                _activeIds.Add(objectId);
                var objectScreen = new SKPoint(objSx, objSy);

                bool isUnknown = predicted.RenderObjectType == SpaceObjectType.UnknownSpaceObject;
                float maximumWidth = ObjectLabelLayout.MaximumTextWidth(viewportW);
                if (!_labels.TryGetValue(objectId, out var label) ||
                    label.RenderType != predicted.RenderObjectType || label.Name != predicted.DisplayName ||
                    label.MaximumWidth != maximumWidth)
                {
                    string text = ObjectLabelText.Build(predicted.RenderObjectType, predicted.DisplayName, objectId);
                    var paint = isUnknown ? _unknownTextPaint : _textPaint;
                    text = FitText(text, paint, maximumWidth);
                    label = new(predicted.RenderObjectType, predicted.DisplayName, text,
                        paint.MeasureText(text), maximumWidth);
                    _labels[objectId] = label;
                }
                float textWidth = label.Width;

                // Target geometry from orbit layout (no smoothing).
                var targetGeom = ObjectLabelLayout.Create(objectScreen, predicted.Direction, textWidth,
                    viewport, markerRadius, HasModuleBar(state.Source));

                // Apply smoothing to get the visible plaque position.
                SKRect visiblePlaque = _smoother.Update(
                    objectId,
                    targetGeom.PlaqueRect,
                    targetGeom.PlaqueCenter,
                    deltaSeconds,
                    viewportW,
                    viewportH,
                    reset: resetSmoothing);

                if (important && availableMap is { } free)
                {
                    // Explicit targets must remain readable when fitted beside a panel.
                    float x = Math.Clamp(visiblePlaque.Left, free.Left + 4, Math.Max(free.Left + 4, free.Right - visiblePlaque.Width - 4));
                    float y = Math.Clamp(visiblePlaque.Top, free.Top + 4, Math.Max(free.Top + 4, free.Bottom - visiblePlaque.Height - 4));
                    visiblePlaque = new SKRect(x, y, x + visiblePlaque.Width, y + visiblePlaque.Height);
                }

                if (mapSettings is not null && !important && OverlapsExistingPlaque(visiblePlaque)) continue;
                _occupiedPlaques.Add(visiblePlaque);

                // Leader endpoint — always bottom-left corner of the visible plaque.
                var leaderEndPoint = new SKPoint(visiblePlaque.Left, visiblePlaque.Bottom);

                // Recompute status rect and text origin relative to the visible plaque.
                float sqX = visiblePlaque.Left + ObjectLabelLayout.TextPaddingX + ObjectLabelLayout.ContentOffsetX;
                float sqY = visiblePlaque.Top + (Math.Min(visiblePlaque.Height, ObjectLabelLayout.PlaqueHeight) - ObjectLabelLayout.StatusSquareSize) / 2f
                            + ObjectLabelLayout.StatusOffsetY;
                var statusRect = new SKRect(sqX, sqY,
                    sqX + ObjectLabelLayout.StatusSquareSize, sqY + ObjectLabelLayout.StatusSquareSize);

                float textX = statusRect.Right + ObjectLabelLayout.StatusTextGap;
                float textY = visiblePlaque.Top + ObjectLabelLayout.TextPaddingY + ObjectLabelLayout.TextOffsetY;

                _geometries[objectId] = new ObjectLabelGeometry(
                    visiblePlaque, leaderEndPoint, statusRect, new SKPoint(textX, textY),
                    targetGeom.PlaqueCenter);
            }

        _smoother.RemoveStaleExcept(_activeIds);
        _staleLabels.Clear();
        foreach (string id in _labels.Keys)
            if (!_activeIds.Contains(id)) _staleLabels.Add(id);
        foreach (string id in _staleLabels) _labels.Remove(id);
    }

    internal static string FitText(string text, SKPaint paint, float maximumWidth)
    {
        if (maximumWidth <= 0) return string.Empty;
        if (paint.MeasureText(text) <= maximumWidth) return text;
        const string ellipsis = "…";
        if (paint.MeasureText(ellipsis) > maximumWidth) return string.Empty;
        // Cut on text-element boundaries, preserving surrogate pairs and combining marks.
        int[] starts = System.Globalization.StringInfo.ParseCombiningCharacters(text);
        int low = 0, high = starts.Length;
        while (low < high)
        {
            int mid = low + (high - low + 1) / 2;
            int end = mid == starts.Length ? text.Length : starts[mid];
            if (paint.MeasureText(text[..end] + ellipsis) <= maximumWidth) low = mid;
            else high = mid - 1;
        }
        return text[..(low == starts.Length ? text.Length : starts[low])] + ellipsis;
    }

    private bool OverlapsExistingPlaque(SKRect plaque)
    {
        foreach (var r in _occupiedPlaques) if (r.IntersectsWith(plaque)) return true;
        return false;
    }

    /// <summary>
    /// Draw leader lines only — called BEFORE object glyphs so lines go behind ships.
    /// </summary>
    public void DrawLeaders(
        SKCanvas canvas,
        IReadOnlyList<ObjectRenderState> renderStates,
        int viewportW,
        int viewportH,
        CameraState camera)
    {
        for (int i = 0; i < renderStates.Count; i++)
        {
            string objectId = renderStates[i].Pose.ObjectId;
            if (!_geometries.TryGetValue(objectId, out var geometry))
                continue;

            var predicted = renderStates[i].Pose;
            var (objSx, objSy) = camera.WorldToScreen(predicted.X, predicted.Y, viewportW, viewportH);

            _leaderLinePaint.Color = _leaderLinePaint.Color.WithAlpha(_opacity[objectId]);
            canvas.DrawLine(objSx, objSy,
                geometry.LeaderEndPoint.X, geometry.LeaderEndPoint.Y,
                _leaderLinePaint);
        }
    }

    /// <summary>
    /// Draw plaque, stripe, status square and text — called AFTER object glyphs
    /// so the plaque UI sits on top.
    /// </summary>
    public void DrawPlaques(
        SKCanvas canvas,
        IReadOnlyList<ObjectRenderState> renderStates,
        long uiTimeMs,
        SimulationSpeed speed,
        int viewportW,
        int viewportH,
        CameraState camera, SolarSystemMapSnapshot? solarMap = null)
    {
        for (int i = 0; i < renderStates.Count; i++)
        {
            var state = renderStates[i];
            string objectId = state.Pose.ObjectId;
            if (!_geometries.TryGetValue(objectId, out var geometry))
                continue;

            var predicted = state.Pose;

            var plaqueRect = geometry.PlaqueRect;
            bool hasModuleBar = HasModuleBar(state.Source);
            if (hasModuleBar) plaqueRect.Bottom = Math.Max(plaqueRect.Top, plaqueRect.Bottom - ObjectLabelLayout.ModuleBarSpace);

            byte opacity = _opacity[objectId];
            _plaqueBgPaint.Color = TacticalMapPalette.LabelBackground.WithAlpha((byte)Math.Round(235 * opacity / 255d));
            _plaqueBorderPaint.Color = _plaqueBorderPaint.Color.WithAlpha(opacity);

            SKColor objectColor = state.IsPlayerShip
                ? SpaceMapColorResolver.PlayerShipColor
                : predicted.RenderObjectType switch
                {
                    SpaceObjectType.Missile => _combatSettings.Torpedo,
                    SpaceObjectType.Countermeasure => _combatSettings.Countermeasure,
                    SpaceObjectType.Wreck => _combatSettings.Wreck,
                    _ => SpaceMapColorResolver.GetColor(predicted.RenderObjectType, predicted.RelationToPlayer)
                };

            if (predicted.RenderObjectType == SpaceObjectType.Planet && solarMap is not null &&
                solarMap.Planets.FirstOrDefault(p => p.ObjectId == objectId) is { } planet)
                objectColor = SolarSystemLayerRenderer.PlanetColor(planet.Kind);

            // Plaque background + border
            canvas.DrawRect(plaqueRect, _plaqueBgPaint);
            canvas.DrawRect(plaqueRect, _plaqueBorderPaint);

            // Status square — blink driven by real/UI time, not game time
            if (StatusSquareAnimator.IsStatusSquareVisible(uiTimeMs, speed))
            {
                _statusSquarePaint.Color = objectColor.WithAlpha(opacity);
                canvas.DrawRect(geometry.StatusRect, _statusSquarePaint);
            }

            // Text
            string label = _labels[objectId].Text;
            bool isUnknown = predicted.RenderObjectType == SpaceObjectType.UnknownSpaceObject;
            var textPaint = isUnknown ? _unknownTextPaint : _textPaint;
            float textY = geometry.TextOrigin.Y + textPaint.TextSize;
            textPaint.Color = TacticalMapPalette.Label.WithAlpha((byte)Math.Round(242 * opacity / 255d));
            canvas.Save();
            canvas.ClipRect(plaqueRect);
            canvas.DrawText(label, geometry.TextOrigin.X, textY, textPaint);
            canvas.Restore();
            if (hasModuleBar)
            {
                double fraction = ModuleBarFraction(state.Source)!.Value;
                var bar = ObjectLabelLayout.ModuleBarRect(geometry.PlaqueRect);
                _stripePaint.Color = TacticalMapPalette.HpEmpty;
                canvas.DrawRect(bar, _stripePaint);
                bar.Right = bar.Left + bar.Width * (float)fraction;
                _stripePaint.Color = _combatSettings.HullHp;
                canvas.DrawRect(bar, _stripePaint);
            }
        }
    }
}
