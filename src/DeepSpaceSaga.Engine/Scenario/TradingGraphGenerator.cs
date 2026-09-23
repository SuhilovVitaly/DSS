using DeepSpaceSaga.Engine.Content;
using DeepSpaceSaga.Engine.Rng;

namespace DeepSpaceSaga.Engine.Scenario;

internal sealed record TradingGraphPlan(
    TradingMapGenerationData Rules,
    TradingMapTemplateData Template,
    IReadOnlyList<TradingMapCargoFlowData> CargoFlows,
    TradingMapRngData TopologyStream);

internal static class TradingGraphGenerator
{
    private static readonly string[] RequiredProfiles =
    [
        "market.transit",
        "market.mining",
        "market.industrial",
        "market.hydroponic",
        "market.scientific-military",
    ];

    internal static TradingGraphPlan Generate(
        TradingMapGenerationData rules,
        ulong masterSeed,
        GameDataRegistry registry)
    {
        ArgumentNullException.ThrowIfNull(rules);
        ArgumentNullException.ThrowIfNull(registry);

        var canonicalRules = CanonicalizeRules(rules);
        var validatedTemplates = new List<(TradingMapTemplateData Template, IReadOnlyList<TradingMapCargoFlowData> Flows)>(
            canonicalRules.Templates.Count);

        // Validate every candidate before consuming the topology stream. A bad candidate must
        // never become seed-dependent merely because another candidate would have been chosen.
        foreach (var template in canonicalRules.Templates)
            validatedTemplates.Add((template, ValidateTemplate(canonicalRules, template, registry)));

        if (validatedTemplates.Count == 0)
            throw new ScenarioException("tradingMap.rules.templates must contain at least one template.");

        var selection = TradingMapRandom.DrawIndex(
            masterSeed,
            "TradingMap.Topology",
            validatedTemplates.Count);
        var selected = validatedTemplates[selection.Value];

        return new TradingGraphPlan(
            canonicalRules,
            selected.Template,
            selected.Flows,
            selection.State);
    }

    internal static IReadOnlyList<TradingMapCargoFlowData> ValidateTemplate(
        TradingMapGenerationData rules,
        TradingMapTemplateData template,
        GameDataRegistry registry)
    {
        ArgumentNullException.ThrowIfNull(rules);
        ArgumentNullException.ThrowIfNull(template);
        ArgumentNullException.ThrowIfNull(registry);

        var canonicalRules = CanonicalizeRules(rules);
        var stationById = BuildStationMap(canonicalRules);
        var canonicalTemplate = CanonicalizeTemplate(template, stationById, canonicalRules.RiskProfiles);
        var profileByStation = ResolveProfiles(canonicalRules, canonicalTemplate.TemplateId, stationById, registry);
        ValidateGraph(canonicalRules, canonicalTemplate, stationById);

        var flows = BuildCargoFlows(canonicalRules, canonicalTemplate, profileByStation, stationById);
        ValidateCargoRequirements(canonicalRules, flows, profileByStation);
        return flows;
    }

    private static TradingMapGenerationData CanonicalizeRules(TradingMapGenerationData rules)
    {
        if (rules.Stations is null)
            throw new ScenarioException("tradingMap.rules.stations must not be null.");
        if (rules.Templates is null)
            throw new ScenarioException("tradingMap.rules.templates must not be null.");
        if (rules.RiskProfiles is null)
            throw new ScenarioException("tradingMap.rules.riskProfiles must not be null.");

        var stations = rules.Stations.ToArray();
        if (stations.Length != RequiredProfiles.Length)
            throw new ScenarioException($"tradingMap.rules.stations must contain exactly five stations, got {stations.Length}.");

        var stationIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var profileIds = new HashSet<string>(StringComparer.Ordinal);
        foreach (var station in stations)
        {
            if (station is null)
                throw new ScenarioException("tradingMap.rules.stations contains a null element.");
            Required(station.ObjectId, "tradingMap.rules.stations.objectId");
            Required(station.MarketProfileId, $"station '{station.ObjectId}'.marketProfileId");
            if (!stationIds.Add(station.ObjectId))
                throw new ScenarioException($"tradingMap.rules.stations has duplicate objectId '{station.ObjectId}'.");
            if (!profileIds.Add(station.MarketProfileId))
                throw new ScenarioException($"tradingMap.rules.stations has duplicate marketProfileId '{station.MarketProfileId}'.");
            if (!RequiredProfiles.Contains(station.MarketProfileId, StringComparer.Ordinal))
                throw new ScenarioException($"station '{station.ObjectId}' has unknown marketProfileId '{station.MarketProfileId}'.");
        }

        if (!profileIds.SetEquals(RequiredProfiles))
            throw new ScenarioException("tradingMap.rules.stations must contain exactly one station for each required market profile.");

        var riskIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var risk in rules.RiskProfiles)
        {
            if (risk is null)
                throw new ScenarioException("tradingMap.rules.riskProfiles contains a null element.");
            Required(risk.RiskProfileId, "tradingMap.rules.riskProfiles.riskProfileId");
            if (!riskIds.Add(risk.RiskProfileId))
                throw new ScenarioException($"tradingMap.rules.riskProfiles has duplicate riskProfileId '{risk.RiskProfileId}'.");
        }

        var stationMap = stations.ToDictionary(station => station.ObjectId, StringComparer.OrdinalIgnoreCase);
        var templateIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var template in rules.Templates)
        {
            if (template is null)
                throw new ScenarioException("tradingMap.rules.templates contains a null element.");
            Required(template.TemplateId, "tradingMap.rules.templates.templateId");
            if (!templateIds.Add(template.TemplateId))
                throw new ScenarioException($"tradingMap.rules.templates has duplicate templateId '{template.TemplateId}'.");
        }
        var canonicalTemplates = rules.Templates
            .Select(template => CanonicalizeTemplate(template, stationMap, rules.RiskProfiles))
            .OrderBy(template => template.TemplateId, StringComparer.Ordinal)
            .ToArray();

        var startStation = ResolveStation(rules.StartStationObjectId, stationMap, "tradingMap.rules.startStationObjectId");
        var transit = stations.Single(station => station.MarketProfileId == "market.transit");
        if (!string.Equals(startStation, transit.ObjectId, StringComparison.OrdinalIgnoreCase))
            throw new ScenarioException("tradingMap.rules.startStationObjectId must reference the market.transit station.");

        return rules with
        {
            StartStationObjectId = transit.ObjectId,
            Stations = stations.OrderBy(station => station.ObjectId, StringComparer.Ordinal).ToArray(),
            Templates = canonicalTemplates,
            RiskProfiles = rules.RiskProfiles.OrderBy(risk => risk.RiskProfileId, StringComparer.Ordinal).ToArray(),
        };
    }

    private static TradingMapTemplateData CanonicalizeTemplate(
        TradingMapTemplateData template,
        IReadOnlyDictionary<string, TradingMapStationData> stationById,
        IReadOnlyList<TradingMapRiskData> risks)
    {
        if (template is null)
            throw new ScenarioException("tradingMap.rules.templates contains a null element.");
        Required(template.TemplateId, "tradingMap.rules.templates.templateId");
        if (template.Links is null || template.Links.Count == 0)
            throw new ScenarioException($"template '{template.TemplateId}'.links must not be null or empty.");
        if (template.Offsets is null || template.Offsets.Count == 0)
            throw new ScenarioException($"template '{template.TemplateId}'.offsets must not be null or empty.");

        var riskById = risks.ToDictionary(risk => risk.RiskProfileId, StringComparer.OrdinalIgnoreCase);
        var links = new List<TradingMapLinkData>(template.Links.Count);
        var edgeKeys = new HashSet<string>(StringComparer.Ordinal);
        foreach (var link in template.Links)
        {
            if (link is null)
                throw new ScenarioException($"template '{template.TemplateId}'.links contains a null element.");
            var from = ResolveStation(link.FromStationObjectId, stationById,
                $"template '{template.TemplateId}'.links.fromStationObjectId");
            var to = ResolveStation(link.ToStationObjectId, stationById,
                $"template '{template.TemplateId}'.links.toStationObjectId");
            if (string.Equals(from, to, StringComparison.OrdinalIgnoreCase))
                throw new ScenarioException($"template '{template.TemplateId}' must not contain self-loop '{from}'.");
            if (!riskById.TryGetValue(link.RiskProfileId ?? string.Empty, out var risk))
                throw new ScenarioException(
                    $"template '{template.TemplateId}' references unknown riskProfileId '{link.RiskProfileId}'.");

            (from, to) = OrderEndpoints(from, to);
            if (!edgeKeys.Add(EndpointKey(from, to)))
                throw new ScenarioException($"template '{template.TemplateId}' duplicates edge '{from}'/'{to}'.");
            links.Add(new TradingMapLinkData(from, to, risk.RiskProfileId));
        }

        var offsets = new List<TradingMapOffsetData>(template.Offsets.Count);
        var offsetIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var offset in template.Offsets)
        {
            if (offset is null)
                throw new ScenarioException($"template '{template.TemplateId}'.offsets contains a null element.");
            var stationId = ResolveStation(offset.StationObjectId, stationById,
                $"template '{template.TemplateId}'.offsets.stationObjectId");
            if (!offsetIds.Add(stationId))
                throw new ScenarioException($"template '{template.TemplateId}'.offsets repeats station '{stationId}'.");
            if (!double.IsFinite(offset.X) || !double.IsFinite(offset.Y))
                throw new ScenarioException($"template '{template.TemplateId}'.offsets['{stationId}'] coordinates must be finite.");
            offsets.Add(new TradingMapOffsetData(stationId, offset.X, offset.Y));
        }

        if (!offsetIds.SetEquals(stationById.Keys))
            throw new ScenarioException($"template '{template.TemplateId}'.offsets must cover all five stations.");

        return template with
        {
            Links = links.OrderBy(link => link.FromStationObjectId, StringComparer.Ordinal)
                .ThenBy(link => link.ToStationObjectId, StringComparer.Ordinal)
                .ToArray(),
            Offsets = offsets.OrderBy(offset => offset.StationObjectId, StringComparer.Ordinal).ToArray(),
        };
    }

    private static Dictionary<string, StationMarketProfileDefinition> ResolveProfiles(
        TradingMapGenerationData rules,
        string templateId,
        IReadOnlyDictionary<string, TradingMapStationData> stationById,
        GameDataRegistry registry)
    {
        var result = new Dictionary<string, StationMarketProfileDefinition>(StringComparer.Ordinal);
        foreach (var station in stationById.Values)
        {
            StationMarketProfileDefinition profile;
            try
            {
                profile = registry.StationMarketProfiles.GetDefinition(
                    registry.StationMarketProfiles.GetIndex(station.MarketProfileId));
            }
            catch (ContentException ex)
            {
                throw new ScenarioException(
                    $"template '{templateId}' station '{station.ObjectId}' uses unknown market profile '{station.MarketProfileId}'.", ex);
            }

            if (!string.Equals(profile.TypeId, station.MarketProfileId, StringComparison.Ordinal))
                throw new ScenarioException(
                    $"station '{station.ObjectId}' market profile '{station.MarketProfileId}' resolved to '{profile.TypeId}'.");

            ValidateProfileItems(templateId, profile, registry);
            result.Add(station.ObjectId, profile);
        }

        return result;
    }

    private static void ValidateProfileItems(
        string templateId,
        StationMarketProfileDefinition profile,
        GameDataRegistry registry)
    {
        ValidateItemList(templateId, profile.TypeId, "supplyItemTypeIds", profile.SupplyItemTypeIds, registry);
        ValidateItemList(templateId, profile.TypeId, "demandItemTypeIds", profile.DemandItemTypeIds, registry);
        foreach (var stock in profile.InitialInventory)
            ResolveItem(templateId, profile.TypeId, "initialInventory", stock.ItemTypeId, registry);

        if (profile.Economy is not { } economy)
            return;
        foreach (var stock in economy.HourlyInputs)
            ResolveItem(templateId, profile.TypeId, "economy.hourlyInputs", stock.ItemTypeId, registry);
        foreach (var stock in economy.HourlyOutputs)
            ResolveItem(templateId, profile.TypeId, "economy.hourlyOutputs", stock.ItemTypeId, registry);
        foreach (var stock in economy.HourlyConsumption)
            ResolveItem(templateId, profile.TypeId, "economy.hourlyConsumption", stock.ItemTypeId, registry);
        foreach (var target in economy.StockTargets)
            ResolveItem(templateId, profile.TypeId, "economy.stockTargets", target.ItemTypeId, registry);
    }

    private static void ValidateItemList(
        string templateId,
        string profileId,
        string field,
        IEnumerable<string> itemIds,
        GameDataRegistry registry)
    {
        foreach (var itemId in itemIds)
            ResolveItem(templateId, profileId, field, itemId, registry);
    }

    private static string ResolveItem(
        string templateId,
        string profileId,
        string field,
        string? itemId,
        GameDataRegistry registry)
    {
        Required(itemId, $"profile '{profileId}'.{field}");
        try
        {
            var index = registry.ItemTypes.GetIndex(itemId!);
            return registry.ItemTypes.GetDefinition(index).TypeId;
        }
        catch (ContentException ex)
        {
            throw new ScenarioException(
                $"template '{templateId}' profile '{profileId}' references unknown itemTypeId '{itemId}'.", ex);
        }
    }

    private static void ValidateGraph(
        TradingMapGenerationData rules,
        TradingMapTemplateData template,
        IReadOnlyDictionary<string, TradingMapStationData> stationById)
    {
        var adjacency = stationById.Keys.ToDictionary(id => id, _ => new HashSet<string>(StringComparer.Ordinal));
        foreach (var link in template.Links)
        {
            adjacency[link.FromStationObjectId].Add(link.ToStationObjectId);
            adjacency[link.ToStationObjectId].Add(link.FromStationObjectId);
        }

        var first = stationById.Keys.First();
        if (ReachableCount(adjacency, first) != stationById.Count)
            throw new ScenarioException($"template '{template.TemplateId}' graph must be connected.");
        if (adjacency.Any(pair => pair.Value.Count < 2))
            throw new ScenarioException($"template '{template.TemplateId}' graph requires degree >= 2 for every station.");

        var components = 1;
        var cycleRank = template.Links.Count - stationById.Count + components;
        if (cycleRank < 2)
            throw new ScenarioException($"template '{template.TemplateId}' graph must contain at least two independent cycles.");

        foreach (var link in template.Links)
        {
            var remaining = stationById.Keys.ToDictionary(id => id, _ => new HashSet<string>(StringComparer.Ordinal));
            foreach (var candidate in template.Links)
            {
                if (ReferenceEquals(link, candidate) ||
                    (candidate.FromStationObjectId == link.FromStationObjectId && candidate.ToStationObjectId == link.ToStationObjectId))
                    continue;
                remaining[candidate.FromStationObjectId].Add(candidate.ToStationObjectId);
                remaining[candidate.ToStationObjectId].Add(candidate.FromStationObjectId);
            }

            if (ReachableCount(remaining, first) != stationById.Count)
                throw new ScenarioException(
                    $"template '{template.TemplateId}' contains bridge edge '{link.FromStationObjectId}'/'{link.ToStationObjectId}'.");
        }

        var mining = stationById.Values.Single(station => station.MarketProfileId == "market.mining").ObjectId;
        var industrial = stationById.Values.Single(station => station.MarketProfileId == "market.industrial").ObjectId;
        var hydroponic = stationById.Values.Single(station => station.MarketProfileId == "market.hydroponic").ObjectId;
        var scientific = stationById.Values.Single(station => station.MarketProfileId == "market.scientific-military").ObjectId;
        RequireEdge(template, mining, industrial);
        RequireEdge(template, industrial, hydroponic);
        RequireEdge(template, industrial, scientific);
    }

    private static IReadOnlyList<TradingMapCargoFlowData> BuildCargoFlows(
        TradingMapGenerationData rules,
        TradingMapTemplateData template,
        IReadOnlyDictionary<string, StationMarketProfileDefinition> profiles,
        IReadOnlyDictionary<string, TradingMapStationData> stationById)
    {
        var flows = new List<TradingMapCargoFlowData>();
        foreach (var link in template.Links)
        {
            AddFlow(link.FromStationObjectId, link.ToStationObjectId);
            AddFlow(link.ToStationObjectId, link.FromStationObjectId);
        }

        return flows
            .OrderBy(flow => flow.FromStationObjectId, StringComparer.Ordinal)
            .ThenBy(flow => flow.ToStationObjectId, StringComparer.Ordinal)
            .ToArray();

        void AddFlow(string from, string to)
        {
            var items = profiles[from].SupplyItemTypeIds
                .Intersect(profiles[to].DemandItemTypeIds, StringComparer.Ordinal)
                .Where(itemId => itemId != "item.fuel")
                .OrderBy(itemId => itemId, StringComparer.Ordinal)
                .ToArray();
            if (items.Length != 0)
                flows.Add(new TradingMapCargoFlowData(from, to, items));
        }
    }

    private static void ValidateCargoRequirements(
        TradingMapGenerationData rules,
        IReadOnlyList<TradingMapCargoFlowData> flows,
        IReadOnlyDictionary<string, StationMarketProfileDefinition> profiles)
    {
        var transitId = rules.StartStationObjectId;
        foreach (var pair in profiles)
        {
            if (string.Equals(pair.Key, transitId, StringComparison.OrdinalIgnoreCase) &&
                pair.Value.SupplyItemTypeIds.Length != 0)
                throw new ScenarioException($"transit station '{pair.Key}' must not have supply items.");

            if (pair.Value.SupplyItemTypeIds.Length == 0 ||
                flows.Any(flow => flow.FromStationObjectId == pair.Key))
                continue;
            throw new ScenarioException($"station '{pair.Key}' has supply items but no outgoing cargo flow.");
        }

        var hasReturnPair = flows.Any(flow => flows.Any(reverse =>
            reverse.FromStationObjectId == flow.ToStationObjectId &&
            reverse.ToStationObjectId == flow.FromStationObjectId));
        if (!hasReturnPair)
            throw new ScenarioException("template cargo graph must contain at least one pair of flows in both directions.");

        if (!flows.Any(flow => !string.Equals(flow.ToStationObjectId, transitId, StringComparison.OrdinalIgnoreCase)))
            throw new ScenarioException("template cargo graph must contain a non-transit consumer.");
    }

    private static int ReachableCount(IReadOnlyDictionary<string, HashSet<string>> adjacency, string start)
    {
        var visited = new HashSet<string>(StringComparer.Ordinal) { start };
        var queue = new Queue<string>();
        queue.Enqueue(start);
        while (queue.Count != 0)
        {
            foreach (var next in adjacency[queue.Dequeue()])
            {
                if (visited.Add(next))
                    queue.Enqueue(next);
            }
        }

        return visited.Count;
    }

    private static void RequireEdge(TradingMapTemplateData template, string left, string right)
    {
        if (!template.Links.Any(link =>
            (link.FromStationObjectId == left && link.ToStationObjectId == right) ||
            (link.FromStationObjectId == right && link.ToStationObjectId == left)))
            throw new ScenarioException($"template '{template.TemplateId}' is missing required edge '{left}'/'{right}'.");
    }

    private static Dictionary<string, TradingMapStationData> BuildStationMap(TradingMapGenerationData rules) =>
        rules.Stations.ToDictionary(station => station.ObjectId, StringComparer.OrdinalIgnoreCase);

    private static string ResolveStation(
        string? value,
        IReadOnlyDictionary<string, TradingMapStationData> stations,
        string field)
    {
        Required(value, field);
        if (!stations.TryGetValue(value!, out var station))
            throw new ScenarioException($"{field} '{value}' references unknown station objectId.");
        return station.ObjectId;
    }

    private static void Required(string? value, string field)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ScenarioException($"{field} is required.");
    }

    private static (string From, string To) OrderEndpoints(string from, string to) =>
        string.CompareOrdinal(from, to) <= 0 ? (from, to) : (to, from);

    private static string EndpointKey(string from, string to) => $"{from}\u001f{to}";
}

internal static class TradingMapRandom
{
    internal static (int Value, TradingMapRngData State) DrawIndex(
        ulong masterSeed,
        string streamName,
        int exclusiveMax)
    {
        if (exclusiveMax <= 0)
            throw new ArgumentOutOfRangeException(nameof(exclusiveMax), exclusiveMax, "Must be positive.");

        var seed = RngStreamSeedDerivation.DeriveStreamSeed(masterSeed, streamName);
        var random = RngStreamNames.CreateDeterministicRandom(seed);
        for (var i = 0; i < 1000; i++)
            _ = random.NextDouble();

        var value = random.NextDouble();
        var index = (int)Math.Floor(value * exclusiveMax);
        return (index, new TradingMapRngData(streamName, seed, 10010));
    }
}
