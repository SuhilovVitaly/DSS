using DeepSpaceSaga.Contracts;
using System.Globalization;
using SkiaSharp;

namespace DeepSpaceSaga.Client.UI.Screens.GameSession;

internal sealed record ClusterStationPresentation(string ClusterName, string Profile, string Directions);
internal sealed record DomainMapMarker(string Id, string Name, string[] MemberIds, double X, double Y, MapWorldBounds Bounds,
    MapOrbitCurve? Orbit, double Diameter);

internal static class ClusterMapPresentation
{
    internal static IReadOnlyList<DomainMapMarker> DomainMarkers(StationClusterMapSnapshot map, IEnumerable<ObjectMotionSnapshot> objects)
    {
        var poses = PosesById(objects);
        return map.Clusters.Where(c => !c.StationIds.IsDefaultOrEmpty).Select(c =>
            Marker(c.Id, c.Name, c.StationIds.Where(poses.ContainsKey).Select(id => poses[id]).ToArray(), averageOrbit: true))
            .Where(m => m is not null).Cast<DomainMapMarker>().ToArray();
    }

    internal static IReadOnlyList<DomainMapMarker> ResourceMarkers(StationClusterMapSnapshot map, IEnumerable<ObjectMotionSnapshot> objects)
    {
        if (map.ResourceBindings.IsDefaultOrEmpty) return [];
        var poses = PosesById(objects);
        return map.ResourceBindings.GroupBy(b => b.AnchorStationId, StringComparer.Ordinal).OrderBy(g => g.Key, StringComparer.Ordinal)
            .Select(g => Marker(g.Key, g.Key, g.Where(b => poses.ContainsKey(b.FieldId)).Select(b => poses[b.FieldId]).ToArray()))
            .OfType<DomainMapMarker>().Where(m => m.MemberIds.Length > 1).ToArray();
    }

    private static DomainMapMarker? Marker(string id, string name, ObjectMotionSnapshot[] members, bool averageOrbit = false)
    {
        if (members.Length == 0) return null;
        MapWorldBounds bounds = new();
        double major = 0, minor = 0, centerX = 0, centerY = 0;
        int count = 0;
        foreach (var member in members)
        {
            bounds.Include(member.X, member.Y);
            if (!averageOrbit || member.Orbit is not { } orbit) continue;
            major += orbit.SemiMajorAxis;
            minor += orbit.SemiMinorAxis;
            centerX += member.WorldOffsetX;
            centerY += member.WorldOffsetY;
            count++;
        }
        double x = bounds.MinX + (bounds.MaxX - bounds.MinX) / 2;
        double y = bounds.MinY + (bounds.MaxY - bounds.MinY) / 2;
        MapOrbitCurve? curve = null;
        if (count > 0)
        {
            curve = new(major / count, minor / count, centerX / count, centerY / count);
            var ellipse = curve.Value;
            double dx = x - ellipse.X, dy = y - ellipse.Y;
            double distance = double.Hypot(dx / ellipse.Major, dy / ellipse.Minor);
            if (distance > 0 && double.IsFinite(distance))
            {
                x = ellipse.X + dx / distance;
                y = ellipse.Y + dy / distance;
            }
        }
        double diameterSquared = 0;
        if (averageOrbit)
            for (int i = 1; i < members.Length; i++)
                for (int j = 0; j < i; j++)
                {
                    double dx = members[i].X - members[j].X, dy = members[i].Y - members[j].Y;
                    diameterSquared = Math.Max(diameterSquared, dx * dx + dy * dy);
                }
        return new(id, name, members.Select(m => m.ObjectId).Order(StringComparer.Ordinal).ToArray(),
            x, y, bounds, curve, Math.Sqrt(diameterSquared));
    }

    internal static string? ClusterName(AuthoritativeSnapshot? snapshot, string? stationId)
    {
        var map = snapshot?.ClusterMap;
        if (map is null || map.Stations.IsDefaultOrEmpty || map.Clusters.IsDefaultOrEmpty) return null;
        string? clusterId = map.Stations.FirstOrDefault(s => s.ObjectId == stationId)?.ClusterId;
        return map.Clusters.FirstOrDefault(c => c.Id == clusterId)?.Name;
    }

    internal static double? EstimateStraightDays(double distanceWorld, double maxSpeedKmS)
    {
        if (!double.IsFinite(distanceWorld) || distanceWorld < 0 || !double.IsFinite(maxSpeedKmS) || maxSpeedKmS <= 0) return null;
        double days = distanceWorld / 10 / maxSpeedKmS * 300 / 86400;
        return double.IsFinite(days) ? days : null;
    }

    internal static string FormatStraightFlightDays(double? days)
    {
        if (days is not { } value || !double.IsFinite(value) || value < 0) return "Unavailable";
        if (value < 1)
        {
            double hours = value * 24;
            if (Math.Round(hours, 3) >= 24) return "< 1 calendar day";
            if (hours is > 0 and < .001) return "< 0.001 calendar hours";
            return string.Format(CultureInfo.InvariantCulture, "{0:0.###} calendar {1}", hours, hours == 1 ? "hour" : "hours");
        }
        return string.Format(CultureInfo.InvariantCulture, "{0:0.###} calendar {1}", value, value == 1 ? "day" : "days");
    }

    internal static void DrawDirections(SKCanvas canvas, StationClusterMapSnapshot map, string? selectedId,
        IReadOnlyList<ObjectMotionSnapshot> poses, CameraState camera, int width, int height)
        => DrawDirections(canvas, map, selectedId, PosesById(poses), camera, width, height);

    private static Dictionary<string, ObjectMotionSnapshot> PosesById(IEnumerable<ObjectMotionSnapshot> objects)
    {
        var poses = new Dictionary<string, ObjectMotionSnapshot>(StringComparer.Ordinal);
        foreach (var obj in objects) poses.TryAdd(obj.ObjectId, obj);
        return poses;
    }

    private static void DrawDirections(SKCanvas canvas, StationClusterMapSnapshot map, string? selectedId,
        IReadOnlyDictionary<string, ObjectMotionSnapshot> poses, CameraState camera, int width, int height)
    {
        if (selectedId is null || map.Links.IsDefaultOrEmpty) return;
        if (!poses.TryGetValue(selectedId, out var source) || source.RenderObjectType != SpaceObjectType.Station) return;
        using var dash = SKPathEffect.CreateDash([8, 6], 0);
        using var paint = new SKPaint
        {
            Color = new SKColor(180, 190, 120, 150),
            IsAntialias = true,
            Style = SKPaintStyle.Stroke,
            StrokeWidth = 1,
            PathEffect = dash
        };
        var (x, y) = camera.WorldToScreen(source.X, source.Y, width, height);
        foreach (var destination in map.Links.Where(l => l.FromStationId == selectedId).Select(l => l.ToStationId).Distinct())
        {
            if (!poses.TryGetValue(destination, out var target) || target.RenderObjectType != SpaceObjectType.Station) continue;
            var (tx, ty) = camera.WorldToScreen(target.X, target.Y, width, height);
            canvas.DrawLine(x, y, tx, ty, paint);
        }
    }

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

    internal static MapWorldBounds StationBounds(string stationId, IEnumerable<ObjectMotionSnapshot> objects)
    {
        MapWorldBounds bounds = new();
        var station = objects.FirstOrDefault(o => o.ObjectId == stationId && o.RenderObjectType == SpaceObjectType.Station);
        if (station is null) return bounds;
        bounds.Include(station.X, station.Y);
        if (station.Orbit is null) return bounds;
        foreach (var companion in objects)
        {
            if (companion.RenderObjectType == SpaceObjectType.Factory &&
                companion.Orbit == station.Orbit && (companion.WorldOffsetX != 0 || companion.WorldOffsetY != 0))
                bounds.Include(companion.X, companion.Y);
        }
        return bounds;
    }

    internal static ClusterResourceBinding? Resource(AuthoritativeSnapshot? snapshot, string objectId)
    {
        var bindings = snapshot?.ClusterMap?.ResourceBindings ?? default;
        return bindings.IsDefaultOrEmpty ? null : bindings.FirstOrDefault(b => b.FieldId == objectId);
    }

    internal static void DrawFactoryLinks(SKCanvas canvas, IReadOnlyList<ObjectRenderState> objects, IReadOnlySet<string> hidden,
        CameraState camera, int width, int height)
    {
        Dictionary<OrbitalElements, RenderMotion>? stations = null;
        using var paint = new SKPaint { Color = new SKColor(128, 128, 128, 135), StrokeWidth = 1, IsAntialias = true };
        foreach (var factory in objects)
        {
            if (factory.Pose.RenderObjectType != SpaceObjectType.Factory || hidden.Contains(factory.Pose.ObjectId) ||
                factory.Source.Orbit is not { } orbit) continue;
            if (stations is null)
            {
                stations = new();
                foreach (var station in objects)
                    if (station.Pose.RenderObjectType == SpaceObjectType.Station && !hidden.Contains(station.Pose.ObjectId) &&
                        station.Source.Orbit is { } stationOrbit)
                        stations.TryAdd(stationOrbit, station.Pose);
            }
            if (!stations.TryGetValue(orbit, out var owner)) continue;
            var (x1, y1) = camera.WorldToScreen(owner.X, owner.Y, width, height);
            var (x2, y2) = camera.WorldToScreen(factory.Pose.X, factory.Pose.Y, width, height);
            canvas.DrawLine(x1, y1, x2, y2, paint);
        }
    }

    internal static void Draw(SKCanvas canvas, AuthoritativeSnapshot snapshot, IEnumerable<ObjectMotionSnapshot> objects, CameraState camera, int width, int height, string? selectedId = null, MapDetailLevel detail = MapDetailLevel.Cluster)
    {
        if (detail != MapDetailLevel.Cluster) return;
        if (snapshot.ClusterMap is not { } map || map.Clusters.IsDefaultOrEmpty) return;
        var poses = PosesById(objects);
        DrawDirections(canvas, map, selectedId, poses, camera, width, height);
        using var paint = new SKPaint { IsAntialias = true, Color = new SKColor(160, 210, 225), TextSize = 14 };
        foreach (var cluster in map.Clusters)
        {
            if (cluster.StationIds.IsDefaultOrEmpty) continue;
            var members = cluster.StationIds.Where(poses.ContainsKey).Select(id => poses[id]).ToArray();
            if (members.Length == 0) continue;
            // The geometric center can sit in empty space between widely separated stations.
            (float X, float Y)? anchor = null;
            double bestDistance = double.PositiveInfinity;
            foreach (var station in members)
            {
                if (station.RenderObjectType != SpaceObjectType.Station) continue;
                var (sx, sy) = camera.WorldToScreen(station.X, station.Y, width, height);
                if (!(sx >= 0 && sx < width && sy >= 0 && sy < height)) continue;
                double distance = double.Hypot(sx - width / 2d, sy - height / 2d);
                if (distance >= bestDistance) continue;
                anchor = (sx, sy);
                bestDistance = distance;
            }
            if (anchor is not { } point) continue;
            var bounds = Bounds(cluster, members);
            var (left, top) = camera.WorldToScreen(bounds.MinX, bounds.MinY, width, height);
            var (right, bottom) = camera.WorldToScreen(bounds.MaxX, bounds.MaxY, width, height);
            paint.Style = SKPaintStyle.Stroke;
            paint.Color = new SKColor(160, 210, 225, 90);
            canvas.DrawRoundRect(new SKRect(left - 10, top - 10, right + 10, bottom + 10), 8, 8, paint);
            paint.Style = SKPaintStyle.Fill;
            paint.Color = new SKColor(160, 210, 225);
            string caption = $"{cluster.Name} · {members.Length} stations";
            float textWidth = paint.MeasureText(caption);
            float captionX = Math.Clamp(point.X - textWidth / 2, 0, Math.Max(0, width - textWidth));
            float captionY = point.Y >= paint.TextSize + 18
                ? point.Y - 18 : Math.Min(height, point.Y + 24);
            canvas.DrawText(caption, captionX, captionY, paint);
        }
    }
}
