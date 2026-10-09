using DeepSpaceSaga.Client.UI;
using DeepSpaceSaga.Contracts;
using SkiaSharp;

namespace DeepSpaceSaga.Client.UI.Screens.GameSession;

/// <summary>
/// Draws compact object labels on the tactical map:
/// leader line → dark plaque → bottom accent stripe → status square → text.
/// Uses orbit-based layout and per-object smoothing for plaque position.
/// </summary>
internal sealed class ObjectLabelRenderer : IDisposable
{
    private bool _disposed;
    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _leaderLinePaint.Dispose();
        _plaqueBgPaint.Dispose();
        _plaqueBorderPaint.Dispose();
        _textPaint.Dispose();
        _unknownTextPaint.Dispose();
        _statusSquarePaint.Dispose();
        _stripePaint.Dispose();
    }

    private readonly CombatVisualSettings _combatSettings;
    internal IReadOnlyDictionary<string, ObjectLabelGeometry> Geometries => _geometries;
    internal static bool HasHullBar(ObjectMotionSnapshot source) =>
        !source.IsDestroyed && source.RenderObjectType is SpaceObjectType.PlayerShip or SpaceObjectType.NpcShip &&
        source.HullCombat is { ShipClassId: "ship.tetrarch", MaxHp: > 0, CurrentHp: > 0 };
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
    private long _localeRevision = -1;
    internal System.Collections.Immutable.ImmutableArray<TacticalMapLabelGeometry> CaptureGeometry(IReadOnlyList<ObjectRenderState> states)
    {
        var result = System.Collections.Immutable.ImmutableArray.CreateBuilder<TacticalMapLabelGeometry>();
        foreach (var state in states)
        {
            string id = state.Pose.ObjectId;
            if (!_geometries.TryGetValue(id, out var geometry)) continue;
            result.Add(new(state, geometry, _opacity[id], id == _groupOwner ? _groupText : _labels[id].Text,
                !_groupedIds.Contains(id) || id == _groupOwner));
        }
        return result.ToImmutable();
    }

    internal string? PreparedText(string objectId) => _labels.TryGetValue(objectId, out var label) ? label.Text : null;
    private readonly List<string> _staleLabels = new();
    private readonly List<SKRect> _occupiedPlaques = new();
    private readonly List<ObjectRenderState> _placementOrder = new();
    private readonly HashSet<string> _groupedIds = new(StringComparer.Ordinal);
    private string? _groupOwner;
    private string _groupText = string.Empty;
    internal IReadOnlySet<string> GroupedLabelIds => _groupedIds;
    internal string GroupLabelText => _groupText;
    private readonly record struct LabelMetrics(string? RenderType, string? Name, string Text, float Width, float MaximumWidth);

    public ObjectLabelRenderer(CombatVisualSettings? combatSettings = null)
    {
        _combatSettings = combatSettings ?? CombatVisualSettings.Default;
        using var ownedTypeface = SKTypeface.FromFamilyName("Consolas");
        var typeface = ownedTypeface ?? SKTypeface.Default;

        _leaderLinePaint = new SKPaint
        {
            Color = new SKColor(32, 32, 32),
            Style = SKPaintStyle.Stroke,
            StrokeWidth = 1f,
            IsAntialias = true
        };

        _plaqueBgPaint = new SKPaint
        {
            Color = new SKColor(22, 22, 22),
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
        string? navigationTargetId = null)
    {
        if (_localeRevision != Localization.Revision)
        {
            _labels.Clear();
            _localeRevision = Localization.Revision;
            resetSmoothing = true;
        }
        _geometries.Clear();
        _activeIds.Clear();
        _opacity.Clear();
        _occupiedPlaques.Clear();
        _groupedIds.Clear();
        _groupOwner = null;
        _groupText = string.Empty;

        if (viewportW <= 0 || viewportH <= 0 || availableMap is { IsEmpty: true }) return;

        if (resetSmoothing)
            _smoother.ResetAll();

        var viewport = new SKSize(viewportW, viewportH);
        var labelBounds = new SKRect(0, 0, viewportW, viewportH);
        if (availableMap is { } available && !available.IsEmpty)
            labelBounds = SKRect.Intersect(labelBounds, available);
        bool needsGroup = false;
        bool Important(ObjectRenderState s) => s.IsPlayerShip || HasHullBar(s.Source) ||
            s.Source.Torpedo is not null || isImportant?.Invoke(s.Pose.ObjectId) == true ||
            s.Pose is { RenderObjectType: SpaceObjectType.NpcShip, RelationToPlayer: PlayerRelation.Enemy };
        int Rank(ObjectRenderState s) => s.Pose.ObjectId == selectedObjectId ? 0 :
            s.Pose.ObjectId == navigationTargetId ? 1 : s.IsPlayerShip ? 2 : Important(s) ? 3 : 4;
        _placementOrder.Clear();
        _placementOrder.AddRange(renderStates);
        _placementOrder.Sort((a, b) =>
        {
            int rank = Rank(a).CompareTo(Rank(b));
            return rank != 0 ? rank : string.CompareOrdinal(a.Pose.ObjectId, b.Pose.ObjectId);
        });

        // Reserve space for the player and explicit targets before secondary labels.
        for (int pass = 0; pass < 2; pass++)
            for (int i = 0; i < _placementOrder.Count; i++)
            {
                var state = _placementOrder[i];
                var predicted = state.Pose;
                string objectId = predicted.ObjectId;
                if (clusteredIds?.Contains(objectId) == true) continue;
                bool important = Important(state);
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
                float maximumWidth = ObjectLabelLayout.MaximumTextWidth(important ? labelBounds.Width : viewportW);
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
                    viewport, markerRadius, HasHullBar(state.Source));

                // Apply smoothing to get the visible plaque position.
                SKRect visiblePlaque = _smoother.Update(
                    objectId,
                    targetGeom.PlaqueRect,
                    targetGeom.PlaqueCenter,
                    deltaSeconds,
                    viewportW,
                    viewportH,
                    reset: resetSmoothing);

                if (important)
                {
                    if (!TryPlaceImportant(visiblePlaque, labelBounds, out visiblePlaque)) needsGroup = true;
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

        if (needsGroup)
        {
            // One explicit group with a link from every member. Keep the selected
            // identity first and show the remaining count even when text is truncated.
            var members = _placementOrder.Where(s => Important(s) && _geometries.ContainsKey(s.Pose.ObjectId)).ToArray();
            if (members.Length > 0)
            {
                _groupOwner = members[0].Pose.ObjectId;
                var ownerGeometry = _geometries[_groupOwner];
                _geometries.Clear();
                string suffix = $" +{members.Length - 1}";
                _groupText = FitText(_labels[_groupOwner].Text, _textPaint,
                    Math.Max(0, ownerGeometry.PlaqueRect.Width - 38 - _textPaint.MeasureText(suffix))) + suffix;
                foreach (var member in members)
                {
                    _groupedIds.Add(member.Pose.ObjectId);
                    _geometries[member.Pose.ObjectId] = ownerGeometry;
                }
            }
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

    private bool TryPlaceImportant(SKRect preferred, SKRect bounds, out SKRect result)
    {
        float width = Math.Min(preferred.Width, Math.Max(0, bounds.Width));
        float height = Math.Min(preferred.Height, Math.Max(0, bounds.Height));
        float x = Math.Clamp(preferred.Left, bounds.Left, Math.Max(bounds.Left, bounds.Right - width));
        float y = Math.Clamp(preferred.Top, bounds.Top, Math.Max(bounds.Top, bounds.Bottom - height));
        result = new(x, y, x + width, y + height);
        // Fixed, bounded candidate set. Revalidate after smoothing and clamping.
        ReadOnlySpan<int> offsets = [0, 1, -1, 2, -2];
        foreach (int row in offsets)
            foreach (int column in offsets)
            {
                float left = x + column * (width + 4), top = y + row * (height + 4);
                var candidate = new SKRect(left, top, left + width, top + height);
                if (bounds.Contains(candidate) && !OverlapsExistingPlaque(candidate))
                {
                    result = candidate;
                    return true;
                }
            }
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
        CameraState camera)
    {
        DrawPlaquesCore(ref canvas, renderStates, uiTimeMs, speed, viewportW, viewportH, camera, null);
    }

    internal void RecordPlaques(TacticalMapPaintRecorder recorder, IReadOnlyList<ObjectRenderState> renderStates,
        long uiTimeMs, SimulationSpeed speed, int viewportW, int viewportH, CameraState camera)
    {
        var canvas = recorder.Canvas;
        DrawPlaquesCore(ref canvas, renderStates, uiTimeMs, speed, viewportW, viewportH, camera, recorder);
    }

    private void DrawPlaquesCore(ref SKCanvas canvas, IReadOnlyList<ObjectRenderState> renderStates,
        long uiTimeMs, SimulationSpeed speed, int viewportW, int viewportH, CameraState camera, TacticalMapPaintRecorder? recorder)
    {
        for (int i = 0; i < renderStates.Count; i++)
        {
            var state = renderStates[i];
            string objectId = state.Pose.ObjectId;
            if (_groupedIds.Contains(objectId) && objectId != _groupOwner) continue;
            if (!_geometries.TryGetValue(objectId, out var geometry))
                continue;

            var predicted = state.Pose;

            var plaqueRect = geometry.PlaqueRect;
            bool hasHullBar = HasHullBar(state.Source);
            if (hasHullBar) plaqueRect.Bottom = Math.Max(plaqueRect.Top, plaqueRect.Bottom - ObjectLabelLayout.HullBarSpace);

            byte opacity = _opacity[objectId];
            _plaqueBgPaint.Color = _plaqueBgPaint.Color.WithAlpha(opacity);
            _plaqueBorderPaint.Color = _plaqueBorderPaint.Color.WithAlpha(opacity);

            SKColor objectColor = state.IsPlayerShip
                ? SpaceMapColorResolver.PlayerShipColor
                : SpaceMapColorResolver.GetColor(predicted.RenderObjectType, predicted.RelationToPlayer);

            // Plaque background + border
            canvas.DrawRect(plaqueRect, _plaqueBgPaint);
            canvas.DrawRect(plaqueRect, _plaqueBorderPaint);

            // Bottom accent stripe
            var stripeRect = new SKRect(
                plaqueRect.Left,
                plaqueRect.Bottom - ObjectLabelLayout.StripeHeight,
                plaqueRect.Right,
                plaqueRect.Bottom);

            byte sr = (byte)((objectColor.Red + 52) / 3);
            byte sg = (byte)((objectColor.Green + 52) / 3);
            byte sb = (byte)((objectColor.Blue + 52) / 3);
            _stripePaint.Color = new SKColor(sr, sg, sb, opacity);
            canvas.DrawRect(stripeRect, _stripePaint);

            // Status square — blink driven by real/UI time, not game time
            if (recorder is not null)
                canvas = recorder.Animate(new(TacticalMapAnimationKind.Status, 0, 0, 0,
                    Rect: geometry.StatusRect, Color: objectColor.WithAlpha(opacity), Speed: speed));
            else if (StatusSquareAnimator.IsStatusSquareVisible(uiTimeMs, speed))
            {
                _statusSquarePaint.Color = objectColor.WithAlpha(opacity);
                canvas.DrawRect(geometry.StatusRect, _statusSquarePaint);
            }

            // Text
            string label = objectId == _groupOwner ? _groupText : _labels[objectId].Text;
            bool isUnknown = predicted.RenderObjectType == SpaceObjectType.UnknownSpaceObject;
            var textPaint = isUnknown ? _unknownTextPaint : _textPaint;
            if (!isUnknown)
            {
                textPaint.Color = new SKColor(
                    (byte)Math.Min(255, objectColor.Red + 60),
                    (byte)Math.Min(255, objectColor.Green + 60),
                    (byte)Math.Min(255, objectColor.Blue + 60));
            }
            float textY = geometry.TextOrigin.Y + textPaint.TextSize;
            textPaint.Color = textPaint.Color.WithAlpha(opacity);
            canvas.Save();
            canvas.ClipRect(plaqueRect);
            canvas.DrawText(label, geometry.TextOrigin.X, textY, textPaint);
            canvas.Restore();
            if (hasHullBar)
            {
                var hp = state.Source.HullCombat!;
                var bar = ObjectLabelLayout.HullBarRect(geometry.PlaqueRect);
                _stripePaint.Color = new SKColor(24, 40, 24);
                canvas.DrawRect(bar, _stripePaint);
                bar.Right = bar.Left + bar.Width * (float)Math.Clamp((double)hp.CurrentHp / hp.MaxHp, 0, 1);
                _stripePaint.Color = _combatSettings.HullHp;
                canvas.DrawRect(bar, _stripePaint);
            }
        }
    }
}
