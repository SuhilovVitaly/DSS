using System.Collections.Immutable;
using System.Text.Json.Serialization;
using DeepSpaceSaga.Contracts;

namespace DeepSpaceSaga.Engine.Scenario;

/// <summary>Campaign metadata only. Resources remain in the existing authoritative world.</summary>
public sealed record DemoCampaignData(
    [property: JsonPropertyName("schemaVersion")] int SchemaVersion = 1,
    [property: JsonPropertyName("revision")] long Revision = 0,
    [property: JsonPropertyName("commitSequence")] long CommitSequence = 0,
    [property: JsonPropertyName("lastTickGameTimeMs")] long LastTickGameTimeMs = 0,
    [property: JsonPropertyName("company")] DemoCompanySnapshot? Company = null,
    [property: JsonPropertyName("transport")] DemoTransportSnapshot? Transport = null,
    [property: JsonPropertyName("encounters")] DemoEncountersSnapshot? Encounters = null,
    [property: JsonPropertyName("rewards")] DemoRewardsSnapshot? Rewards = null,
    [property: JsonPropertyName("progression")] DemoProgressionSnapshot? Progression = null,
    [property: JsonPropertyName("story")] DemoStorySnapshot? Story = null)
{
    [JsonPropertyName("catalogFingerprint")]
    public string? CatalogFingerprint { get; init; }

    [JsonPropertyName("pilotMemorial")]
    public ImmutableArray<PilotMemorialEntry> PilotMemorial { get; init; } = [];

    [JsonPropertyName("nextCrewSequence")]
    public long NextCrewSequence { get; init; } = 1;

    [JsonPropertyName("receipts")]
    public ImmutableArray<DemoCampaignReceiptData> Receipts { get; init; } = [];

    /// <summary>EP-0034 US-0006: published offers and the obligations journal; null until the first publication.</summary>
    [JsonPropertyName("stationJobs")]
    public StationJobsStateData? StationJobs { get; init; }

    internal static DemoCampaignData Validate(DemoCampaignData? data, long gameTimeMs)
    {
        data ??= new(LastTickGameTimeMs: gameTimeMs);
        if (data.SchemaVersion != 1 || data.Revision < 0 || data.CommitSequence < data.Revision ||
            data.LastTickGameTimeMs < 0 || data.LastTickGameTimeMs > gameTimeMs || data.Receipts.IsDefault ||
            data.NextCrewSequence < 1 || data.PilotMemorial.IsDefault)
            throw new ScenarioException("Invalid demo campaign version or counters.");
        // EP-0034 US-0001: passenger capacity is derived from the world on projection, never trusted from a file.
        if (data.Transport?.PassengerCapacity is not null)
            throw new ScenarioException("Invalid demo campaign transport: passenger capacity is derived, not persisted.");
        ValidateStationJobs(data, gameTimeMs);
        var ids = new HashSet<string>(StringComparer.Ordinal);
        long previousSequence = 0;
        long previousRevision = 0;
        foreach (var receipt in data.Receipts)
        {
            if (receipt is null || receipt.Response is null || string.IsNullOrWhiteSpace(receipt.Response.CommandId) ||
                !ids.Add(receipt.Response.CommandId) || receipt.PayloadHash is not { Length: 64 } ||
                !receipt.PayloadHash.All(Uri.IsHexDigit) || receipt.CommitSequence <= previousSequence ||
                receipt.CommitSequence > data.CommitSequence || receipt.Response.Revision < 0 ||
                receipt.Response.Revision > data.Revision || receipt.Response.Revision < previousRevision ||
                (receipt.Response.Applied ? receipt.Response.Reason is not null : string.IsNullOrWhiteSpace(receipt.Response.Reason)))
                throw new ScenarioException("Invalid demo campaign receipt.");
            previousSequence = receipt.CommitSequence;
            previousRevision = receipt.Response.Revision;
        }
        return data;
    }

    /// <summary>Atomic structural check of the board/journal; a corrupted section rejects the whole Load.</summary>
    private static void ValidateStationJobs(DemoCampaignData data, long gameTimeMs)
    {
        if (data.Transport?.StationJobs is not null)
            throw new ScenarioException("Invalid demo campaign transport: the job board projection is not persisted.");
        if (data.StationJobs is not { } jobs) return;
        static bool Text(string? value) => !string.IsNullOrWhiteSpace(value);
        bool valid = data.CatalogFingerprint is not null && jobs.PublishedBucket >= 0 &&
            !jobs.Offers.IsDefault && !jobs.Obligations.IsDefault &&
            jobs.Offers.All(o => o is not null && Text(o.Id) && StationJobTypes.IsKnown(o.Type) && Text(o.OriginStationId) &&
                Text(o.DestinationStationId) && o.OriginStationId != o.DestinationStationId && o.Count > 0 && o.MassKg >= 0 &&
                StationJobRiskBands.IsKnown(o.RiskBand) && o.RouteDistanceKm > 0 && o.DeadlineDurationMs > 0 &&
                o.PublishedAtGameTimeMs >= 0 && o.PublishedAtGameTimeMs <= gameTimeMs && o.ExpiresAtGameTimeMs > o.PublishedAtGameTimeMs &&
                o.Terms is { BaseTokens: >= 0, LoadTokens: >= 0, DistanceTokens: >= 0, UrgencyPermille: >= 1000, RiskPermille: >= 1000 } &&
                o.Payout == SimulationEngine.StationJobPayout(o.Terms) &&
                (o.Type == StationJobTypes.Cargo) == (o.ItemTypeId is not null && o.MassKg > 0)) &&
            jobs.Obligations.All(o => o is not null && Text(o.ContractId) && Text(o.OfferId) && StationJobTypes.IsKnown(o.Type) &&
                Text(o.OriginStationId) && Text(o.DestinationStationId) && o.Count > 0 && o.MassKg >= 0 && o.Payout >= 0 &&
                StationJobStatuses.IsKnown(o.Status) && StationJobRiskBands.IsKnown(o.RiskBand) &&
                o.AcceptedAtGameTimeMs >= 0 && o.AcceptedAtGameTimeMs <= gameTimeMs && o.DeadlineGameTimeMs > o.AcceptedAtGameTimeMs &&
                (o.Status == StationJobStatuses.Active) == (o.ClosedAtGameTimeMs is null) &&
                (o.ClosedAtGameTimeMs is not { } closed || closed >= o.AcceptedAtGameTimeMs && closed <= gameTimeMs)) &&
            jobs.Offers.Select(o => o.Id).Distinct(StringComparer.Ordinal).Count() == jobs.Offers.Length &&
            jobs.Obligations.Select(o => o.ContractId).Distinct(StringComparer.Ordinal).Count() == jobs.Obligations.Length &&
            jobs.Obligations.Select(o => o.OfferId).Distinct(StringComparer.Ordinal).Count() == jobs.Obligations.Length &&
            !jobs.Offers.Any(o => jobs.Obligations.Any(b => b.OfferId == o.Id));
        if (!valid) throw new ScenarioException("Invalid demo campaign station jobs.");
    }
}

/// <summary>Persisted job board: last published calendar bucket, live offers with frozen terms and the journal.</summary>
public sealed record StationJobsStateData(
    [property: JsonPropertyName("publishedBucket")] long PublishedBucket,
    [property: JsonPropertyName("offers")] ImmutableArray<JobOffer> Offers,
    [property: JsonPropertyName("obligations")] ImmutableArray<JobObligation> Obligations);

public sealed record DemoCampaignReceiptData(
    [property: JsonPropertyName("payloadHash")] string PayloadHash,
    [property: JsonPropertyName("commitSequence")] long CommitSequence,
    [property: JsonPropertyName("response")] DemoCampaignResponse Response);
