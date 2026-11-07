using System.Collections.Immutable;
using System.Text.Json.Serialization;

namespace DeepSpaceSaga.Contracts;

/// <summary>Authoritative membership, independent of map zoom and economic ownership.</summary>
public sealed record StationClusterData(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("beltId")] string BeltId,
    [property: JsonPropertyName("stationIds"), JsonConverter(typeof(ImmutableArrayDefaultJsonConverter<string>))]
    ImmutableArray<string> StationIds,
    [property: JsonPropertyName("specialization")] string Specialization);

public sealed record ClusterStationData(
    [property: JsonPropertyName("objectId")] string ObjectId,
    [property: JsonPropertyName("clusterId")] string ClusterId,
    [property: JsonPropertyName("marketProfileId")] string MarketProfileId);

/// <summary>Potential cargo flow; carries neither a quote nor a fixed travel time.</summary>
public sealed record ClusterTradeLink(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("fromStationId")] string FromStationId,
    [property: JsonPropertyName("toStationId")] string ToStationId,
    [property: JsonPropertyName("itemTypeIds"), JsonConverter(typeof(ImmutableArrayDefaultJsonConverter<string>))]
    ImmutableArray<string> ItemTypeIds);

/// <summary>Frozen generation speed in km/s and straight-flight distances in calendar days.
/// One world unit is 100 metres; calendar time advances 300 times faster than motion time.
/// Semantic validation belongs to Engine, not the transport contract.</summary>
public sealed record ClusterGeographyRules(
    [property: JsonPropertyName("referenceSpeedKmS")] double ReferenceSpeedKmS,
    [property: JsonPropertyName("minimumStationDays")] double MinimumStationDays,
    [property: JsonPropertyName("neighbourMinDays")] double NeighbourMinDays,
    [property: JsonPropertyName("neighbourMaxDays")] double NeighbourMaxDays);

public sealed record StationClusterMapSnapshot(
    [property: JsonPropertyName("rulesVersion")] int RulesVersion,
    [property: JsonPropertyName("startClusterId")] string StartClusterId,
    [property: JsonPropertyName("clusters"), JsonConverter(typeof(ImmutableArrayDefaultJsonConverter<StationClusterData>))]
    ImmutableArray<StationClusterData> Clusters,
    [property: JsonPropertyName("stations"), JsonConverter(typeof(ImmutableArrayDefaultJsonConverter<ClusterStationData>))]
    ImmutableArray<ClusterStationData> Stations,
    [property: JsonPropertyName("links"), JsonConverter(typeof(ImmutableArrayDefaultJsonConverter<ClusterTradeLink>))]
    ImmutableArray<ClusterTradeLink> Links,
    [property: JsonPropertyName("resourceBindings"), JsonConverter(typeof(ImmutableArrayDefaultJsonConverter<ClusterResourceBinding>))]
    ImmutableArray<ClusterResourceBinding> ResourceBindings = default,
    [property: JsonPropertyName("geography")] ClusterGeographyRules? Geography = null);

/// <summary>References a canonical resource object/field. Offset is at the orbital epoch,
/// in world units, and rotates with the owning group; composition remains in resource state.</summary>
public sealed record ClusterResourceBinding(
    [property: JsonPropertyName("fieldId")] string FieldId,
    [property: JsonPropertyName("clusterId")] string ClusterId,
    [property: JsonPropertyName("anchorStationId")] string AnchorStationId,
    [property: JsonPropertyName("offsetX")] double OffsetX,
    [property: JsonPropertyName("offsetY")] double OffsetY);
