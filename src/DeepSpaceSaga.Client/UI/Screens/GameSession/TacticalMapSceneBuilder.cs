using System.Collections.Immutable;
using DeepSpaceSaga.Contracts;
using SkiaSharp;

namespace DeepSpaceSaga.Client.UI.Screens.GameSession;

/// <summary>Projects the single frame baseline; existing projectors supply owned path samples.</summary>
internal sealed class TacticalMapSceneBuilder
{
    internal TacticalMapSceneCache Cache { get; } = new();
    internal TacticalMapSpatialIndex Index { get; }
    private readonly HashSet<string> _candidates = new(StringComparer.Ordinal);
    internal TacticalMapSceneBuilder(int indexCapacity = 10000) => Index = new(indexCapacity);
    internal TacticalMapSceneGeometry Prepare(TacticalMapFrameState frame, TacticalMapViewInput view)
    {
        var changes = Cache.Changes(frame, view);
        bool rebuildMarkers = (changes & TacticalMapSceneCache.MarkerDependencies) != 0;
        bool rebuildHits = (changes & TacticalMapSceneCache.HitDependencies) != 0;
        if (!rebuildMarkers && !rebuildHits && Cache.Current is { } cached)
        {
            var reused = new TacticalMapSceneGeometry(frame, view, RefreshMarkerStates(cached, frame), cached.HitCandidates);
            Cache.Store(reused, false, false);
            return reused;
        }
        Index.BeginUpdate();
        foreach (var state in frame.Objects)
            Index.Include(state.Pose.ObjectId, new(state.Pose.X, state.Pose.Y, state.Pose.X, state.Pose.Y));
        Index.EndUpdate();
        double ppu = view.Camera.PixelsPerWorldUnit;
        const double marginPixels = 256; // Greater than every marker/halo extent; exact cull follows.
        bool indexed = Index.Query(new(view.Camera.X - (view.Width / 2.0 + marginPixels) / ppu,
            view.Camera.Y - (view.Height / 2.0 + marginPixels) / ppu,
            view.Camera.X + (view.Width / 2.0 + marginPixels) / ppu,
            view.Camera.Y + (view.Height / 2.0 + marginPixels) / ppu), _candidates);
        var markers = ImmutableArray.CreateBuilder<TacticalMapMarkerGeometry>();
        var hits = ImmutableArray.CreateBuilder<TacticalMapHitCandidate>();
        if (view.Width <= 0 || view.Height <= 0) return new(frame, view, markers.ToImmutable(), hits.ToImmutable());
        var labels = view.Labels.ToDictionary(label => label.State.Source.ObjectId, StringComparer.Ordinal);
        for (int pass = 0; pass < 2; pass++)
            foreach (var state in frame.Objects)
            {
                string id = state.Pose.ObjectId;
                if (indexed && !_candidates.Contains(id)) continue;
                bool important = view.ImportantIds.Contains(id);
                if (important != (pass == 1) || view.ClusteredIds.Contains(id)) continue;
                var point = view.Camera.Project(state.Pose.X, state.Pose.Y, view.Width, view.Height);
                if (!float.IsFinite(point.X) || !float.IsFinite(point.Y)) continue;
                bool combat = GameSessionScreen.HasCombatMarker(state.Source);
                bool compact = !combat && !state.IsPlayerShip && !important &&
                    view.Camera.PixelsPerWorldUnit <= view.CompactMarkerPpu &&
                    state.Pose.RenderObjectType is not (SpaceObjectType.Planet or SpaceObjectType.Sun);
                float radius = combat ? GameSessionScreen.CombatMarkerRadius :
                    TacticalMapMarkerPolicy.GetMarkerRadiusPx(state.IsPlayerShip ? SpaceObjectType.PlayerShip : state.Pose.RenderObjectType);
                float margin = radius * 5 + 4;
                if (point.X < -margin || point.Y < -margin || point.X > view.Width + margin || point.Y > view.Height + margin) continue;
                markers.Add(new(state, point, radius, important, compact));
                float core = compact ? 2.5f : radius;
                if (point.X + core < 0 || point.Y + core < 0 || point.X - core > view.Width || point.Y - core > view.Height) continue;
                int priority = state.Pose.RenderObjectType == SpaceObjectType.Station ? 0 :
                    state.IsPlayerShip ? 1 : state.Pose.RenderObjectType == SpaceObjectType.NpcShip ? 2 : 3;
                SKRect? plaque = ObjectLabelRenderer.HasHullBar(state.Source) && labels.TryGetValue(id, out var label)
                    ? label.Geometry.PlaqueRect : null;
                hits.Add(new(id, point, core, priority, plaque));
            }
        var scene = new TacticalMapSceneGeometry(frame, view,
            rebuildMarkers ? markers.ToImmutable() : RefreshMarkerStates(Cache.Current!, frame), hits.ToImmutable());
        Cache.Store(scene, rebuildMarkers, rebuildHits);
        return scene;
    }
    private static ImmutableArray<TacticalMapMarkerGeometry> RefreshMarkerStates(TacticalMapSceneGeometry cached, TacticalMapFrameState frame)
    {
        if (cached.Frame.Objects == frame.Objects) return cached.Markers;
        var states = frame.Objects.ToDictionary(s => s.Source.ObjectId, StringComparer.Ordinal);
        return cached.Markers.Select(marker => marker with { State = states[marker.State.Source.ObjectId] }).ToImmutableArray();
    }

}
