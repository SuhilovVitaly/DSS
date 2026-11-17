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

    internal static DemoCampaignData Validate(DemoCampaignData? data, long gameTimeMs)
    {
        data ??= new(LastTickGameTimeMs: gameTimeMs);
        if (data.Company?.StationRepair is { } repair)
        {
            if (repair.Postings.IsDefault || repair.Postings.Any(p => p is null || string.IsNullOrWhiteSpace(p.Id) ||
                string.IsNullOrWhiteSpace(p.TargetId) || p.Kind != "repair" || p.Amount < 0 || p.GameTimeMs < 0 || p.GameTimeMs > gameTimeMs) ||
                repair.Postings.Select(p => p.Id).Distinct(StringComparer.Ordinal).Count() != repair.Postings.Length)
                throw new ScenarioException("Invalid company repair postings.");
            if (repair.LastResult is { } result && (result.Paid < 0 ||
                !repair.Postings.Any(p => p.Id == result.PostingId && p.Amount == result.Paid && p.TargetId == result.TargetId)))
                throw new ScenarioException("Invalid company repair result.");
            if (repair.Quote is { } quote && (string.IsNullOrWhiteSpace(quote.Id) || quote.Total < 0 ||
                quote.Items.IsDefault || quote.Items.Any(i => i is null || i.Quantity < 0 || i.UnitPrice < 0 ||
                    (decimal)i.Quantity * i.UnitPrice != i.Total) || quote.Items.Sum(i => (decimal)i.Total) != quote.Total))
                throw new ScenarioException("Invalid company repair quote.");
        }
        if (data.SchemaVersion != 1 || data.Revision < 0 || data.CommitSequence < data.Revision ||
            data.LastTickGameTimeMs < 0 || data.LastTickGameTimeMs > gameTimeMs || data.Receipts.IsDefault ||
            data.NextCrewSequence < 1 || data.PilotMemorial.IsDefault)
            throw new ScenarioException("Invalid demo campaign version or counters.");
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
}

public sealed record DemoCampaignReceiptData(
    [property: JsonPropertyName("payloadHash")] string PayloadHash,
    [property: JsonPropertyName("commitSequence")] long CommitSequence,
    [property: JsonPropertyName("response")] DemoCampaignResponse Response);
