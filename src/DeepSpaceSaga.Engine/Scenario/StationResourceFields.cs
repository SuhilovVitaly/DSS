using System.Collections.Immutable;
using System.Diagnostics;
using System.Globalization;
using System.Text.Json.Serialization;
using DeepSpaceSaga.Engine.Content;
using DeepSpaceSaga.Engine.Rng;

namespace DeepSpaceSaga.Engine.Scenario;

public sealed record StationResourceFieldConfig(
    [property: JsonPropertyName("schemaVersion")] int SchemaVersion,
    [property: JsonPropertyName("firstObjectNumber")] int FirstObjectNumber,
    [property: JsonPropertyName("maxPlacementAttempts")] int MaxPlacementAttempts,
    [property: JsonPropertyName("innerRadiusKm")] double InnerRadiusKm,
    [property: JsonPropertyName("outerRadiusKm")] double OuterRadiusKm,
    [property: JsonPropertyName("stationClearanceKm")] double StationClearanceKm,
    [property: JsonPropertyName("asteroidSpacingKm")] double AsteroidSpacingKm,
    [property: JsonPropertyName("corridorHalfWidthKm")] double CorridorHalfWidthKm,
    [property: JsonPropertyName("roles")] IReadOnlyList<ResourceFieldRoleData> Roles,
    [property: JsonPropertyName("fieldKinds")] IReadOnlyList<ResourceFieldKindData> FieldKinds,
    [property: JsonPropertyName("massBands")] IReadOnlyList<ResourceMassBandData> MassBands,
    [property: JsonPropertyName("structuralScan")] ResourceSurveyRulesData StructuralScan);

public sealed record ResourceFieldRoleData(
    [property: JsonPropertyName("marketProfileId")] string MarketProfileId,
    [property: JsonPropertyName("fields")] IReadOnlyList<ResourceFieldCountData> Fields);
public sealed record ResourceFieldCountData(
    [property: JsonPropertyName("fieldKindId")] string FieldKindId,
    [property: JsonPropertyName("asteroidCount")] int AsteroidCount);
public sealed record ResourceFieldKindData(
    [property: JsonPropertyName("fieldKindId")] string FieldKindId,
    [property: JsonPropertyName("variants")] IReadOnlyList<ResourceVariantData> Variants);
public sealed record ResourceVariantData(
    [property: JsonPropertyName("variantId")] string VariantId,
    [property: JsonPropertyName("compositionType")] string CompositionType,
    [property: JsonPropertyName("weight")] int Weight,
    [property: JsonPropertyName("resources")] IReadOnlyList<ResourceFractionData> Resources);
public sealed record ResourceFractionData(
    [property: JsonPropertyName("itemTypeId")] string ItemTypeId,
    [property: JsonPropertyName("permille")] int Permille);
public sealed record ResourceMassBandData(
    [property: JsonPropertyName("bandId")] string BandId,
    [property: JsonPropertyName("minKg")] long MinKg,
    [property: JsonPropertyName("maxKg")] long MaxKg,
    [property: JsonPropertyName("weight")] int Weight);
public sealed record ResourceSurveyRulesData(
    [property: JsonPropertyName("rangeKm")] double RangeKm,
    [property: JsonPropertyName("durationGameTimeMs")] long DurationGameTimeMs,
    [property: JsonPropertyName("successChancePercent")] int SuccessChancePercent);
public sealed record ResourceFieldRngData(
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("seed")] ulong Seed,
    [property: JsonPropertyName("counter")] ulong Counter);
public sealed record ResourceFieldAsteroidData(
    [property: JsonPropertyName("objectId")] string ObjectId,
    [property: JsonPropertyName("stationObjectId")] string StationObjectId,
    [property: JsonPropertyName("fieldKindId")] string FieldKindId,
    [property: JsonPropertyName("variantId")] string VariantId,
    [property: JsonPropertyName("resources")] IReadOnlyList<ResourceFractionData> Resources,
    [property: JsonPropertyName("compositionKnown")] bool CompositionKnown = false);
public sealed record ResourceSurveyJobData(
    [property: JsonPropertyName("commandId")] string CommandId,
    [property: JsonPropertyName("objectId")] string ObjectId,
    [property: JsonPropertyName("moduleId")] string ModuleId,
    [property: JsonPropertyName("targetObjectId")] string TargetObjectId,
    [property: JsonPropertyName("startedGameTimeMs")] long StartedGameTimeMs,
    [property: JsonPropertyName("dueGameTimeMs")] long DueGameTimeMs,
    [property: JsonPropertyName("lastValidatedSimulationTimeMs")] long LastValidatedSimulationTimeMs);
public sealed record StationResourceFieldsState(
    [property: JsonPropertyName("schemaVersion")] int SchemaVersion,
    [property: JsonPropertyName("rules")] StationResourceFieldConfig Rules,
    [property: JsonPropertyName("nextObjectNumber")] int NextObjectNumber,
    [property: JsonPropertyName("asteroids")] IReadOnlyList<ResourceFieldAsteroidData> Asteroids,
    [property: JsonPropertyName("rngStreams")] IReadOnlyList<ResourceFieldRngData> RngStreams,
    [property: JsonPropertyName("surveys")] IReadOnlyList<ResourceSurveyJobData> Surveys);

/// <summary>Restores lazily: validating/capturing a saved stream never consumes randomness.</summary>
internal sealed class ResourceFieldRandom
{
    private ResourceFieldRngData _state;
    private Random? _random;

    internal ResourceFieldRandom(ResourceFieldRngData state)
    {
        if (state is null || string.IsNullOrWhiteSpace(state.Name))
            throw new ScenarioException("Resource field RNG name is required.");
        _state = state with { Counter = NormalizeCounter(state.Counter) };
    }

    internal static ulong NormalizeCounter(ulong counter)
    {
        if (counter % 10 == 0) return counter;
        if (counter > ulong.MaxValue - (10 - counter % 10))
            throw new ScenarioException("Resource field RNG counter overflow.");
        Trace.TraceWarning("Resource field RNG counter {0} rounded upward to a multiple of ten.", counter);
        return counter + 10 - counter % 10;
    }

    internal double NextDouble()
    {
        if (_state.Counter > ulong.MaxValue - 10)
            throw new ScenarioException("Resource field RNG counter overflow.");
        if (_random is null)
        {
            _random = RngStreamNames.CreateDeterministicRandom(_state.Seed);
            for (ulong i = 0; i < _state.Counter / 10; i++) _random.NextDouble();
        }
        double value = _random.NextDouble();
        _state = _state with { Counter = checked(_state.Counter + 10) };
        return value;
    }

    internal ResourceFieldRngData Capture() => _state;
}

internal static class StationResourceFields
{
    private static readonly string[] Profiles =
        ["market.transit", "market.mining", "market.industrial", "market.hydroponic", "market.scientific-military"];
    private static readonly string[] Purposes = ["Geometry", "Composition", "Mass"];

    internal static StationResourceFieldConfig ValidateConfig(StationResourceFieldConfig config, GameDataRegistry registry)
    {
        try
        {
            Require(config is not null && config.SchemaVersion == 1, "schemaVersion must be 1.");
            Require(config!.FirstObjectNumber >= 1 && config.MaxPlacementAttempts > 0, "Invalid ID range or placement attempts.");
            Require(new[] { config.InnerRadiusKm, config.OuterRadiusKm, config.StationClearanceKm,
                config.AsteroidSpacingKm, config.CorridorHalfWidthKm }.All(v => double.IsFinite(v) && v > 0), "Geometry must be finite and positive.");
            Require(config.StationClearanceKm <= config.InnerRadiusKm && config.InnerRadiusKm < config.OuterRadiusKm &&
                double.IsFinite(config.OuterRadiusKm * config.OuterRadiusKm * 100), "Invalid annulus/clearance.");
            var scan = config.StructuralScan;
            Require(scan is not null && double.IsFinite(scan.RangeKm) && scan.RangeKm > 0 &&
                scan.DurationGameTimeMs > 0 && scan.SuccessChancePercent is >= 0 and <= 100, "Invalid structuralScan rules.");
            var kinds = Unique(config.FieldKinds, k => k.FieldKindId, "fieldKinds").Select(k =>
            {
                var variants = Unique(k.Variants, v => v.VariantId, "variants").Select(v =>
                {
                    Require(v.CompositionType is "Ice" or "Silicate" or "Iron", "Invalid base composition.");
                    var resources = Unique(v.Resources, r => r.ItemTypeId, "resources");
                    int total = 0;
                    foreach (var r in resources)
                    {
                        Require(registry.ItemTypes.Contains(r.ItemTypeId), $"Unknown resource '{r.ItemTypeId}'.");
                        var item = registry.ItemTypes.GetDefinition(registry.ItemTypes.GetIndex(r.ItemTypeId));
                        Require(item.Category == TradeCategory.Resource && item.StorageKind == ItemStorageKind.Cargo && r.Permille > 0,
                            "Fractions require positive Resource/Cargo items.");
                        total = checked(total + r.Permille);
                    }
                    Require(total == 1000, "Resource fractions must sum to 1000.");
                    return v with { Resources = resources };
                }).ToImmutableArray();
                ValidateWeights(variants, v => v.Weight, v => v.VariantId, k.FieldKindId);
                return k with { Variants = variants };
            }).ToImmutableArray();
            var roles = Unique(config.Roles, r => r.MarketProfileId, "roles").Select(r =>
            {
                Require(Profiles.Contains(r.MarketProfileId, StringComparer.Ordinal) && registry.StationMarketProfiles.Contains(r.MarketProfileId),
                    $"Unknown market profile '{r.MarketProfileId}'.");
                var fields = Unique(r.Fields, f => f.FieldKindId, "fields");
                foreach (var f in fields)
                    Require(f.AsteroidCount > 0 && kinds.Any(k => k.FieldKindId == f.FieldKindId), "Invalid field count or kind reference.");
                return r with { Fields = fields };
            }).ToImmutableArray();
            Require(roles.Length == Profiles.Length, "Exactly the five MVP market profiles are required.");
            int next = config.FirstObjectNumber;
            foreach (var f in roles.SelectMany(r => r.Fields)) next = checked(next + f.AsteroidCount);
            var bands = Unique(config.MassBands, b => b.BandId, "massBands");
            long previousMax = 0;
            foreach (var b in bands.OrderBy(b => b.MinKg))
            {
                Require(b.MinKg >= 1_000_000 && b.MaxKg <= 1_000_000_000 && b.MinKg <= b.MaxKg && b.MinKg > previousMax,
                    "Mass bands must be disjoint inclusive intervals within 1000000..1000000000.");
                previousMax = b.MaxKg;
            }
            ValidateWeights(bands, b => b.Weight, b => b.BandId, "massBands");
            return config with { Roles = roles, FieldKinds = kinds, MassBands = bands };
        }
        catch (OverflowException ex) { throw new ContentException("Resource field count/weight/fraction overflow.", ex); }
    }

    private static ImmutableArray<T> Unique<T>(IReadOnlyList<T>? values, Func<T, string> id, string label)
    {
        Require(values is { Count: > 0 }, $"{label} must be nonempty.");
        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (var value in values!)
            Require(value is not null && !string.IsNullOrWhiteSpace(id(value)) && seen.Add(id(value)), $"Invalid/duplicate {label} ID.");
        return values.OrderBy(id, StringComparer.Ordinal).ToImmutableArray();
    }

    private static void ValidateWeights<T>(IReadOnlyList<T> values, Func<T, int> weight, Func<T, string> id, string label)
    {
        long total = 0;
        foreach (var value in values)
        {
            Require(weight(value) >= 0, "Weights must be nonnegative.");
            total = checked(total + weight(value));
        }
        Require(total > 0, "Weighted table must have a positive total.");
        Trace.TraceInformation("Resource fields {0}: {1}; total={2}", label,
            string.Join(",", values.Select(v => $"{id(v)}={weight(v)}")), total);
    }

    private static T Draw<T>(IReadOnlyList<T> values, Func<T, int> weight, ResourceFieldRandom random)
    {
        double draw = random.NextDouble() * values.Sum(v => (long)weight(v));
        long end = 0;
        foreach (var v in values)
        {
            end += weight(v);
            if (weight(v) > 0 && draw < end) return v;
        }
        return values.Last(v => weight(v) > 0);
    }

    private static void Require(bool valid, string message)
    {
        if (!valid) throw new ContentException("stationResourceFields: " + message);
    }

    private static void Saved(bool valid, string message)
    {
        if (!valid) throw new ScenarioException("stationResourceFields: " + message);
    }

    private static string ObjectId(int number) => "SPC-" + number.ToString("D4", CultureInfo.InvariantCulture);

    private static ResourceFieldRngData Stream(ulong masterSeed, string stationId, string purpose) => new(
        $"ResourceFields:{stationId}:{purpose}",
        RngStreamSeedDerivation.DeriveStreamSeed(RngStreamSeedDerivation.DeriveStreamSeed(masterSeed, "ResourceFields:" + stationId), purpose), 10000);

    private static TradingMapStationData[] Stations(GameStateData state)
    {
        Saved(state.TradingMap is not null, "Materialized trading map is required.");
        var stations = state.TradingMap!.Rules.Stations.OrderBy(s => s.ObjectId, StringComparer.Ordinal).ToArray();
        Saved(stations.Length == 5 && stations.Select(s => s.MarketProfileId).ToHashSet(StringComparer.Ordinal).SetEquals(Profiles),
            "The five owning stations/profiles are required.");
        foreach (var station in stations)
            Saved(state.SpaceObjects.Any(o => o.ObjectId == station.ObjectId && o.ObjectType == "Station" && o.MarketProfileId == station.MarketProfileId),
                "Missing owning station/profile.");
        return stations;
    }

    internal static GameStateData Generate(GameStateData materialized, ulong masterSeed, StationResourceFieldConfig config, GameDataRegistry registry)
    {
        config = ValidateConfig(config, registry);
        Saved(materialized.StationResourceFields is null, "Fields are already materialized.");
        var stations = Stations(materialized);
        var objects = materialized.SpaceObjects.ToList();
        var ids = objects.Select(o => o.ObjectId).ToHashSet(StringComparer.OrdinalIgnoreCase);
        int count = config.Roles.SelectMany(r => r.Fields).Sum(f => f.AsteroidCount);
        for (int i = 0; i < count; i++)
            Saved(!ids.Contains(ObjectId(checked(config.FirstObjectNumber + i))), "Generated object ID collision.");
        var manifest = new List<ResourceFieldAsteroidData>(count);
        var streams = new List<ResourceFieldRngData>();
        int next = config.FirstObjectNumber;
        foreach (var station in stations)
        {
            var owner = objects.Single(o => o.ObjectId == station.ObjectId);
            var rng = Purposes.Select(p =>
            {
                var stream = Stream(masterSeed, station.ObjectId, p);
                Trace.TraceWarning("Creating missing resource field RNG stream {0}.", stream.Name);
                return new ResourceFieldRandom(stream);
            }).ToArray();
            foreach (var field in config.Roles.Single(r => r.MarketProfileId == station.MarketProfileId).Fields)
            {
                var kind = config.FieldKinds.Single(k => k.FieldKindId == field.FieldKindId);
                for (int ordinal = 0; ordinal < field.AsteroidCount; ordinal++)
                {
                    double x = 0, y = 0;
                    bool placed = false;
                    for (int attempt = 0; attempt < config.MaxPlacementAttempts; attempt++)
                    {
                        double angle = 2 * Math.PI * rng[0].NextDouble();
                        double radius = Math.Sqrt(config.InnerRadiusKm * config.InnerRadiusKm + rng[0].NextDouble() *
                            (config.OuterRadiusKm * config.OuterRadiusKm - config.InnerRadiusKm * config.InnerRadiusKm));
                        x = owner.PositionX + Math.Cos(angle) * radius * 10;
                        y = owner.PositionY + Math.Sin(angle) * radius * 10;
                        if (Clear(x, y, config, materialized, objects.Where(o => o.ObjectType == "Asteroid")))
                        {
                            placed = true;
                            break;
                        }
                    }
                    Saved(placed, $"Placement exhausted for station {station.ObjectId}, kind {field.FieldKindId}, ordinal {ordinal}.");
                    var variant = Draw(kind.Variants, v => v.Weight, rng[1]);
                    var band = Draw(config.MassBands, b => b.Weight, rng[2]);
                    long mass = band.MinKg + (long)Math.Floor(rng[2].NextDouble() * (band.MaxKg - band.MinKg + 1));
                    string id = ObjectId(next);
                    next = checked(next + 1);
                    objects.Add(new(id, "Asteroid", "Permanent", null, x, y, 0, 0, "Stationary", mass, variant.CompositionType, [], IsKnown: true));
                    manifest.Add(new(id, station.ObjectId, kind.FieldKindId, variant.VariantId, variant.Resources));
                }
            }
            streams.AddRange(rng.Select(r => r.Capture()));
        }
        return materialized with
        {
            SpaceObjects = objects.ToImmutableArray(),
            StationResourceFields = new(1, config, next, manifest.ToImmutableArray(),
                streams.OrderBy(s => s.Name, StringComparer.Ordinal).ToImmutableArray(), [])
        };
    }

    private static double Distance(double x, double y, SpaceObjectData o) =>
        Math.Sqrt((x - o.PositionX) * (x - o.PositionX) + (y - o.PositionY) * (y - o.PositionY)) / 10;

    private static bool Clear(double x, double y, StationResourceFieldConfig rules, GameStateData world, IEnumerable<SpaceObjectData> asteroids)
    {
        if (!double.IsFinite(x) || !double.IsFinite(y)) return false;
        // Materialized-map validation also checks every asteroid on save/load.
        double stationClearanceKm = Math.Max(rules.StationClearanceKm, world.TradingMap!.Rules.ClearanceKm);
        if (world.SpaceObjects.Where(o => o.ObjectType == "Station").Any(o => Distance(x, y, o) < stationClearanceKm)) return false;
        if (asteroids.Any(o => Distance(x, y, o) < rules.AsteroidSpacingKm)) return false;
        foreach (var edge in world.TradingMap!.Edges)
        {
            var a = world.SpaceObjects.Single(o => o.ObjectId == edge.FromStationObjectId);
            var b = world.SpaceObjects.Single(o => o.ObjectId == edge.ToStationObjectId);
            double dx = b.PositionX - a.PositionX, dy = b.PositionY - a.PositionY;
            double length2 = dx * dx + dy * dy;
            Saved(double.IsFinite(length2) && length2 > 0, "Invalid corridor endpoints.");
            double t = Math.Clamp(((x - a.PositionX) * dx + (y - a.PositionY) * dy) / length2, 0, 1);
            double ex = x - a.PositionX - t * dx, ey = y - a.PositionY - t * dy;
            if (Math.Sqrt(ex * ex + ey * ey) / 10 < rules.CorridorHalfWidthKm) return false;
        }
        return true;
    }

    internal static GameStateData ValidateSaved(GameStateData state, GameDataRegistry registry)
    {
        var fields = state.StationResourceFields;
        if (fields is null) return state;
        Saved(fields.SchemaVersion == 1 && state.MasterSeed is not null, "Schema 1 and masterSeed are required.");
        StationResourceFieldConfig rules;
        try { rules = ValidateConfig(fields.Rules, registry); }
        catch (ContentException ex) { throw new ScenarioException(ex.Message); }
        var stations = Stations(state);
        Saved(fields.Asteroids is not null && fields.RngStreams is not null && fields.Surveys is not null, "Manifest collections are required.");
        var manifests = new Dictionary<string, ResourceFieldAsteroidData>(StringComparer.Ordinal);
        foreach (var asteroid in fields.Asteroids!)
            Saved(asteroid is not null && !string.IsNullOrWhiteSpace(asteroid.ObjectId) && manifests.TryAdd(asteroid.ObjectId, asteroid), "Invalid/duplicate manifest ID.");
        var normalized = new List<ResourceFieldAsteroidData>();
        var placed = new List<SpaceObjectData>();
        var expectedStreams = new Dictionary<string, ResourceFieldRngData>(StringComparer.Ordinal);
        int next = rules.FirstObjectNumber;
        foreach (var station in stations)
        {
            var owner = state.SpaceObjects.Single(o => o.ObjectId == station.ObjectId);
            foreach (var purpose in Purposes)
            {
                var stream = Stream(state.MasterSeed!.Value, station.ObjectId, purpose);
                expectedStreams.Add(stream.Name, stream);
            }
            foreach (var field in rules.Roles.Single(r => r.MarketProfileId == station.MarketProfileId).Fields)
            {
                var kind = rules.FieldKinds.Single(k => k.FieldKindId == field.FieldKindId);
                for (int i = 0; i < field.AsteroidCount; i++)
                {
                    string id = ObjectId(next++);
                    Saved(manifests.TryGetValue(id, out var m) && m.StationObjectId == station.ObjectId && m.FieldKindId == field.FieldKindId,
                        "Canonical manifest ID/owner/kind/count mismatch.");
                    var variant = kind.Variants.SingleOrDefault(v => v.VariantId == m!.VariantId && v.Weight > 0);
                    Saved(variant is not null && m!.Resources is not null && m.Resources.All(r => r is not null) &&
                        m.Resources.OrderBy(r => r.ItemTypeId, StringComparer.Ordinal).SequenceEqual(variant.Resources), "Variant/resource fraction mismatch.");
                    var obj = state.SpaceObjects.SingleOrDefault(o => o.ObjectId == id);
                    Saved(obj is not null && obj.ObjectType == "Asteroid" && obj.PersistenceType == "Permanent" && obj.IsKnown &&
                        !obj.IsDestroyed && obj.MovementType == "Stationary" && obj.SpeedMps == 0 && obj.DirectionDegrees == 0 &&
                        obj.Name is null && (obj.Modules is null || obj.Modules.Count == 0) && obj.CompositionType == variant!.CompositionType &&
                        rules.MassBands.Any(b => b.Weight > 0 && obj.MassKg >= b.MinKg && obj.MassKg <= b.MaxKg), "Invalid saved asteroid physics/composition/mass.");
                    double radius = Distance(obj!.PositionX, obj.PositionY, owner);
                    // A tiny tolerance accommodates subtraction of translated world coordinates.
                    Saved(radius >= rules.InnerRadiusKm - 1e-10 && radius <= rules.OuterRadiusKm + 1e-10 &&
                        Clear(obj.PositionX, obj.PositionY, rules, state, placed), "Saved field geometry violates placement rules.");
                    placed.Add(obj);
                    normalized.Add(m! with { Resources = variant!.Resources });
                }
            }
        }
        Saved(manifests.Count == normalized.Count && fields.NextObjectNumber >= next, "Manifest count/nextObjectNumber mismatch.");
        var player = state.SpaceObjects.SingleOrDefault(o => o.ObjectId == state.PlayerShipObjectId && o.ObjectType == "PlayerShip");
        Saved(player is not null, "Missing player.");
        var streams = new List<ResourceFieldRngData>();
        var streamNames = new HashSet<string>(StringComparer.Ordinal);
        foreach (var stream in fields.RngStreams!)
        {
            Saved(stream is not null && !string.IsNullOrWhiteSpace(stream.Name) && streamNames.Add(stream.Name), "Invalid/duplicate RNG stream.");
            ulong seed;
            ulong counter = ResourceFieldRandom.NormalizeCounter(stream!.Counter);
            if (expectedStreams.TryGetValue(stream.Name, out var expected))
            {
                seed = expected.Seed;
                var station = stations.Single(s => Purposes.Any(p => Stream(state.MasterSeed!.Value, s.ObjectId, p).Name == stream.Name));
                ulong count = (ulong)rules.Roles.Single(r => r.MarketProfileId == station.MarketProfileId).Fields.Sum(f => f.AsteroidCount);
                ulong draws = (counter >= 10000 ? counter - 10000 : 0) / 10;
                bool geometry = stream.Name.EndsWith(":Geometry", StringComparison.Ordinal);
                ulong required = stream.Name.EndsWith(":Composition", StringComparison.Ordinal) ? count : count * 2;
                Saved(counter >= 10000 && (geometry ? draws >= required && draws <= required * (ulong)rules.MaxPlacementAttempts && draws % 2 == 0 : draws == required),
                    "Invalid field RNG counter.");
            }
            else
            {
                Saved((player!.Modules ?? []).Any(m => stream.Name == $"ResourceSurvey:{player.ObjectId}:{m.ModuleId}"), "Unknown RNG namespace/module.");
                seed = RngStreamSeedDerivation.DeriveStreamSeed(state.MasterSeed!.Value, stream.Name);
                Saved(counter >= 10000, "Invalid survey RNG counter.");
            }
            Saved(stream.Seed == seed, "RNG seed mismatch.");
            streams.Add(stream with { Counter = counter });
        }
        Saved(expectedStreams.Keys.All(streamNames.Contains), "Missing field RNG stream.");
        var commands = new HashSet<string>(StringComparer.Ordinal);
        var modules = new HashSet<string>(StringComparer.Ordinal);
        var targets = new HashSet<string>(StringComparer.Ordinal);
        foreach (var job in fields.Surveys!)
        {
            Saved(job is not null && !string.IsNullOrWhiteSpace(job.CommandId) && commands.Add(job.CommandId) &&
                !string.IsNullOrWhiteSpace(job.ModuleId) && modules.Add(job.ModuleId) && job.ObjectId == player!.ObjectId &&
                (player.Modules ?? []).Any(m => m.ModuleId == job.ModuleId) && job.TargetObjectId is not null && targets.Add(job.TargetObjectId) &&
                manifests.TryGetValue(job.TargetObjectId, out var target) && !target.CompositionKnown &&
                job.StartedGameTimeMs >= 0 && job.DueGameTimeMs > job.StartedGameTimeMs && job.LastValidatedSimulationTimeMs >= 0,
                "Invalid survey job identity/reference/timestamps.");
        }
        return state with
        {
            StationResourceFields = fields with
            {
                Rules = rules,
                Asteroids = normalized.ToImmutableArray(),
                RngStreams = streams.OrderBy(s => s.Name, StringComparer.Ordinal).ToImmutableArray(),
                Surveys = fields.Surveys!.OrderBy(j => j.CommandId, StringComparer.Ordinal).ToImmutableArray()
            }
        };
    }
}
