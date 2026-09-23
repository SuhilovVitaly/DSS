using DeepSpaceSaga.Engine.Content;

namespace DeepSpaceSaga.Engine.Scenario;

internal sealed record TradingMapGeometryPlan(
    TradingMapStateData State,
    IReadOnlyList<SpaceObjectData> Stations,
    TradingMapRngData GeometryStream);

internal static class TradingMapGeometryGenerator
{
    private const string SafeRiskProfileId = "risk.safe";
    private const string ScientificMilitaryProfileId = "market.scientific-military";
    private const double WorldUnitToKm = 0.1;
    private const double GameMillisecondsPerSecond = 300.0;

    internal static TradingMapGeometryPlan Generate(
        TradingGraphPlan graph,
        IReadOnlyList<SpaceObjectData> existingObjects,
        ulong masterSeed)
    {
        ArgumentNullException.ThrowIfNull(graph);
        ArgumentNullException.ThrowIfNull(existingObjects);

        var rules = graph.Rules ?? throw new ScenarioException("tradingMap geometry requires graph rules.");
        var stationById = CanonicalizeStations(rules);
        var startStation = ResolveExistingStart(rules, stationById, existingObjects);
        var existingById = CanonicalizeExistingObjects(existingObjects);

        foreach (var station in stationById.Values)
        {
            if (!string.Equals(station.ObjectId, rules.StartStationObjectId, StringComparison.OrdinalIgnoreCase) &&
                existingById.ContainsKey(station.ObjectId))
            {
                throw new ScenarioException(
                    $"tradingMap geometry station '{station.ObjectId}' already exists in the input world.");
            }
        }

        var template = CanonicalizeTemplate(graph.Template, stationById, rules.RiskProfiles);
        var geometrySelection = TradingMapRandom.DrawIndex(masterSeed, "TradingMap.Geometry", 4);
        var stations = BuildStations(
            rules,
            template,
            stationById,
            startStation,
            geometrySelection.Value);

        ValidatePlacement(rules, stations, existingObjects, startStation.ObjectId);
        var edges = BuildEdges(rules, template, stations);
        ValidateEdgeCoverage(rules, edges);

        var state = new TradingMapStateData(
            SchemaVersion: 1,
            TemplateId: template.TemplateId,
            QuarterTurns: geometrySelection.Value,
            Rules: rules,
            Edges: edges,
            CargoFlows: graph.CargoFlows,
            RngStreams: [graph.TopologyStream, geometrySelection.State]);

        return new TradingMapGeometryPlan(state, stations, geometrySelection.State);
    }

    internal static void ValidateMaterialized(
        TradingMapStateData map,
        IReadOnlyList<SpaceObjectData> existingObjects,
        ulong masterSeed,
        GameDataRegistry registry)
    {
        ArgumentNullException.ThrowIfNull(map);
        ArgumentNullException.ThrowIfNull(existingObjects);
        ArgumentNullException.ThrowIfNull(registry);

        var rules = map.Rules ?? throw new ScenarioException("tradingMap saved state requires rules.");
        var graph = TradingGraphGenerator.Generate(rules, masterSeed, registry);
        if (!string.Equals(map.TemplateId, graph.Template.TemplateId, StringComparison.Ordinal))
            throw new ScenarioException(
                $"tradingMap saved template '{map.TemplateId}' does not match masterSeed-selected template '{graph.Template.TemplateId}'.");

        var geometrySelection = TradingMapRandom.DrawIndex(masterSeed, "TradingMap.Geometry", 4);
        if (map.QuarterTurns != geometrySelection.Value)
            throw new ScenarioException(
                $"tradingMap saved quarterTurns {map.QuarterTurns} does not match masterSeed-selected value {geometrySelection.Value}.");

        var expectedStreams = new[] { graph.TopologyStream, geometrySelection.State }
            .ToDictionary(stream => stream.Name, StringComparer.Ordinal);
        var actualStreams = map.RngStreams.ToDictionary(stream => stream.Name, StringComparer.Ordinal);
        if (actualStreams.Count != expectedStreams.Count || expectedStreams.Any(pair =>
                !actualStreams.TryGetValue(pair.Key, out var actual) ||
                actual.Seed != pair.Value.Seed || actual.Counter != pair.Value.Counter))
        {
            throw new ScenarioException("tradingMap saved RNG streams do not match the masterSeed replay.");
        }

        var stationById = rules.Stations.ToDictionary(station => station.ObjectId, StringComparer.OrdinalIgnoreCase);
        var template = graph.Template;
        var startStation = ResolveExistingStart(rules, stationById, existingObjects);
        var existingById = CanonicalizeExistingObjects(existingObjects);
        var expectedStations = BuildStations(rules, template, stationById, startStation, map.QuarterTurns);
        var actualStations = new List<SpaceObjectData>(stationById.Count);

        foreach (var stationRule in stationById.Values.OrderBy(station => station.ObjectId, StringComparer.Ordinal))
        {
            if (!existingById.TryGetValue(stationRule.ObjectId, out var actual))
                throw new ScenarioException($"tradingMap saved state is missing station '{stationRule.ObjectId}'.");
            if (!string.Equals(actual.ObjectType, "Station", StringComparison.OrdinalIgnoreCase))
                throw new ScenarioException($"tradingMap saved station '{actual.ObjectId}' must have objectType Station.");
            if (!actual.IsKnown || actual.SpeedMps != 0 ||
                !string.Equals(actual.MovementType, "Stationary", StringComparison.OrdinalIgnoreCase))
            {
                throw new ScenarioException(
                    $"tradingMap saved station '{actual.ObjectId}' must be known and stationary.");
            }

            var expected = expectedStations.Single(station =>
                string.Equals(station.ObjectId, actual.ObjectId, StringComparison.OrdinalIgnoreCase));
            if (!NearlyEqual(actual.PositionX, expected.PositionX) ||
                !NearlyEqual(actual.PositionY, expected.PositionY))
            {
                throw new ScenarioException(
                    $"tradingMap saved station '{actual.ObjectId}' coordinates do not match the selected template geometry.");
            }
            actualStations.Add(actual);
        }

        ValidatePlacement(rules, actualStations, existingObjects, startStation.ObjectId);
        var expectedEdges = BuildEdges(rules, template, actualStations);
        ValidateEdgeCoverage(rules, expectedEdges);
        var actualEdges = map.Edges.ToDictionary(
            edge => EndpointKey(edge.FromStationObjectId, edge.ToStationObjectId),
            StringComparer.Ordinal);
        if (actualEdges.Count != expectedEdges.Count)
            throw new ScenarioException("tradingMap saved state edge count does not match the selected template geometry.");

        foreach (var expectedEdge in expectedEdges)
        {
            var key = EndpointKey(expectedEdge.FromStationObjectId, expectedEdge.ToStationObjectId);
            if (!actualEdges.TryGetValue(key, out var actualEdge))
                throw new ScenarioException($"tradingMap saved state is missing edge '{key}'.");
            if (!NearlyEqual(actualEdge.DistanceKm, expectedEdge.DistanceKm) ||
                actualEdge.TravelEstimateGameTimeMs != expectedEdge.TravelEstimateGameTimeMs ||
                !string.Equals(actualEdge.DistanceClass, expectedEdge.DistanceClass, StringComparison.Ordinal) ||
                actualEdge.FuelMultiplierPermille != expectedEdge.FuelMultiplierPermille ||
                !string.Equals(actualEdge.RiskProfileId, expectedEdge.RiskProfileId, StringComparison.Ordinal))
            {
                throw new ScenarioException(
                    $"tradingMap saved edge '{expectedEdge.FromStationObjectId}'/'{expectedEdge.ToStationObjectId}' metadata does not match its coordinates and rules.");
            }
        }

        var expectedFlows = graph.CargoFlows.Select(FlowShape).OrderBy(flow => flow, StringComparer.Ordinal).ToArray();
        var actualFlows = map.CargoFlows.Select(FlowShape).OrderBy(flow => flow, StringComparer.Ordinal).ToArray();
        if (!expectedFlows.SequenceEqual(actualFlows, StringComparer.Ordinal))
            throw new ScenarioException("tradingMap saved cargo flows do not match the masterSeed-selected graph.");
    }

    private static IReadOnlyDictionary<string, TradingMapStationData> CanonicalizeStations(
        TradingMapGenerationData rules)
    {
        if (rules.Stations is null || rules.Stations.Count != 5)
            throw new ScenarioException("tradingMap geometry requires exactly five station rules.");
        if (string.IsNullOrWhiteSpace(rules.StartStationObjectId))
            throw new ScenarioException("tradingMap geometry requires startStationObjectId.");
        if (!double.IsFinite(rules.ReferenceSpeedMps) || rules.ReferenceSpeedMps <= 0)
            throw new ScenarioException("tradingMap geometry requires a finite positive referenceSpeedMps.");
        if (rules.ShortMaxGameTimeMs <= 0 || rules.MediumMaxGameTimeMs <= rules.ShortMaxGameTimeMs ||
            rules.MaxTravelGameTimeMs <= rules.MediumMaxGameTimeMs)
        {
            throw new ScenarioException("tradingMap geometry requires ordered positive travel time limits.");
        }

        var result = new Dictionary<string, TradingMapStationData>(StringComparer.OrdinalIgnoreCase);
        foreach (var station in rules.Stations)
        {
            if (station is null || string.IsNullOrWhiteSpace(station.ObjectId))
                throw new ScenarioException("tradingMap geometry contains a station with no objectId.");
            if (string.IsNullOrWhiteSpace(station.MarketProfileId) || string.IsNullOrWhiteSpace(station.Name) ||
                string.IsNullOrWhiteSpace(station.StationSize))
            {
                throw new ScenarioException($"tradingMap geometry station '{station.ObjectId}' has incomplete rules.");
            }
            if (!result.TryAdd(station.ObjectId, station))
                throw new ScenarioException($"tradingMap geometry has duplicate station '{station.ObjectId}'.");
        }

        if (!result.TryGetValue(rules.StartStationObjectId, out var start) ||
            !string.Equals(start.MarketProfileId, "market.transit", StringComparison.Ordinal))
        {
            throw new ScenarioException(
                $"tradingMap geometry start station '{rules.StartStationObjectId}' must be the market.transit station.");
        }

        var safeRisk = rules.RiskProfiles?.SingleOrDefault(risk =>
            risk is not null && string.Equals(risk.RiskProfileId, SafeRiskProfileId, StringComparison.Ordinal));
        if (safeRisk is null || safeRisk.FuelMultiplierPermille != 1000)
        {
            throw new ScenarioException("tradingMap geometry requires risk.safe with fuelMultiplierPermille 1000.");
        }

        return result
            .OrderBy(pair => pair.Key, StringComparer.Ordinal)
            .ToDictionary(pair => pair.Key, pair => pair.Value, StringComparer.OrdinalIgnoreCase);
    }

    private static IReadOnlyDictionary<string, SpaceObjectData> CanonicalizeExistingObjects(
        IReadOnlyList<SpaceObjectData> existingObjects)
    {
        var result = new Dictionary<string, SpaceObjectData>(StringComparer.OrdinalIgnoreCase);
        foreach (var obj in existingObjects)
        {
            if (obj is null || string.IsNullOrWhiteSpace(obj.ObjectId))
                throw new ScenarioException("tradingMap geometry contains an existing object with no objectId.");
            if (!double.IsFinite(obj.PositionX) || !double.IsFinite(obj.PositionY))
                throw new ScenarioException($"tradingMap geometry existing object '{obj.ObjectId}' has non-finite coordinates.");
            if (!result.TryAdd(obj.ObjectId, obj))
                throw new ScenarioException($"tradingMap geometry has duplicate existing object '{obj.ObjectId}'.");
        }

        return result;
    }

    private static SpaceObjectData ResolveExistingStart(
        TradingMapGenerationData rules,
        IReadOnlyDictionary<string, TradingMapStationData> stationById,
        IReadOnlyList<SpaceObjectData> existingObjects)
    {
        var start = existingObjects.FirstOrDefault(obj =>
            obj is not null && string.Equals(obj.ObjectId, rules.StartStationObjectId, StringComparison.Ordinal));
        if (start is null)
            throw new ScenarioException(
                $"tradingMap geometry start station '{rules.StartStationObjectId}' is missing from existingObjects.");
        if (!string.Equals(start.ObjectType, "Station", StringComparison.OrdinalIgnoreCase))
            throw new ScenarioException(
                $"tradingMap geometry start object '{start.ObjectId}' must have objectType Station.");
        if (!start.IsKnown || start.SpeedMps != 0 ||
            !string.Equals(start.MovementType, "Stationary", StringComparison.OrdinalIgnoreCase))
        {
            throw new ScenarioException(
                $"tradingMap geometry start station '{start.ObjectId}' must be known and stationary.");
        }
        if (!stationById.ContainsKey(start.ObjectId))
            throw new ScenarioException($"tradingMap geometry has no rule for start station '{start.ObjectId}'.");
        return start;
    }

    private static TradingMapTemplateData CanonicalizeTemplate(
        TradingMapTemplateData template,
        IReadOnlyDictionary<string, TradingMapStationData> stationById,
        IReadOnlyList<TradingMapRiskData> risks)
    {
        if (template is null || string.IsNullOrWhiteSpace(template.TemplateId))
            throw new ScenarioException("tradingMap geometry requires a named template.");
        if (template.Links is null || template.Offsets is null)
            throw new ScenarioException($"template '{template.TemplateId}' geometry collections must not be null.");

        var riskById = risks.ToDictionary(risk => risk.RiskProfileId, StringComparer.OrdinalIgnoreCase);
        var links = new List<TradingMapLinkData>(template.Links.Count);
        var edgeKeys = new HashSet<string>(StringComparer.Ordinal);
        foreach (var link in template.Links)
        {
            if (link is null || !stationById.TryGetValue(link.FromStationObjectId ?? string.Empty, out var from) ||
                !stationById.TryGetValue(link.ToStationObjectId ?? string.Empty, out var to))
            {
                throw new ScenarioException($"template '{template.TemplateId}' contains an unknown geometry edge endpoint.");
            }
            if (string.Equals(from.ObjectId, to.ObjectId, StringComparison.OrdinalIgnoreCase))
                throw new ScenarioException($"template '{template.TemplateId}' contains a self-loop.");
            if (!riskById.TryGetValue(link.RiskProfileId ?? string.Empty, out var risk))
                throw new ScenarioException(
                    $"template '{template.TemplateId}' references unknown riskProfileId '{link.RiskProfileId}'.");

            var (left, right) = OrderEndpoints(from.ObjectId, to.ObjectId);
            if (!edgeKeys.Add(EndpointKey(left, right)))
                throw new ScenarioException($"template '{template.TemplateId}' duplicates edge '{left}'/'{right}'.");
            links.Add(new TradingMapLinkData(left, right, risk.RiskProfileId));
        }

        var offsets = new List<TradingMapOffsetData>(template.Offsets.Count);
        var offsetIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var offset in template.Offsets)
        {
            if (offset is null || !stationById.TryGetValue(offset.StationObjectId ?? string.Empty, out var station))
                throw new ScenarioException($"template '{template.TemplateId}' contains an unknown geometry offset station.");
            if (!double.IsFinite(offset.X) || !double.IsFinite(offset.Y))
                throw new ScenarioException(
                    $"template '{template.TemplateId}' offset for station '{station.ObjectId}' is not finite.");
            if (!offsetIds.Add(station.ObjectId))
                throw new ScenarioException($"template '{template.TemplateId}' repeats offset for '{station.ObjectId}'.");
            offsets.Add(new TradingMapOffsetData(station.ObjectId, offset.X, offset.Y));
        }

        if (!offsetIds.SetEquals(stationById.Keys))
            throw new ScenarioException($"template '{template.TemplateId}' geometry offsets must cover all stations.");

        return template with
        {
            Links = links.OrderBy(link => link.FromStationObjectId, StringComparer.Ordinal)
                .ThenBy(link => link.ToStationObjectId, StringComparer.Ordinal).ToArray(),
            Offsets = offsets.OrderBy(offset => offset.StationObjectId, StringComparer.Ordinal).ToArray(),
        };
    }

    private static IReadOnlyList<SpaceObjectData> BuildStations(
        TradingMapGenerationData rules,
        TradingMapTemplateData template,
        IReadOnlyDictionary<string, TradingMapStationData> stationById,
        SpaceObjectData startStation,
        int quarterTurns)
    {
        var offsets = template.Offsets.ToDictionary(offset => offset.StationObjectId, StringComparer.OrdinalIgnoreCase);
        var startOffset = offsets[rules.StartStationObjectId];
        var stations = new List<SpaceObjectData>(stationById.Count);

        foreach (var station in stationById.Values.OrderBy(station => station.ObjectId, StringComparer.Ordinal))
        {
            if (string.Equals(station.ObjectId, startStation.ObjectId, StringComparison.Ordinal))
            {
                stations.Add(startStation);
                continue;
            }

            var offset = offsets[station.ObjectId];
            var relativeX = offset.X - startOffset.X;
            var relativeY = offset.Y - startOffset.Y;
            var (rotatedX, rotatedY) = Rotate(relativeX, relativeY, quarterTurns);
            var x = startStation.PositionX + rotatedX;
            var y = startStation.PositionY + rotatedY;
            if (!double.IsFinite(x) || !double.IsFinite(y))
                throw new ScenarioException(
                    $"template '{template.TemplateId}' produced non-finite coordinates for station '{station.ObjectId}'.");

            stations.Add(new SpaceObjectData(
                ObjectId: station.ObjectId,
                ObjectType: "Station",
                PersistenceType: "Permanent",
                Name: station.Name,
                PositionX: x,
                PositionY: y,
                SpeedMps: 0,
                DirectionDegrees: 0,
                MovementType: "Stationary",
                MassKg: null,
                CompositionType: null,
                Modules: null,
                IsKnown: true,
                MarketProfileId: station.MarketProfileId,
                StationSize: station.StationSize));
        }

        return stations;
    }

    private static void ValidatePlacement(
        TradingMapGenerationData rules,
        IReadOnlyList<SpaceObjectData> stations,
        IReadOnlyList<SpaceObjectData> existingObjects,
        string startStationId)
    {
        var stationIds = stations.Select(station => station.ObjectId).ToHashSet(StringComparer.OrdinalIgnoreCase);
        for (var i = 0; i < stations.Count; i++)
        {
            for (var j = i + 1; j < stations.Count; j++)
            {
                var distanceKm = DistanceKm(stations[i].PositionX, stations[i].PositionY,
                    stations[j].PositionX, stations[j].PositionY);
                if (distanceKm < rules.MinStationDistanceKm)
                    throw new ScenarioException(
                        $"tradingMap geometry stations '{stations[i].ObjectId}'/'{stations[j].ObjectId}' are " +
                        $"{distanceKm:R} km apart, below minStationDistanceKm {rules.MinStationDistanceKm:R}.");
            }
        }

        foreach (var station in stations)
        {
            foreach (var obj in existingObjects)
            {
                if (obj is null || string.Equals(obj.ObjectId, station.ObjectId, StringComparison.OrdinalIgnoreCase))
                    continue;
                if (string.Equals(obj.ObjectType, "PlayerShip", StringComparison.OrdinalIgnoreCase) &&
                    string.Equals(station.ObjectId, startStationId, StringComparison.OrdinalIgnoreCase))
                    continue;

                var isStation = string.Equals(obj.ObjectType, "Station", StringComparison.OrdinalIgnoreCase);
                var isAsteroid = string.Equals(obj.ObjectType, "Asteroid", StringComparison.OrdinalIgnoreCase);
                var isPlayerShip = string.Equals(obj.ObjectType, "PlayerShip", StringComparison.OrdinalIgnoreCase);
                if (!isStation && !isAsteroid && !isPlayerShip)
                    continue;

                // The start station is already represented in stations. Do not reject its
                // identity pair, but do validate every newly materialized station against it.
                if (stationIds.Contains(obj.ObjectId) &&
                    string.Equals(station.ObjectId, obj.ObjectId, StringComparison.OrdinalIgnoreCase))
                    continue;

                var distanceKm = DistanceKm(station.PositionX, station.PositionY, obj.PositionX, obj.PositionY);
                var requiredKm = isStation ? rules.MinStationDistanceKm : rules.ClearanceKm;
                if (distanceKm < requiredKm)
                {
                    throw new ScenarioException(
                        $"tradingMap geometry station '{station.ObjectId}' is {distanceKm:R} km from " +
                        $"existing {obj.ObjectType} '{obj.ObjectId}', below required {requiredKm:R} km.");
                }
            }
        }
    }

    private static IReadOnlyList<TradingMapEdgeData> BuildEdges(
        TradingMapGenerationData rules,
        TradingMapTemplateData template,
        IReadOnlyList<SpaceObjectData> stations)
    {
        var stationById = stations.ToDictionary(station => station.ObjectId, StringComparer.OrdinalIgnoreCase);
        var riskById = rules.RiskProfiles.ToDictionary(risk => risk.RiskProfileId, StringComparer.OrdinalIgnoreCase);
        var edges = new List<TradingMapEdgeData>(template.Links.Count);
        foreach (var link in template.Links)
        {
            var from = stationById[link.FromStationObjectId];
            var to = stationById[link.ToStationObjectId];
            var distanceKm = DistanceKm(from.PositionX, from.PositionY, to.PositionX, to.PositionY);
            if (!double.IsFinite(distanceKm) || distanceKm <= 0)
                throw new ScenarioException(
                    $"template '{template.TemplateId}' edge '{from.ObjectId}'/'{to.ObjectId}' has invalid distance.");

            var travelEstimate = CalculateTravelEstimate(rules, template.TemplateId, from.ObjectId, to.ObjectId, distanceKm);
            var distanceClass = travelEstimate <= rules.ShortMaxGameTimeMs
                ? "Short"
                : travelEstimate <= rules.MediumMaxGameTimeMs
                    ? "Medium"
                    : "Long";
            if (!riskById.TryGetValue(link.RiskProfileId, out var risk))
                throw new ScenarioException(
                    $"template '{template.TemplateId}' edge '{from.ObjectId}'/'{to.ObjectId}' has unknown risk profile.");

            edges.Add(new TradingMapEdgeData(
                from.ObjectId,
                to.ObjectId,
                distanceKm,
                travelEstimate,
                distanceClass,
                risk.FuelMultiplierPermille,
                risk.RiskProfileId));
        }

        return edges
            .OrderBy(edge => edge.FromStationObjectId, StringComparer.Ordinal)
            .ThenBy(edge => edge.ToStationObjectId, StringComparer.Ordinal)
            .ToArray();
    }

    private static long CalculateTravelEstimate(
        TradingMapGenerationData rules,
        string templateId,
        string fromStationId,
        string toStationId,
        double distanceKm)
    {
        var referenceSpeedKmPerSecond = rules.ReferenceSpeedMps / 1000.0;
        var gameMilliseconds = distanceKm / referenceSpeedKmPerSecond * 1000.0 * GameMillisecondsPerSecond;
        if (!double.IsFinite(gameMilliseconds) || gameMilliseconds <= 0 || gameMilliseconds > long.MaxValue)
            throw new ScenarioException(
                $"template '{templateId}' edge '{fromStationId}'/'{toStationId}' has an unrepresentable travel estimate.");

        var estimate = checked((long)Math.Ceiling(gameMilliseconds));
        if (estimate > rules.MaxTravelGameTimeMs)
            throw new ScenarioException(
                $"template '{templateId}' edge '{fromStationId}'/'{toStationId}' travel estimate {estimate} " +
                $"exceeds maxTravelGameTimeMs {rules.MaxTravelGameTimeMs}.");
        return estimate;
    }

    private static void ValidateEdgeCoverage(
        TradingMapGenerationData rules,
        IReadOnlyList<TradingMapEdgeData> edges)
    {
        var classes = edges.Select(edge => edge.DistanceClass).ToHashSet(StringComparer.Ordinal);
        foreach (var distanceClass in new[] { "Short", "Medium", "Long" })
        {
            if (!classes.Contains(distanceClass))
                throw new ScenarioException($"tradingMap geometry must contain a {distanceClass} edge.");
        }

        var startId = rules.StartStationObjectId;
        if (!edges.Any(edge =>
                (string.Equals(edge.FromStationObjectId, startId, StringComparison.OrdinalIgnoreCase) ||
                 string.Equals(edge.ToStationObjectId, startId, StringComparison.OrdinalIgnoreCase)) &&
                edge.DistanceClass == "Short" &&
                string.Equals(edge.RiskProfileId, SafeRiskProfileId, StringComparison.Ordinal)))
        {
            throw new ScenarioException("tradingMap geometry requires a safe Short edge from the start station.");
        }

        var scientificIds = rules.Stations
            .Where(station => string.Equals(station.MarketProfileId, ScientificMilitaryProfileId, StringComparison.Ordinal))
            .Select(station => station.ObjectId)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        if (!edges.Any(edge => edge.DistanceClass == "Long" &&
                (scientificIds.Contains(edge.FromStationObjectId) || scientificIds.Contains(edge.ToStationObjectId))))
        {
            throw new ScenarioException("tradingMap geometry requires a Long edge with a scientific/military endpoint.");
        }
    }

    private static double DistanceKm(double x1, double y1, double x2, double y2)
    {
        var dx = x2 - x1;
        var dy = y2 - y1;
        return Math.Sqrt(dx * dx + dy * dy) * WorldUnitToKm;
    }

    private static bool NearlyEqual(double left, double right)
    {
        var tolerance = 1e-9 * Math.Max(1, Math.Max(Math.Abs(left), Math.Abs(right)));
        return Math.Abs(left - right) <= tolerance;
    }

    private static string FlowShape(TradingMapCargoFlowData flow) =>
        $"{flow.FromStationObjectId}>{flow.ToStationObjectId}:{string.Join(',', flow.ItemTypeIds)}";

    private static (double X, double Y) Rotate(double x, double y, int quarterTurns) => quarterTurns switch
    {
        0 => (x, y),
        1 => (-y, x),
        2 => (-x, -y),
        3 => (y, -x),
        _ => throw new ArgumentOutOfRangeException(nameof(quarterTurns)),
    };

    private static (string From, string To) OrderEndpoints(string from, string to) =>
        string.CompareOrdinal(from, to) <= 0 ? (from, to) : (to, from);

    private static string EndpointKey(string from, string to) => $"{from}\u001f{to}";
}
