using System.Collections.Immutable;
using System.Text.Json;

namespace DeepSpaceSaga.Contracts.Tests.Demo;

/// <summary>
/// EP-0034-US-0006-TK-0001: the station job board is an immutable, JSON-serializable projection inside the demo
/// transport section; acceptance is a typed campaign command and every verdict uses a stable reason code.
/// </summary>
public sealed class DemoEp0034Us0006Tk0001Tests
{
    private static JobOffer Offer(string id = "jobs:SPC-0002:1:0") => new(id, StationJobTypes.Passenger, "SPC-0002", "SPC-0005",
        Count: 3, MassKg: 0, Payout: 420, DeadlineDurationMs: 7_200_000, PublishedAtGameTimeMs: 86_400_000,
        ExpiresAtGameTimeMs: 259_200_000, RiskBand: StationJobRiskBands.Safe, RouteDistanceKm: 15,
        Terms: new JobPayoutTerms(150, 180, 45, 1000, 1000));

    [Fact]
    public void Payload_RoundTrips()
    {
        var obligation = new JobObligation("contract:jobs:SPC-0002:1:1", "jobs:SPC-0002:1:1", StationJobTypes.Cargo,
            "SPC-0002", "SPC-0006", 4000, 4000, 900, 90_000_000, 120_000_000, StationJobStatuses.Active,
            StationJobRiskBands.Normal, ItemTypeId: "item.steel");
        var board = new StationJobsSnapshot([Offer()], [obligation], 90_000_000, "SPC-0002",
            [new JobOfferAvailability("jobs:SPC-0002:1:0", null)]);
        var snapshot = new DemoCampaignSnapshot(2, 5, Transport: new DemoTransportSnapshot { StationJobs = board });

        var loaded = JsonSerializer.Deserialize<DemoCampaignSnapshot>(JsonSerializer.Serialize(snapshot))!.Transport!.StationJobs!;

        Assert.Equal(Offer(), Assert.Single(loaded.Offers));
        Assert.Equal(obligation, Assert.Single(loaded.Obligations));
        Assert.Equal(90_000_000, loaded.ObservedAtGameTimeMs);
        Assert.Equal("SPC-0002", loaded.DockedStationId);
        Assert.Null(StationJobReasons.AcceptBlocker(loaded, "jobs:SPC-0002:1:0"));

        // The typed command round-trips with its identity, revision and offer only (no client price or deadline).
        var command = new AcceptJobOfferCommand("cmd-1", 2, "jobs:SPC-0002:1:0");
        var json = JsonSerializer.Serialize(command);
        Assert.Equal(command, JsonSerializer.Deserialize<AcceptJobOfferCommand>(json));
        Assert.DoesNotContain("Payout", json);
        Assert.DoesNotContain("Deadline", json);
    }

    [Fact]
    public void LegacyPayload_UsesSafeDefaults()
    {
        // US-0001 transport sections carry no board: the board is unavailable, not an empty acceptable list.
        var legacy = JsonSerializer.Deserialize<DemoTransportSnapshot>("""{"PassengerCapacity":null}""")!;
        Assert.Null(legacy.StationJobs);
        Assert.Equal(DemoCampaignReasons.Unavailable, StationJobReasons.AcceptBlocker(legacy.StationJobs, "jobs:SPC-0002:1:0"));

        // A board without verdicts never makes an offer acceptable.
        var partial = JsonSerializer.Deserialize<StationJobsSnapshot>("""{"ObservedAtGameTimeMs":5}""")!;
        Assert.True(partial.Offers.IsDefaultOrEmpty);
        Assert.True(partial.Obligations.IsDefaultOrEmpty);
        Assert.Equal(DemoCampaignReasons.Unavailable, StationJobReasons.AcceptBlocker(partial, "jobs:SPC-0002:1:0"));
        var unlisted = new StationJobsSnapshot([Offer()], [], 0);
        Assert.Equal(DemoCampaignReasons.Unavailable, StationJobReasons.AcceptBlocker(unlisted, "jobs:SPC-0002:1:0"));
        var other = unlisted with { Availability = [new JobOfferAvailability("jobs:SPC-0005:1:0", null)] };
        Assert.Equal(DemoCampaignReasons.Unavailable, StationJobReasons.AcceptBlocker(other, "jobs:SPC-0002:1:0"));
        Assert.Equal(DemoCampaignReasons.Unavailable, StationJobReasons.AcceptBlocker(other, null));

        // Default arrays serialize as empty lists, so a constructed snapshot stays JSON-safe.
        var json = JsonSerializer.Serialize(new StationJobsSnapshot(default, default, 0));
        Assert.Contains("\"Offers\":[]", json);
        Assert.Contains("\"Availability\":[]", json);
    }

    [Fact]
    public void ReasonCodes_AreStable()
    {
        Assert.Equal("offer_not_found", StationJobReasons.OfferNotFound);
        Assert.Equal("offer_expired", StationJobReasons.OfferExpired);
        Assert.Equal("offer_already_accepted", StationJobReasons.OfferAlreadyAccepted);
        Assert.Equal("not_docked_at_origin", StationJobReasons.NotDockedAtOrigin);
        Assert.Equal("insufficient_cargo_space", StationJobReasons.InsufficientCargoSpace);

        Assert.Equal(["passenger", "urgent_passenger", "cargo", "bounty"],
            new[] { StationJobTypes.Passenger, StationJobTypes.UrgentPassenger, StationJobTypes.Cargo, StationJobTypes.Bounty });
        Assert.Equal(["safe", "normal", "elevated"],
            new[] { StationJobRiskBands.Safe, StationJobRiskBands.Normal, StationJobRiskBands.Elevated });
        Assert.Equal(["active", "completed", "failed", "cancelled"],
            new[] { StationJobStatuses.Active, StationJobStatuses.Completed, StationJobStatuses.Failed, StationJobStatuses.Cancelled });
        Assert.True(StationJobTypes.CarriesPassengers(StationJobTypes.UrgentPassenger));
        Assert.False(StationJobTypes.CarriesPassengers(StationJobTypes.Cargo));
        Assert.False(StationJobTypes.IsKnown("Passenger"));

        // An explicit engine verdict is returned verbatim, including capacity blockers from US-0001.
        var board = new StationJobsSnapshot([Offer()], [], 0, null,
            ImmutableArray.Create(new JobOfferAvailability("jobs:SPC-0002:1:0", StationJobReasons.NotDockedAtOrigin)));
        Assert.Equal(StationJobReasons.NotDockedAtOrigin, StationJobReasons.AcceptBlocker(board, "jobs:SPC-0002:1:0"));
    }
}
