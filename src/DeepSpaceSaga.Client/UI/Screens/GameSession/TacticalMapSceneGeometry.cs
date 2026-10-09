using System.Collections.Immutable;
using DeepSpaceSaga.Contracts;
using SkiaSharp;

namespace DeepSpaceSaga.Client.UI.Screens.GameSession;

internal readonly record struct TacticalMapCamera(double X, double Y, double PixelsPerWorldUnit)
{
    internal SKPoint Project(double x, double y, int width, int height) =>
        new((float)(width / 2.0 + (x - X) * PixelsPerWorldUnit),
            (float)(height / 2.0 + (y - Y) * PixelsPerWorldUnit));
}

internal sealed record TacticalMapViewInput(
    TacticalMapCamera Camera, int Width, int Height, float UiScale, SKRect FreeViewport,
    ImmutableArray<SKRect> Obstacles, long LocaleRevision, double CompactMarkerPpu,
    string? Selected, string? Active, string? Navigation, ImmutableHashSet<string> ImportantIds,
    ImmutableHashSet<string> ClusteredIds, ImmutableArray<TacticalMapClusterGeometry> Clusters,
    ImmutableArray<TacticalMapLabelGeometry> Labels, ImmutableArray<TacticalMapPathGeometry> Paths,
    ImmutableArray<TacticalMapTrailSegment> Trails)
{
    internal TacticalMapGeometrySettings Settings { get; init; } = new(.1, .5, 48, .001, 40, .1, 200, 20, 20);
}

internal readonly record struct TacticalMapGeometrySettings(double LabelDetailPpu, double TrailDetailPpu,
    int MaximumLabels, double ClusterPpu, double ClusterCellPixels, double ClusterHysteresis,
    double GridBaseCellPixels, double GridMinimumPixels, double GridFadePixels);

internal readonly record struct TacticalMapMarkerGeometry(ObjectRenderState State, SKPoint Center,
    float Radius, bool Important, bool Compact);
internal readonly record struct TacticalMapHitCandidate(string ObjectId, SKPoint Center,
    float Radius, int Priority, SKRect? HullPlaque);
internal readonly record struct TacticalMapClusterGeometry(int Level, double CellX, double CellY,
    double WorldX, double WorldY, SKPoint Center, int Count, MapWorldBounds Bounds);
internal readonly record struct TacticalMapLabelGeometry(ObjectRenderState State, ObjectLabelGeometry Geometry,
    byte Opacity, string Text, bool DrawPlaque);
internal sealed record TacticalMapPathGeometry(string ObjectId, string Kind,
    ImmutableArray<FutureTrajectoryPoint> WorldPoints, ImmutableArray<SKPoint> ScreenPoints);
internal readonly record struct TacticalMapTrailSegment(SKPoint From, SKPoint To, SKColor Color);

/// <summary>Owned arrays and immutable records; no cache lists, native handles or mutable camera escape.</summary>
internal sealed record TacticalMapSceneGeometry(TacticalMapFrameState Frame, TacticalMapViewInput View,
    ImmutableArray<TacticalMapMarkerGeometry> Markers, ImmutableArray<TacticalMapHitCandidate> HitCandidates)
{
    internal TacticalMapPaintCommands? PaintCommands { get; init; }

    internal static ImmutableArray<string> LayerOrder { get; } =
        ["grid", "trails", "forecasts", "label_leaders", "clusters", "markers", "label_plaques", "combat_effects", "ui"];
}
