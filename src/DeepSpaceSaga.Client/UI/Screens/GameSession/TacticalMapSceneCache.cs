namespace DeepSpaceSaga.Client.UI.Screens.GameSession;

[Flags]
internal enum TacticalMapRevision
{
    None = 0, World = 1, Pose = 2, Route = 4, Trail = 8, Camera = 16,
    Layout = 32, Settings = 64, Locale = 128, Selection = 256, All = 511
}

/// <summary>One retained scene per session, with explicit geometry dependencies.</summary>
internal sealed class TacticalMapSceneCache
{
    internal const TacticalMapRevision MarkerDependencies = TacticalMapRevision.World | TacticalMapRevision.Pose |
        TacticalMapRevision.Camera | TacticalMapRevision.Settings | TacticalMapRevision.Selection;
    internal const TacticalMapRevision HitDependencies = MarkerDependencies | TacticalMapRevision.Layout | TacticalMapRevision.Locale;
    internal const TacticalMapRevision PathDependencies = TacticalMapRevision.Pose | TacticalMapRevision.Route |
        TacticalMapRevision.Camera | TacticalMapRevision.Settings | TacticalMapRevision.Selection;
    internal const TacticalMapRevision TrailDependencies = TacticalMapRevision.Trail | TacticalMapRevision.Camera | TacticalMapRevision.Settings;
    internal const TacticalMapRevision LabelDependencies = HitDependencies;
    internal TacticalMapSceneGeometry? Current { get; private set; }
    internal long MarkerBuilds { get; private set; }
    internal long HitBuilds { get; private set; }
    internal TacticalMapRevision Changes(TacticalMapFrameState frame, TacticalMapViewInput view)
    {
        if (Current is not { } previous) return TacticalMapRevision.All;
        var old = previous.View;
        TacticalMapRevision changes = TacticalMapRevision.None;
        if (!previous.Frame.Objects.Select(s => s.Source.ObjectId).SequenceEqual(frame.Objects.Select(s => s.Source.ObjectId))) changes |= TacticalMapRevision.World;
        if (!previous.Frame.Objects.Select(PoseKey).SequenceEqual(frame.Objects.Select(PoseKey))) changes |= TacticalMapRevision.Pose;
        if (!RoutesEqual(previous.Frame, frame)) changes |= TacticalMapRevision.Route;
        if (!old.Trails.SequenceEqual(view.Trails)) changes |= TacticalMapRevision.Trail;
        if (old.Camera != view.Camera || old.Width != view.Width || old.Height != view.Height) changes |= TacticalMapRevision.Camera;
        if (old.UiScale != view.UiScale || old.FreeViewport != view.FreeViewport || !old.Obstacles.SequenceEqual(view.Obstacles) ||
            !old.Labels.Select(l => l.Geometry).SequenceEqual(view.Labels.Select(l => l.Geometry))) changes |= TacticalMapRevision.Layout;
        if (old.Settings != view.Settings || old.CompactMarkerPpu != view.CompactMarkerPpu) changes |= TacticalMapRevision.Settings;
        if (old.LocaleRevision != view.LocaleRevision) changes |= TacticalMapRevision.Locale;
        if (old.Selected != view.Selected || old.Active != view.Active || old.Navigation != view.Navigation ||
            !old.ImportantIds.SetEquals(view.ImportantIds) || !old.ClusteredIds.SetEquals(view.ClusteredIds)) changes |= TacticalMapRevision.Selection;
        return changes;
    }
    internal void Store(TacticalMapSceneGeometry scene, bool markersBuilt, bool hitsBuilt)
    {
        Current = scene;
        if (markersBuilt) MarkerBuilds++;
        if (hitsBuilt) HitBuilds++;
    }
    private static (double, double, double, double, string?, string?, bool) PoseKey(ObjectRenderState s) =>
        (s.Pose.X, s.Pose.Y, s.Pose.Direction, s.Pose.SpeedKmS, s.Pose.RenderObjectType, s.Pose.RelationToPlayer, s.IsPlayerShip);
    private static bool RoutesEqual(TacticalMapFrameState a, TacticalMapFrameState b)
    {
        if (a.Objects.Length != b.Objects.Length) return false;
        for (int i = 0; i < a.Objects.Length; i++)
        {
            var x = a.Objects[i].Pose.Motion; var y = b.Objects[i].Pose.Motion;
            if (x.NavigationTargetX != y.NavigationTargetX || x.NavigationTargetY != y.NavigationTargetY ||
                x.NavigationTargetObjectId != y.NavigationTargetObjectId || x.ApproachRoute != y.ApproachRoute ||
                x.Torpedo != y.Torpedo || x.Countermeasure != y.Countermeasure || x.Orbit != y.Orbit) return false;
        }
        return true;
    }
    internal void Clear() => Current = null;
}
