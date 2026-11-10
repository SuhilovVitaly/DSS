using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Engine.Content;
using DeepSpaceSaga.Motion;

namespace DeepSpaceSaga.Engine.Scenario;

internal static class StationClusterSaveValidation
{
    internal static void Validate(GameStateData state, GameDataRegistry registry)
    {
        if (state.ClusterMap is not { } map)
        {
            if (state.SolarSystem?.GeneratorVersion == 2)
                throw new ScenarioException("clusterMap: version 2 requires materialized geography. Running world was not replaced.");
            return;
        }
        AiBaseGenerator.ValidateWorld(state);
        var aiIds = (state.AiMap?.Bases ?? []).Select(b => b.ObjectId).ToHashSet(StringComparer.OrdinalIgnoreCase);
        void Require([DoesNotReturnIf(false)] bool valid, string detail) { if (!valid) throw new ScenarioException($"clusterMap: {detail}. Running world was not replaced."); }
        Require(state.SolarSystem is not null && (map.RulesVersion is 1 or 2) && state.SolarSystem.GeneratorVersion == map.RulesVersion && !map.Clusters.IsDefaultOrEmpty &&
            !map.Stations.IsDefaultOrEmpty && !map.Links.IsDefaultOrEmpty, "solar system, version and resolved arrays required");
        var objects = state.SpaceObjects.ToDictionary(o => o.ObjectId, StringComparer.Ordinal);
        var clusters = new Dictionary<string, StationClusterData>(StringComparer.OrdinalIgnoreCase);
        var members = new Dictionary<string, ClusterStationData>(StringComparer.Ordinal);
        foreach (var c in map.Clusters)
        {
            Require(c is not null && !string.IsNullOrWhiteSpace(c.Id) && !string.IsNullOrWhiteSpace(c.Name) &&
                !string.IsNullOrWhiteSpace(c.Specialization) && clusters.TryAdd(c.Id, c) &&
                !c.StationIds.IsDefault && c.StationIds.Length is >= 10 and <= 12 && c.StationIds.Distinct(StringComparer.OrdinalIgnoreCase).Count() == c.StationIds.Length,
                "invalid or duplicate cluster/membership");
            Require(state.SolarSystem!.Belts.Any(b => b.Id == c!.BeltId), "unknown belt");
        }
        Require(!string.IsNullOrWhiteSpace(map.StartClusterId) && clusters.TryGetValue(map.StartClusterId, out var start) && start.Id == map.StartClusterId && map.Clusters.Length is >= 1 and <= 5, "invalid start cluster/count");
        foreach (var s in map.Stations)
        {
            Require(s is not null && !string.IsNullOrWhiteSpace(s.ObjectId) && !string.IsNullOrWhiteSpace(s.ClusterId) && !string.IsNullOrWhiteSpace(s.MarketProfileId) && members.TryAdd(s.ObjectId, s) &&
                clusters.TryGetValue(s.ClusterId, out var c) && c.Id == s.ClusterId && c.StationIds.Contains(s.ObjectId) &&
                objects.TryGetValue(s.ObjectId, out var obj) && obj.ObjectType == SpaceObjectType.Station && obj.MarketProfileId == s.MarketProfileId &&
                registry.StationMarketProfiles.Contains(s.MarketProfileId), "invalid member or market profile");
        }
        var declared = map.Clusters.SelectMany(c => c.StationIds).ToArray();
        Require(declared.Length == members.Count && declared.Distinct(StringComparer.OrdinalIgnoreCase).Count() == members.Count &&
            declared.All(id => members.ContainsKey(id) && !aiIds.Contains(id)) && state.SpaceObjects.Where(o => o.ObjectType == SpaceObjectType.Station && !aiIds.Contains(o.ObjectId)).All(o => members.ContainsKey(o.ObjectId)),
            "partial or multiply owned station membership");
        var linkIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var pairs = new HashSet<(string, string)>();
        foreach (var link in map.Links)
        {
            Require(link is not null && !string.IsNullOrWhiteSpace(link.Id) && linkIds.Add(link.Id) &&
                !string.IsNullOrWhiteSpace(link.FromStationId) && !string.IsNullOrWhiteSpace(link.ToStationId) &&
                members.ContainsKey(link.FromStationId) && members.ContainsKey(link.ToStationId) && link.FromStationId != link.ToStationId &&
                pairs.Add((link.FromStationId, link.ToStationId)) && !link.ItemTypeIds.IsDefault &&
                link.ItemTypeIds.Distinct(StringComparer.OrdinalIgnoreCase).Count() == link.ItemTypeIds.Length, "invalid or duplicate direction");
            var from = registry.StationMarketProfiles.GetDefinition(registry.StationMarketProfiles.GetIndex(members[link!.FromStationId].MarketProfileId));
            var to = registry.StationMarketProfiles.GetDefinition(registry.StationMarketProfiles.GetIndex(members[link.ToStationId].MarketProfileId));
            Require(link.ItemTypeIds.All(id => from.SupplyItemTypeIds.Contains(id) && to.DemandItemTypeIds.Contains(id)) &&
                (from.SupplyItemTypeIds.IsEmpty || !link.ItemTypeIds.IsEmpty), "cargo differs from profile supply/demand");
        }
        foreach (var c in map.Clusters)
        {
            var stations = c.StationIds.Select(id => objects[id]).ToArray();
            var reference = stations[0].Orbit;
            var belt = state.SolarSystem!.Belts.Single(b => b.Id == c.BeltId);
            Require(reference is not null && stations.All(o => o.Orbit is { } orbit && orbit.SemiMajorAxis == orbit.SemiMinorAxis &&
                orbit.OrbitalPeriodMs == reference.OrbitalPeriodMs && orbit.EpochSimulationTimeMs == reference.EpochSimulationTimeMs &&
                orbit.OrbitDirection == reference.OrbitDirection && orbit.SemiMajorAxis >= belt.InnerRadius && orbit.SemiMajorAxis <= belt.OuterRadius), "nonrigid or out-of-belt group");
            // Compare analytical positions at the common epoch, not possibly stale serialized poses.
            // Legacy source stations can be closer than the generated-station clearance.
            var positions = stations.Select(o => OrbitalMotionMath.At(new(o.ObjectId, o.PositionX, o.PositionY, 0, 0),
                o.Orbit!, reference!.EpochSimulationTimeMs)).ToArray();
            for (int i = 0; i < positions.Length; i++)
                for (int j = i + 1; j < positions.Length; j++)
                    Require(double.Hypot(positions[i].X - positions[j].X, positions[i].Y - positions[j].Y) > 0.000001,
                        $"overlapping stations {stations[i].ObjectId}/{stations[j].ObjectId}");
            if (map.RulesVersion == 1)
                StationClusterGenerator.ValidateGraph(stations, map.Links.Where(l => c.StationIds.Contains(l.FromStationId) && c.StationIds.Contains(l.ToStationId)).ToImmutableArray(), registry);
        }
        if (map.RulesVersion == 2) ValidateV2Geography(map, objects, registry);
        else Require(map.Geography is null, "legacy geography cannot carry a v2 descriptor");
        var seen = new HashSet<string>(StringComparer.Ordinal) { map.Stations[0].ObjectId };
        var queue = new Queue<string>(); queue.Enqueue(map.Stations[0].ObjectId);
        while (queue.TryDequeue(out var id)) foreach (var l in map.Links.Where(l => l.FromStationId == id)) if (seen.Add(l.ToStationId)) queue.Enqueue(l.ToStationId);
        Require(seen.Count == members.Count, "disconnected geography");
        seen.Clear(); seen.Add(map.Stations[0].ObjectId); queue.Enqueue(map.Stations[0].ObjectId);
        while (queue.TryDequeue(out var id)) foreach (var l in map.Links.Where(l => l.ToStationId == id)) if (seen.Add(l.FromStationId)) queue.Enqueue(l.FromStationId);
        Require(seen.Count == members.Count, "geography has no return path");
        var fields = state.StationResourceFields?.Asteroids ?? [];
        var rules = state.StationResourceFields?.Rules;
        bool formation = map.RulesVersion == 2 && rules is not null && rules.InnerRadiusKm == rules.OuterRadiusKm;
        var bindings = map.ResourceBindings.IsDefault ? [] : map.ResourceBindings.ToArray();
        Require(bindings.Length == fields.Count, "partial resource bindings");
        var bound = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var b in bindings)
        {
            Require(b is not null && !string.IsNullOrWhiteSpace(b.FieldId) && bound.Add(b.FieldId) &&
                !string.IsNullOrWhiteSpace(b.AnchorStationId) && !string.IsNullOrWhiteSpace(b.ClusterId) &&
                double.IsFinite(b.OffsetX) && double.IsFinite(b.OffsetY) && objects.TryGetValue(b.FieldId, out var resource) && resource.ObjectType == SpaceObjectType.Asteroid &&
                members.TryGetValue(b.AnchorStationId, out var anchor) && anchor.ClusterId == b.ClusterId &&
                fields.Any(a => a.ObjectId == b.FieldId && a.StationObjectId == b.AnchorStationId), "invalid field/owner reference");
            var owner = objects[b!.AnchorStationId]; var field = objects[b.FieldId];
            Require(owner.Orbit is { } orbit && field.Orbit is { } fo && fo.SemiMajorAxis == fo.SemiMinorAxis &&
                fo.OrbitalPeriodMs == orbit.OrbitalPeriodMs && fo.EpochSimulationTimeMs == orbit.EpochSimulationTimeMs && fo.OrbitDirection == orbit.OrbitDirection,
                "nonrigid resource orbit");
            ObjectMotionSnapshot AtEpoch(SpaceObjectData obj) => OrbitalMotionMath.At(
                new(obj.ObjectId, obj.PositionX, obj.PositionY, 0, 0, WorldOffsetX: obj.WorldOffsetX, WorldOffsetY: obj.WorldOffsetY),
                obj.Orbit!, owner.Orbit!.EpochSimulationTimeMs);
            var a = AtEpoch(owner); var f = AtEpoch(field);
            Require(Math.Abs(f.X - a.X - b.OffsetX) <= 0.000001 && Math.Abs(f.Y - a.Y - b.OffsetY) <= 0.000001, "resource offset differs from orbit");
            if (!formation) continue;
            Require(field.Orbit == owner.Orbit, "resource velocity differs from station");
            double radius = rules!.EffectiveFactoryRadiusKm * 10;
            // Compact saves can contain either the original ring or the irregular layout.
            double tolerance = rules.FactoryRadiusKm.HasValue ? radius * StationClusterGenerator.CompactFactoryRadiusVariation : 0;
            for (int number = 1; number <= StationClusterGenerator.FactoriesPerStation; number++)
            {
                string id = StationClusterGenerator.FactoryId(owner.ObjectId, number);
                Require(objects.TryGetValue(id, out var factory) && factory.ObjectType == SpaceObjectType.Factory &&
                    factory.Orbit == owner.Orbit && factory.IsKnown && !factory.IsDestroyed &&
                    Math.Abs(double.Hypot(factory.WorldOffsetX, factory.WorldOffsetY) - radius) <= tolerance + 1e-6,
                    "invalid station factory formation");
            }
        }
        if (formation)
            Require(objects.Values.Count(o => o.ObjectType == SpaceObjectType.Factory) == fields.Count * StationClusterGenerator.FactoriesPerStation,
                "unexpected station factory count");
    }

    private static void ValidateV2Geography(StationClusterMapSnapshot map,
        IReadOnlyDictionary<string, SpaceObjectData> objects, GameDataRegistry registry)
    {
        void Require([DoesNotReturnIf(false)] bool valid, string detail) { if (!valid) throw new ScenarioException($"clusterMap/v2: {detail}. Running world was not replaced."); }
        var rules = map.Geography;
        Require(rules is not null && double.IsFinite(rules.ReferenceSpeedKmS) && rules.ReferenceSpeedKmS > 0 &&
            double.IsFinite(rules.MinimumStationDays) && rules.MinimumStationDays >= 1 &&
            double.IsFinite(rules.NeighbourMinDays) && rules.NeighbourMinDays >= rules.MinimumStationDays &&
            double.IsFinite(rules.NeighbourMaxDays) && rules.NeighbourMaxDays >= rules.NeighbourMinDays,
            "missing or invalid reference speed/minimum/neighbour descriptor");
        double units = rules!.ReferenceSpeedKmS * 86400 / 300 * 10;
        Require(double.IsFinite(units) && units > 0, "reference speed exceeds supported range");
        var poses = new Dictionary<string, ObjectMotionSnapshot>(StringComparer.Ordinal);
        var lanes = new List<(double Inner, double Outer)>();
        foreach (var cluster in map.Clusters)
        {
            var reference = objects[cluster.StationIds[0]].Orbit!;
            var radii = cluster.StationIds.Select(id => objects[id].Orbit!.SemiMajorAxis).ToArray();
            lanes.Add((radii.Min(), radii.Max()));
            foreach (string id in cluster.StationIds)
            {
                var obj = objects[id];
                poses.Add(id, OrbitalMotionMath.At(new(id, 0, 0, 0, 0), obj.Orbit!, reference.EpochSimulationTimeMs));
            }
            foreach (string id in cluster.StationIds)
            {
                double nearest = double.PositiveInfinity;
                foreach (string other in cluster.StationIds.Where(other => other != id))
                {
                    double distance = double.Hypot(poses[id].X - poses[other].X, poses[id].Y - poses[other].Y);
                    double tolerance = 1e-6 + distance * 1e-10;
                    Require(double.IsFinite(distance) && distance + tolerance >= rules.MinimumStationDays * units, "station pair violates minimum days");
                    nearest = Math.Min(nearest, distance);
                }
                double toleranceNearest = 1e-6 + nearest * 1e-10;
                Require(nearest + toleranceNearest >= rules.NeighbourMinDays * units && nearest - toleranceNearest <= rules.NeighbourMaxDays * units,
                    "nearest station outside configured days");
            }
        }
        lanes.Sort((a, b) => a.Inner.CompareTo(b.Inner));
        for (int i = 1; i < lanes.Count; i++)
        {
            double gap = lanes[i].Inner - lanes[i - 1].Outer;
            Require(gap + 1e-6 + Math.Abs(gap) * 1e-10 >= rules.MinimumStationDays * units,
                "radial lanes do not prove the all-phase station minimum");
        }
        var members = map.Stations.ToDictionary(s => s.ObjectId, StringComparer.Ordinal);
        foreach (var link in map.Links.Where(l => members[l.FromStationId].ClusterId == members[l.ToStationId].ClusterId))
        {
            var a = poses[link.FromStationId]; var b = poses[link.ToStationId];
            double distance = double.Hypot(a.X - b.X, a.Y - b.Y), tolerance = 1e-6 + distance * 1e-10;
            Require(distance + tolerance >= rules.NeighbourMinDays * units && distance - tolerance <= rules.NeighbourMaxDays * units,
                "local link violates distance eligibility");
        }
        StationGeographyV2.ValidateGraph(map, registry);
    }
}
