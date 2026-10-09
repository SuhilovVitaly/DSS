using System.Collections.Immutable;
using DeepSpaceSaga.Contracts;
using SkiaSharp;

namespace DeepSpaceSaga.Client.UI.Screens.GameSession;

/// <summary>Projects the single frame baseline; existing projectors supply owned path samples.</summary>
internal sealed class TacticalMapSceneBuilder
{
    internal TacticalMapSceneGeometry Prepare(TacticalMapFrameState frame, TacticalMapViewInput view)
    {
        var markers = ImmutableArray.CreateBuilder<TacticalMapMarkerGeometry>();
        var hits = ImmutableArray.CreateBuilder<TacticalMapHitCandidate>();
        if (view.Width <= 0 || view.Height <= 0) return new(frame, view, markers.ToImmutable(), hits.ToImmutable());
        var labels = view.Labels.ToDictionary(label => label.State.Source.ObjectId, StringComparer.Ordinal);
        for (int pass = 0; pass < 2; pass++)
            foreach (var state in frame.Objects)
            {
                string id = state.Pose.ObjectId;
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
        return new(frame, view, markers.ToImmutable(), hits.ToImmutable());
    }
}
