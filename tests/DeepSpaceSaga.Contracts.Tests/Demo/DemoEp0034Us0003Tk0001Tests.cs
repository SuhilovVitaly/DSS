using System.Text.Json;

namespace DeepSpaceSaga.Contracts.Tests.Demo;

/// <summary>
/// EP-0034-US-0003-TK-0001: a failed or cancelled transport obligation is an immutable, JSON-serializable projection
/// inside the demo transport section. It states the reason, the penalty, what was paid, the remaining contract debt
/// and whether passengers still wait for a docking to leave; the terms of cancellation/expiry are published before
/// the player acts, and every outcome and refusal uses a stable code.
/// </summary>
public sealed class DemoEp0034Us0003Tk0001Tests
{
    private const string ContractId = "contract:jobs:SPC-0002:0:0";

    private static TransportFailureSnapshot Failure(string reason = TransportFailureReasons.Cancelled) => new(
        ContractId, StationJobTypes.Passenger, reason, FailedAtGameTimeMs: 3_600_001, PenaltyCredits: 105,
        PenaltyPaid: 80, Debt: 25, DisembarkPending: true, PostingId: TransportFailureReasons.PostingIdFor(ContractId),
        PassengerIds: [ContractId + ":pax-1", ContractId + ":pax-2"]);

    private static TransportFailuresSnapshot Section() => new(
        [Failure(), Failure(TransportFailureReasons.Expired) with
        {
            ContractId = "contract:jobs:SPC-0005:1:0", DisembarkPending = false, DisembarkedAtGameTimeMs = 4_000_000,
            DisembarkStationId = "SPC-0005", Debt = 0, PenaltyPaid = 105
        }],
        [new TransportCancellationTerms("contract:jobs:SPC-0002:2:0", CancelPenaltyCredits: 50, ExpiryPenaltyCredits: 50,
            DeadlineGameTimeMs: 9_000_000, Blocker: null),
         new TransportCancellationTerms("contract:jobs:SPC-0002:3:0", 70, 70, 9_500_000, DemoCampaignReasons.BattleActive)],
        ContractDebt: 25, ObservedAtGameTimeMs: 4_000_000);

    [Fact]
    public void Payload_RoundTrips()
    {
        var snapshot = new DemoCampaignSnapshot(5, 7, Transport: new DemoTransportSnapshot { TransportFailures = Section() });

        var loaded = JsonSerializer.Deserialize<DemoCampaignSnapshot>(JsonSerializer.Serialize(snapshot))!.Transport!.TransportFailures!;

        Assert.Equal(2, loaded.Failures.Length);
        var expected = Failure();
        Assert.Equal(expected, loaded.Failures[0] with { PassengerIds = expected.PassengerIds });
        Assert.Equal(expected.PassengerIds.ToArray(), loaded.Failures[0].PassengerIds.ToArray());
        Assert.Equal(("SPC-0005", 4_000_000L), (loaded.Failures[1].DisembarkStationId!, loaded.Failures[1].DisembarkedAtGameTimeMs!.Value));
        Assert.Equal(25, loaded.ContractDebt);
        Assert.Equal(4_000_000, loaded.ObservedAtGameTimeMs);
        Assert.Equal(Section().Terms.ToArray(), loaded.Terms.ToArray());

        // Penalty arithmetic is explained by the engine: paid now + still owed + repaid later = incurred.
        Assert.Equal(expected.PenaltyCredits, expected.PenaltyPaid + expected.Debt + expected.RepaidCredits);
        Assert.Same(loaded.Failures[0], TransportFailureReasons.Outcome(loaded, ContractId));
        Assert.Null(TransportFailureReasons.CancelBlocker(loaded, "contract:jobs:SPC-0002:2:0"));
        Assert.Equal(DemoCampaignReasons.BattleActive, TransportFailureReasons.CancelBlocker(loaded, "contract:jobs:SPC-0002:3:0"));

        // Cancellation names only the contract: the client never sends a penalty, a station or a passenger list.
        var json = JsonSerializer.Serialize(new CancelTransportCommand("cmd-9", 5, ContractId));
        Assert.Contains(ContractId, json);
        Assert.DoesNotContain("Penalty", json);
        Assert.DoesNotContain("Station", json);
        Assert.DoesNotContain("Passenger", json);
        Assert.IsAssignableFrom<DemoCampaignCommand>(new CancelTransportCommand("cmd-9", 5, ContractId));
    }

    [Fact]
    public void LegacyPayload_UsesSafeDefaults()
    {
        // Earlier transport sections have no failure section: cancellation is unavailable, nothing is owed or paid.
        var legacy = JsonSerializer.Deserialize<DemoTransportSnapshot>("""{"PassengerCapacity":null,"StationJobs":null,"PassengerContracts":null}""")!;
        Assert.Null(legacy.TransportFailures);
        Assert.Equal(DemoCampaignReasons.Unavailable, TransportFailureReasons.CancelBlocker(legacy.TransportFailures, ContractId));
        Assert.Null(TransportFailureReasons.Outcome(legacy.TransportFailures, ContractId));

        // A partial section lists nothing, offers no cancellation and owes nothing.
        var partial = JsonSerializer.Deserialize<TransportFailuresSnapshot>("""{"ObservedAtGameTimeMs":5}""")!;
        Assert.True(partial.Failures.IsDefaultOrEmpty);
        Assert.True(partial.Terms.IsDefaultOrEmpty);
        Assert.Equal(0, partial.ContractDebt);
        Assert.Equal(DemoCampaignReasons.Unavailable, TransportFailureReasons.CancelBlocker(partial, ContractId));
        Assert.Null(TransportFailureReasons.Outcome(partial, ContractId));

        // A failure with an unknown reason is not an outcome; default arrays serialize as empty lists.
        var unknown = new TransportFailuresSnapshot([Failure() with { Reason = "Cancelled" }], default, 0, 0);
        Assert.Null(TransportFailureReasons.Outcome(unknown, ContractId));
        var noIds = JsonSerializer.Deserialize<TransportFailureSnapshot>(JsonSerializer.Serialize(Failure() with { PassengerIds = default }))!;
        Assert.True(noIds.PassengerIds.IsEmpty);
        Assert.Null(noIds.DisembarkedAtGameTimeMs);
        Assert.Contains("\"Failures\":[]", JsonSerializer.Serialize(new TransportFailuresSnapshot(default, default, 0, 0)));
    }

    [Fact]
    public void ReasonCodes_AreStable()
    {
        Assert.Equal(["cancelled", "expired", "carrier_lost"],
            new[] { TransportFailureReasons.Cancelled, TransportFailureReasons.Expired, TransportFailureReasons.CarrierLost });
        Assert.True(TransportFailureReasons.IsKnownOutcome(TransportFailureReasons.Expired));
        Assert.False(TransportFailureReasons.IsKnownOutcome("Expired"));
        Assert.Equal(["contract_not_found", "contract_not_active", "not_cancellable"],
            new[] { TransportFailureReasons.ContractNotFound, TransportFailureReasons.ContractNotActive, TransportFailureReasons.NotCancellable });
        Assert.Equal("penalty:" + ContractId, TransportFailureReasons.PostingIdFor(ContractId));
        Assert.Equal("penalty:", TransportFailureReasons.PostingPrefix);

        // A failed passenger contract has its own delivery state; earlier codes are unchanged.
        Assert.Equal("failed", PassengerDeliveryStates.Failed);
        Assert.True(PassengerDeliveryStates.IsKnown(PassengerDeliveryStates.Failed));
        Assert.Equal(["aboard", "overdue", "delivered"],
            new[] { PassengerDeliveryStates.Aboard, PassengerDeliveryStates.Overdue, PassengerDeliveryStates.Delivered });
        Assert.Equal("failed", StationJobStatuses.Failed);
        Assert.Equal("cancelled", StationJobStatuses.Cancelled);
    }
}
