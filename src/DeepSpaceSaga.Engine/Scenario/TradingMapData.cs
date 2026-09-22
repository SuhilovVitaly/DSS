using System.Diagnostics;
using System.Text.Json.Serialization;

namespace DeepSpaceSaga.Engine.Scenario;

public sealed record TradingMapGenerationData(
    [property: JsonPropertyName("schemaVersion")] int SchemaVersion,
    [property: JsonPropertyName("startStationObjectId")] string StartStationObjectId,
    [property: JsonPropertyName("referenceSpeedMps")] double ReferenceSpeedMps,
    [property: JsonPropertyName("shortMaxGameTimeMs")] long ShortMaxGameTimeMs,
    [property: JsonPropertyName("mediumMaxGameTimeMs")] long MediumMaxGameTimeMs,
    [property: JsonPropertyName("maxTravelGameTimeMs")] long MaxTravelGameTimeMs,
    [property: JsonPropertyName("minStationDistanceKm")] double MinStationDistanceKm,
    [property: JsonPropertyName("clearanceKm")] double ClearanceKm,
    [property: JsonPropertyName("stations")] IReadOnlyList<TradingMapStationData> Stations,
    [property: JsonPropertyName("templates")] IReadOnlyList<TradingMapTemplateData> Templates,
    [property: JsonPropertyName("riskProfiles")] IReadOnlyList<TradingMapRiskData> RiskProfiles);

public sealed record TradingMapStationData(
    [property: JsonPropertyName("objectId")] string ObjectId,
    [property: JsonPropertyName("marketProfileId")] string MarketProfileId,
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("stationSize")] string StationSize);

public sealed record TradingMapTemplateData(
    [property: JsonPropertyName("templateId")] string TemplateId,
    [property: JsonPropertyName("links")] IReadOnlyList<TradingMapLinkData> Links,
    [property: JsonPropertyName("offsets")] IReadOnlyList<TradingMapOffsetData> Offsets);

public sealed record TradingMapLinkData(
    [property: JsonPropertyName("fromStationObjectId")] string FromStationObjectId,
    [property: JsonPropertyName("toStationObjectId")] string ToStationObjectId,
    [property: JsonPropertyName("riskProfileId")] string RiskProfileId);

public sealed record TradingMapOffsetData(
    [property: JsonPropertyName("stationObjectId")] string StationObjectId,
    [property: JsonPropertyName("x")] double X,
    [property: JsonPropertyName("y")] double Y);

public sealed record TradingMapRiskData(
    [property: JsonPropertyName("riskProfileId")] string RiskProfileId,
    [property: JsonPropertyName("fuelMultiplierPermille")] int FuelMultiplierPermille);

public sealed record TradingMapRngData(
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("seed")] ulong Seed,
    [property: JsonPropertyName("counter")] ulong Counter);

public sealed record TradingMapCargoFlowData(
    [property: JsonPropertyName("fromStationObjectId")] string FromStationObjectId,
    [property: JsonPropertyName("toStationObjectId")] string ToStationObjectId,
    [property: JsonPropertyName("itemTypeIds")] IReadOnlyList<string> ItemTypeIds);

public sealed record TradingMapEdgeData(
    [property: JsonPropertyName("fromStationObjectId")] string FromStationObjectId,
    [property: JsonPropertyName("toStationObjectId")] string ToStationObjectId,
    [property: JsonPropertyName("distanceKm")] double DistanceKm,
    [property: JsonPropertyName("travelEstimateGameTimeMs")] long TravelEstimateGameTimeMs,
    [property: JsonPropertyName("distanceClass")] string DistanceClass,
    [property: JsonPropertyName("fuelMultiplierPermille")] int FuelMultiplierPermille,
    [property: JsonPropertyName("riskProfileId")] string RiskProfileId);

public sealed record TradingMapStateData(
    [property: JsonPropertyName("schemaVersion")] int SchemaVersion,
    [property: JsonPropertyName("templateId")] string TemplateId,
    [property: JsonPropertyName("quarterTurns")] int QuarterTurns,
    [property: JsonPropertyName("rules")] TradingMapGenerationData Rules,
    [property: JsonPropertyName("edges")] IReadOnlyList<TradingMapEdgeData> Edges,
    [property: JsonPropertyName("cargoFlows")] IReadOnlyList<TradingMapCargoFlowData> CargoFlows,
    [property: JsonPropertyName("rngStreams")] IReadOnlyList<TradingMapRngData> RngStreams);

internal static class TradingMapDataValidation
{
    private static readonly string[] RequiredProfiles =
    [
        "market.transit",
        "market.mining",
        "market.industrial",
        "market.hydroponic",
        "market.scientific-military",
    ];

    private static readonly string[] RequiredRngStreams =
    ["TradingMap.Topology", "TradingMap.Geometry"];

    internal static GameStateData ValidateAndNormalize(GameStateData state, int saveFormatVersion)
    {
        if (state.TradingMapGeneration is null && state.TradingMap is null)
            return state;
        if (state.TradingMapGeneration is not null && state.TradingMap is not null)
            throw new ScenarioException("tradingMapGeneration and tradingMap are mutually exclusive.");

        if (state.TradingMapGeneration is { } request)
        {
            if (saveFormatVersion != 0)
                throw new ScenarioException("tradingMapGeneration is allowed only when saveFormatVersion is 0.");
            if (state.GameTimeMs != 0)
                throw new ScenarioException("tradingMapGeneration is allowed only when gameTimeMs is 0.");

            return state with
            {
                TradingMapGeneration = ValidateRules(request, state.SpaceObjects, isResult: false),
            };
        }

        var map = state.TradingMap!;
        if (state.MasterSeed is null)
            throw new ScenarioException("tradingMap requires gameState.masterSeed.");
        if (map.SchemaVersion != 1)
            throw new ScenarioException($"tradingMap.schemaVersion must be 1, got {map.SchemaVersion}.");

        var rules = ValidateRules(map.Rules, state.SpaceObjects, isResult: true);
        var stations = rules.Stations.ToDictionary(s => s.ObjectId, StringComparer.OrdinalIgnoreCase);
        var templates = rules.Templates.ToDictionary(t => t.TemplateId, StringComparer.OrdinalIgnoreCase);
        var risks = rules.RiskProfiles.ToDictionary(r => r.RiskProfileId, r => r.RiskProfileId, StringComparer.OrdinalIgnoreCase);
        if (map.TemplateId is null || string.IsNullOrWhiteSpace(map.TemplateId))
            throw new ScenarioException("tradingMap.templateId is required.");
        if (!templates.TryGetValue(map.TemplateId, out var selectedTemplate))
            throw new ScenarioException($"tradingMap.templateId '{map.TemplateId}' is not declared in tradingMap.rules.templates.");
        if (map.QuarterTurns is < 0 or > 3)
            throw new ScenarioException($"tradingMap.quarterTurns must be between 0 and 3, got {map.QuarterTurns}.");

        var edges = RequireCollection(map.Edges, "tradingMap.edges");
        var normalizedEdges = new List<TradingMapEdgeData>(edges.Count);
        var edgePairs = new HashSet<string>(StringComparer.Ordinal);
        for (var i = 0; i < edges.Count; i++)
        {
            var edge = edges[i] ?? throw new ScenarioException($"tradingMap.edges[{i}] is null.");
            var from = ResolveStation(edge.FromStationObjectId, stations, $"tradingMap.edges[{i}].fromStationObjectId");
            var to = ResolveStation(edge.ToStationObjectId, stations, $"tradingMap.edges[{i}].toStationObjectId");
            if (string.Equals(from, to, StringComparison.OrdinalIgnoreCase))
                throw new ScenarioException($"tradingMap.edges[{i}] '{from}' must not be a self-loop.");
            (from, to) = OrderEndpoints(from, to);
            if (!edgePairs.Add(EndpointKey(from, to)))
                throw new ScenarioException($"tradingMap.edges[{i}] duplicates edge '{from}'/'{to}'.");
            if (!double.IsFinite(edge.DistanceKm) || edge.DistanceKm <= 0)
                throw new ScenarioException($"tradingMap.edges[{i}].distanceKm must be finite and positive.");
            if (edge.TravelEstimateGameTimeMs <= 0)
                throw new ScenarioException($"tradingMap.edges[{i}].travelEstimateGameTimeMs must be positive.");
            if (edge.DistanceClass is not ("Short" or "Medium" or "Long"))
                throw new ScenarioException($"tradingMap.edges[{i}].distanceClass must be Short, Medium, or Long.");
            if (edge.FuelMultiplierPermille <= 0)
                throw new ScenarioException($"tradingMap.edges[{i}].fuelMultiplierPermille must be positive.");
            var risk = ResolveRisk(edge.RiskProfileId, risks, $"tradingMap.edges[{i}].riskProfileId");
            var riskProfile = rules.RiskProfiles.Single(profile => profile.RiskProfileId == risk);
            if (edge.FuelMultiplierPermille != riskProfile.FuelMultiplierPermille)
                throw new ScenarioException(
                    $"tradingMap.edges[{i}].fuelMultiplierPermille must equal riskProfile '{risk}'.fuelMultiplierPermille " +
                    $"({riskProfile.FuelMultiplierPermille}), got {edge.FuelMultiplierPermille}.");
            normalizedEdges.Add(edge with
            {
                FromStationObjectId = from,
                ToStationObjectId = to,
                RiskProfileId = risk,
            });
        }

        var expectedEdges = selectedTemplate.Links
            .Select(link => (Key: EndpointKey(link.FromStationObjectId, link.ToStationObjectId), link.RiskProfileId))
            .ToHashSet();
        var actualEdges = normalizedEdges
            .Select(edge => (Key: EndpointKey(edge.FromStationObjectId, edge.ToStationObjectId), edge.RiskProfileId))
            .ToHashSet();
        if (!expectedEdges.SetEquals(actualEdges))
            throw new ScenarioException($"tradingMap.edges must match template '{selectedTemplate.TemplateId}' endpoints and riskProfileId values.");

        var flows = RequireCollection(map.CargoFlows, "tradingMap.cargoFlows");
        var normalizedFlows = new List<TradingMapCargoFlowData>(flows.Count);
        var flowPairs = new HashSet<string>(StringComparer.Ordinal);
        for (var i = 0; i < flows.Count; i++)
        {
            var flow = flows[i] ?? throw new ScenarioException($"tradingMap.cargoFlows[{i}] is null.");
            var from = ResolveStation(flow.FromStationObjectId, stations, $"tradingMap.cargoFlows[{i}].fromStationObjectId");
            var to = ResolveStation(flow.ToStationObjectId, stations, $"tradingMap.cargoFlows[{i}].toStationObjectId");
            if (string.Equals(from, to, StringComparison.OrdinalIgnoreCase))
                throw new ScenarioException($"tradingMap.cargoFlows[{i}] must have different endpoints.");
            if (!edgePairs.Contains(EndpointKey(from, to)))
                throw new ScenarioException($"tradingMap.cargoFlows[{i}] references a pair without a tradingMap edge.");
            var items = RequireCollection(flow.ItemTypeIds, $"tradingMap.cargoFlows[{i}].itemTypeIds");
            var itemIds = new HashSet<string>(StringComparer.Ordinal);
            foreach (var item in items)
            {
                if (string.IsNullOrWhiteSpace(item))
                    throw new ScenarioException($"tradingMap.cargoFlows[{i}].itemTypeIds contains a blank itemTypeId.");
                if (!itemIds.Add(item))
                    throw new ScenarioException($"tradingMap.cargoFlows[{i}] repeats itemTypeId '{item}'.");
            }
            if (!flowPairs.Add($"{from}\u001f{to}"))
                throw new ScenarioException($"tradingMap.cargoFlows[{i}] repeats directed pair '{from}'/'{to}'.");
            normalizedFlows.Add(flow with
            {
                FromStationObjectId = from,
                ToStationObjectId = to,
                ItemTypeIds = items.OrderBy(item => item, StringComparer.Ordinal).ToArray(),
            });
        }

        var streams = RequireCollection(map.RngStreams, "tradingMap.rngStreams");
        if (streams.Count != RequiredRngStreams.Length)
            throw new ScenarioException("tradingMap.rngStreams must contain exactly TradingMap.Topology and TradingMap.Geometry.");
        var streamNames = new HashSet<string>(StringComparer.Ordinal);
        var normalizedStreams = new List<TradingMapRngData>(streams.Count);
        foreach (var stream in streams)
        {
            if (stream is null)
                throw new ScenarioException("tradingMap.rngStreams contains a null element.");
            var streamName = stream.Name ?? string.Empty;
            if (!streamNames.Add(streamName))
                throw new ScenarioException($"tradingMap.rngStreams contains duplicate name '{stream.Name}'.");
            if (!RequiredRngStreams.Contains(streamName, StringComparer.Ordinal))
                throw new ScenarioException($"tradingMap.rngStreams contains unknown name '{stream.Name}'.");
            var counter = NormalizeCounter(streamName, stream.Counter);
            normalizedStreams.Add(stream with { Counter = counter });
        }
        if (!RequiredRngStreams.All(streamNames.Contains))
            throw new ScenarioException("tradingMap.rngStreams is missing a required stream.");

        return state with
        {
            TradingMap = map with
            {
                TemplateId = selectedTemplate.TemplateId,
                Rules = rules,
                Edges = normalizedEdges
                    .OrderBy(edge => edge.FromStationObjectId, StringComparer.Ordinal)
                    .ThenBy(edge => edge.ToStationObjectId, StringComparer.Ordinal)
                    .ToArray(),
                CargoFlows = normalizedFlows
                    .OrderBy(flow => flow.FromStationObjectId, StringComparer.Ordinal)
                    .ThenBy(flow => flow.ToStationObjectId, StringComparer.Ordinal)
                    .ToArray(),
                RngStreams = normalizedStreams.OrderBy(stream => stream.Name, StringComparer.Ordinal).ToArray(),
            },
        };
    }

    private static TradingMapGenerationData ValidateRules(
        TradingMapGenerationData rules,
        IReadOnlyList<SpaceObjectData> spaceObjects,
        bool isResult)
    {
        if (rules is null)
            throw new ScenarioException("tradingMap.rules is required.");
        if (rules.SchemaVersion != 1)
            throw new ScenarioException($"tradingMap.rules.schemaVersion must be 1, got {rules.SchemaVersion}.");
        if (string.IsNullOrWhiteSpace(rules.StartStationObjectId))
            throw new ScenarioException("tradingMap.rules.startStationObjectId is required.");
        if (!double.IsFinite(rules.ReferenceSpeedMps) || rules.ReferenceSpeedMps <= 0)
            throw new ScenarioException("tradingMap.rules.referenceSpeedMps must be finite and positive.");
        if (rules.ShortMaxGameTimeMs <= 0 || rules.MediumMaxGameTimeMs <= rules.ShortMaxGameTimeMs ||
            rules.MaxTravelGameTimeMs <= rules.MediumMaxGameTimeMs)
            throw new ScenarioException("tradingMap.rules travel time limits must satisfy 0 < short < medium < max.");
        if (!double.IsFinite(rules.MinStationDistanceKm) || rules.MinStationDistanceKm <= 0 ||
            !double.IsFinite(rules.ClearanceKm) || rules.ClearanceKm <= 0 ||
            rules.ClearanceKm >= rules.MinStationDistanceKm)
            throw new ScenarioException("tradingMap.rules clearance and minimum distance must be finite and satisfy 0 < clearance < minStationDistanceKm.");

        var stations = RequireCollection(rules.Stations, "tradingMap.rules.stations");
        if (stations.Count != 5)
            throw new ScenarioException($"tradingMap.rules.stations must contain exactly five stations, got {stations.Count}.");
        var stationIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var normalizedStations = new List<TradingMapStationData>(stations.Count);
        foreach (var station in stations)
        {
            if (station is null)
                throw new ScenarioException("tradingMap.rules.stations contains a null element.");
            Required(station.ObjectId, "tradingMap.rules.stations.objectId");
            Required(station.MarketProfileId, $"tradingMap.rules.stations['{station.ObjectId}'].marketProfileId");
            Required(station.Name, $"tradingMap.rules.stations['{station.ObjectId}'].name");
            if (!stationIds.Add(station.ObjectId))
                throw new ScenarioException($"tradingMap.rules.stations has duplicate objectId '{station.ObjectId}'.");
            if (!RequiredProfiles.Contains(station.MarketProfileId, StringComparer.Ordinal))
                throw new ScenarioException($"tradingMap.rules.stations['{station.ObjectId}'] has unknown marketProfileId '{station.MarketProfileId}'.");
            normalizedStations.Add(station with { StationSize = NormalizeStationSize(station.StationSize, station.ObjectId) });
        }
        if (normalizedStations.Select(s => s.MarketProfileId).Distinct(StringComparer.Ordinal).Count() != 5)
            throw new ScenarioException("tradingMap.rules.stations must contain one station for each required marketProfileId.");

        var stationMap = normalizedStations.ToDictionary(s => s.ObjectId, StringComparer.OrdinalIgnoreCase);
        var startId = ResolveStation(rules.StartStationObjectId, stationMap, "tradingMap.rules.startStationObjectId");
        var transit = normalizedStations.Single(s => s.MarketProfileId == "market.transit");
        if (!string.Equals(startId, transit.ObjectId, StringComparison.OrdinalIgnoreCase))
            throw new ScenarioException($"tradingMap.rules.startStationObjectId must reference the market.transit station, got '{startId}'.");
        if (!string.Equals(transit.StationSize, "Large", StringComparison.Ordinal))
            throw new ScenarioException($"tradingMap.rules.stations['{transit.ObjectId}'].stationSize must be Large.");

        var objectMap = spaceObjects.ToDictionary(o => o.ObjectId, StringComparer.OrdinalIgnoreCase);
        foreach (var station in normalizedStations)
        {
            if (!objectMap.TryGetValue(station.ObjectId, out var obj))
            {
                if (isResult)
                    throw new ScenarioException($"tradingMap.rules.stations['{station.ObjectId}'] is missing from spaceObjects.");
                if (string.Equals(station.ObjectId, transit.ObjectId, StringComparison.OrdinalIgnoreCase))
                    throw new ScenarioException($"tradingMap.rules.stations['{station.ObjectId}'] start station is missing from spaceObjects.");
                continue;
            }
            if (!string.Equals(obj.ObjectType, "Station", StringComparison.OrdinalIgnoreCase))
                throw new ScenarioException($"tradingMap.rules.stations['{station.ObjectId}'] references objectType '{obj.ObjectType}', expected Station.");
            if (!string.Equals(obj.ObjectId, station.ObjectId, StringComparison.Ordinal))
                throw new ScenarioException(
                    $"tradingMap.rules.stations['{station.ObjectId}'].objectId must match spaceObjects objectId '{obj.ObjectId}' exactly, including case.");
            if (!string.Equals(obj.MarketProfileId, station.MarketProfileId, StringComparison.Ordinal))
                throw new ScenarioException($"spaceObjects['{obj.ObjectId}'].marketProfileId does not match tradingMap.rules station profile.");
            if (!string.Equals(NormalizeStationSize(obj.StationSize, obj.ObjectId), station.StationSize, StringComparison.Ordinal))
                throw new ScenarioException($"spaceObjects['{obj.ObjectId}'].stationSize does not match tradingMap.rules station size.");
        }
        if (!objectMap.TryGetValue(transit.ObjectId, out var startObject) ||
            !string.Equals(startObject.ObjectType, "Station", StringComparison.OrdinalIgnoreCase))
            throw new ScenarioException($"tradingMap.rules.startStationObjectId '{transit.ObjectId}' must reference an existing Station.");

        var risks = RequireCollection(rules.RiskProfiles, "tradingMap.rules.riskProfiles");
        var riskIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var riskMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var normalizedRisks = new List<TradingMapRiskData>(risks.Count);
        foreach (var risk in risks)
        {
            if (risk is null)
                throw new ScenarioException("tradingMap.rules.riskProfiles contains a null element.");
            Required(risk.RiskProfileId, "tradingMap.rules.riskProfiles.riskProfileId");
            if (!riskIds.Add(risk.RiskProfileId))
                throw new ScenarioException($"tradingMap.rules.riskProfiles has duplicate riskProfileId '{risk.RiskProfileId}'.");
            if (risk.FuelMultiplierPermille <= 0)
                throw new ScenarioException($"tradingMap.rules.riskProfiles['{risk.RiskProfileId}'].fuelMultiplierPermille must be positive.");
            riskMap.Add(risk.RiskProfileId, risk.RiskProfileId);
            normalizedRisks.Add(risk);
        }

        var templates = RequireCollection(rules.Templates, "tradingMap.rules.templates");
        var templateIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var normalizedTemplates = new List<TradingMapTemplateData>(templates.Count);
        foreach (var template in templates)
        {
            if (template is null)
                throw new ScenarioException("tradingMap.rules.templates contains a null element.");
            Required(template.TemplateId, "tradingMap.rules.templates.templateId");
            if (!templateIds.Add(template.TemplateId))
                throw new ScenarioException($"tradingMap.rules.templates has duplicate templateId '{template.TemplateId}'.");
            var offsets = RequireCollection(template.Offsets, $"tradingMap.rules.templates['{template.TemplateId}'].offsets");
            if (offsets.Count != 5)
                throw new ScenarioException($"tradingMap.rules.templates['{template.TemplateId}'].offsets must contain exactly five entries.");
            var offsetIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var normalizedOffsets = new List<TradingMapOffsetData>(offsets.Count);
            foreach (var offset in offsets)
            {
                if (offset is null)
                    throw new ScenarioException($"tradingMap.rules.templates['{template.TemplateId}'].offsets contains a null element.");
                var stationId = ResolveStation(offset.StationObjectId, stationMap,
                    $"tradingMap.rules.templates['{template.TemplateId}'].offsets.stationObjectId");
                if (!offsetIds.Add(stationId))
                    throw new ScenarioException($"tradingMap.rules.templates['{template.TemplateId}'].offsets repeats station '{stationId}'.");
                if (!double.IsFinite(offset.X) || !double.IsFinite(offset.Y))
                    throw new ScenarioException($"tradingMap.rules.templates['{template.TemplateId}'].offsets['{stationId}'] coordinates must be finite.");
                if (string.Equals(stationId, startId, StringComparison.OrdinalIgnoreCase) && (offset.X != 0 || offset.Y != 0))
                    throw new ScenarioException($"tradingMap.rules.templates['{template.TemplateId}'].offsets['{stationId}'] must be (0,0) for the start station.");
                normalizedOffsets.Add(offset with { StationObjectId = stationId });
            }
            if (offsetIds.Count != stationMap.Count)
                throw new ScenarioException($"tradingMap.rules.templates['{template.TemplateId}'].offsets must cover all five stations.");

            var links = RequireCollection(template.Links, $"tradingMap.rules.templates['{template.TemplateId}'].links");
            var linkPairs = new HashSet<string>(StringComparer.Ordinal);
            var normalizedLinks = new List<TradingMapLinkData>(links.Count);
            for (var i = 0; i < links.Count; i++)
            {
                var link = links[i] ?? throw new ScenarioException($"tradingMap.rules.templates['{template.TemplateId}'].links[{i}] is null.");
                var from = ResolveStation(link.FromStationObjectId, stationMap,
                    $"tradingMap.rules.templates['{template.TemplateId}'].links[{i}].fromStationObjectId");
                var to = ResolveStation(link.ToStationObjectId, stationMap,
                    $"tradingMap.rules.templates['{template.TemplateId}'].links[{i}].toStationObjectId");
                if (string.Equals(from, to, StringComparison.OrdinalIgnoreCase))
                    throw new ScenarioException($"tradingMap.rules.templates['{template.TemplateId}'].links[{i}] must not be a self-loop.");
                var risk = ResolveRisk(link.RiskProfileId, riskMap, $"tradingMap.rules.templates['{template.TemplateId}'].links[{i}].riskProfileId");
                (from, to) = OrderEndpoints(from, to);
                if (!linkPairs.Add(EndpointKey(from, to)))
                    throw new ScenarioException($"tradingMap.rules.templates['{template.TemplateId}'].links[{i}] duplicates edge '{from}'/'{to}'.");
                normalizedLinks.Add(link with { FromStationObjectId = from, ToStationObjectId = to, RiskProfileId = risk });
            }
            normalizedTemplates.Add(template with
            {
                Links = normalizedLinks.OrderBy(l => l.FromStationObjectId, StringComparer.Ordinal)
                    .ThenBy(l => l.ToStationObjectId, StringComparer.Ordinal)
                    .ThenBy(l => l.RiskProfileId, StringComparer.Ordinal).ToArray(),
                Offsets = normalizedOffsets.OrderBy(o => o.StationObjectId, StringComparer.Ordinal).ToArray(),
            });
        }

        if (!isResult)
        {
            foreach (var station in normalizedStations.Where(s => !string.Equals(s.ObjectId, transit.ObjectId, StringComparison.OrdinalIgnoreCase)))
                if (objectMap.ContainsKey(station.ObjectId))
                    throw new ScenarioException($"tradingMap.rules.stations['{station.ObjectId}'].objectId overlaps an existing spaceObjects object.");
        }

        return rules with
        {
            StartStationObjectId = startId,
            Stations = normalizedStations.OrderBy(s => s.ObjectId, StringComparer.Ordinal).ToArray(),
            Templates = normalizedTemplates.OrderBy(t => t.TemplateId, StringComparer.Ordinal).ToArray(),
            RiskProfiles = normalizedRisks.OrderBy(r => r.RiskProfileId, StringComparer.Ordinal).ToArray(),
        };
    }

    private static ulong NormalizeCounter(string name, ulong counter)
    {
        if (counter < 10_000)
            throw new ScenarioException($"tradingMap.rngStreams['{name}'].counter must be at least 10000.");
        var remainder = counter % 10;
        if (remainder == 0)
            return counter;
        try
        {
            var normalized = checked(counter + (10 - remainder));
            Trace.TraceWarning("Rounded RNG stream '{0}' counter from {1} to {2}.", name, counter, normalized);
            return normalized;
        }
        catch (OverflowException ex)
        {
            throw new ScenarioException($"tradingMap.rngStreams['{name}'].counter cannot be rounded without overflow.", ex);
        }
    }

    private static string NormalizeStationSize(string? value, string objectId)
    {
        Required(value, $"station '{objectId}'.stationSize");
        return value switch
        {
            "Outpost" => "Outpost",
            "Medium" => "Medium",
            "Large" => "Large",
            "Huge" => "Huge",
            _ => throw new ScenarioException($"station '{objectId}'.stationSize must be Outpost, Medium, Large, or Huge."),
        };
    }

    private static string ResolveStation(string? value, IReadOnlyDictionary<string, TradingMapStationData> stations, string field)
    {
        Required(value, field);
        if (!stations.TryGetValue(value!, out var station))
            throw new ScenarioException($"{field} '{value}' references an unknown station objectId.");
        return station.ObjectId;
    }

    private static string ResolveRisk(string? value, IReadOnlyDictionary<string, string> risks, string field)
    {
        Required(value, field);
        if (!risks.TryGetValue(value!, out var canonical))
            throw new ScenarioException($"{field} '{value}' references an unknown riskProfileId.");
        return canonical;
    }

    private static void Required(string? value, string field)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ScenarioException($"{field} is required.");
    }

    private static IReadOnlyList<T> RequireCollection<T>(IReadOnlyList<T>? value, string field)
    {
        if (value is null || value.Count == 0)
            throw new ScenarioException($"{field} must not be null or empty.");
        return value;
    }

    private static (string From, string To) OrderEndpoints(string from, string to) =>
        string.CompareOrdinal(from, to) <= 0 ? (from, to) : (to, from);

    private static string EndpointKey(string from, string to) => $"{from}\u001f{to}";
}
