using DeepSpaceSaga.Contracts;
using SkiaSharp;

namespace DeepSpaceSaga.Client.UI.Screens.GameSession;

internal sealed record ClusterStationPresentation(string ClusterName, string Profile, string Directions);

internal static class ClusterMapPresentation
{
    internal static ClusterStationPresentation? Station(AuthoritativeSnapshot? snapshot, string objectId)
    {
        if (snapshot?.ClusterMap is not { } map || map.Stations.IsDefaultOrEmpty || map.Clusters.IsDefaultOrEmpty) return null;
        if (!snapshot.Objects.Any(o => o.ObjectId == objectId && o.RenderObjectType == SpaceObjectType.Station)) return null;
        var member = map.Stations.FirstOrDefault(s => s.ObjectId == objectId);
        var cluster = map.Clusters.FirstOrDefault(c => c.Id == member?.ClusterId);
        if (member is null || cluster is null) return null;
        var links = map.Links.IsDefaultOrEmpty ? [] : map.Links.Where(l => l.FromStationId == objectId).ToArray();
        string directions = string.Join("; ", links.Select(l =>
        {
            var target = snapshot.Objects.FirstOrDefault(o => o.ObjectId == l.ToStationId);
            string cargo = l.ItemTypeIds.IsDefaultOrEmpty ? "visit / return" : string.Join(", ", l.ItemTypeIds);
            return $"{target?.DisplayName ?? l.ToStationId}: {cargo}";
        }));
        return new(cluster.Name, member.MarketProfileId, directions);
    }

    internal static MapWorldBounds Bounds(StationClusterData cluster, IEnumerable<ObjectMotionSnapshot> objects)
    {
        MapWorldBounds bounds = new();
        if (cluster.StationIds.IsDefaultOrEmpty) return bounds;
        var members = cluster.StationIds.ToHashSet(StringComparer.Ordinal);
        foreach (var obj in objects) if (members.Contains(obj.ObjectId)) bounds.Include(obj.X, obj.Y);
        return bounds;
    }

    internal static void Draw(SKCanvas canvas, AuthoritativeSnapshot snapshot, IEnumerable<ObjectMotionSnapshot> objects, CameraState camera, int width, int height)
    {
        if (snapshot.ClusterMap is not { } map || map.Clusters.IsDefaultOrEmpty) return;
        var poses = objects.ToArray();
        using var paint = new SKPaint { IsAntialias = true, Color = new SKColor(160, 210, 225), TextSize = 14 };
        foreach (var cluster in map.Clusters)
        {
            var members = poses.Where(o => !cluster.StationIds.IsDefaultOrEmpty && cluster.StationIds.Contains(o.ObjectId)).ToArray();
            if (members.Length == 0) continue;
            var (x, y) = camera.WorldToScreen(members.Average(o => o.X), members.Average(o => o.Y), width, height);
            if (x < 0 || x > width || y < 0 || y > height) continue;
            canvas.DrawText($"{cluster.Name} · {members.Length} stations", x, y - 24, paint);
        }
    }
}
