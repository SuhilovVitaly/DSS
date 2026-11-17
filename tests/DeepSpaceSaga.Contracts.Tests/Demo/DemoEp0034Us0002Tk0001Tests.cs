using System.Text.Json;

namespace DeepSpaceSaga.Contracts.Tests.Demo;

/// <summary>
/// EP-0034-US-0002-TK-0001: an accepted passenger order is an immutable, JSON-serializable projection inside the
/// demo transport section; it names the boarded people, the calendar deadline, the payout and its durable posting,
/// and every lifecycle state uses a stable code.
/// </summary>
public sealed class DemoEp0034Us0002Tk0001Tests
{
    private const string ContractId = "contract:jobs:SPC-0002:0:0";

    private static PassengerContract Contract(string state = PassengerDeliveryStates.Aboard) => new(
        ContractId, "jobs:SPC-0002:0:0", StationJobTypes.Passenger, "SPC-0002", "SPC-0005", Count: 3,
        AcceptedAtGameTimeMs: 0, DeadlineGameTimeMs: 7_200_000, Payout: 420,
        Status: state == PassengerDeliveryStates.Delivered ? StationJobStatuses.Completed : StationJobStatuses.Active,
        PostingId: PassengerContractReasons.PostingIdFor(ContractId),
        PassengerIds: [ContractId + ":pax-1", ContractId + ":pax-2", ContractId + ":pax-3"], DeliveryState: state);

    [Fact]
    public void Payload_RoundTrips()
    {
        var delivered = Contract(PassengerDeliveryStates.Delivered) with { DeliveredAtGameTimeMs = 5_000_000, PaidCredits = 420 };
        var section = new PassengerContractsSnapshot([Contract(), delivered with { ContractId = "contract:jobs:SPC-0005:1:0" }],
            5_000_000, PassengersAboard: 3);
        var snapshot = new DemoCampaignSnapshot(3, 4, Transport: new DemoTransportSnapshot { PassengerContracts = section });

        var loaded = JsonSerializer.Deserialize<DemoCampaignSnapshot>(JsonSerializer.Serialize(snapshot))!.Transport!.PassengerContracts!;

        Assert.Equal(2, loaded.Contracts.Length);
        var expected = Contract();
        Assert.Equal(expected, loaded.Contracts[0] with { PassengerIds = expected.PassengerIds });
        Assert.Equal(Contract().PassengerIds.ToArray(), loaded.Contracts[0].PassengerIds.ToArray());
        Assert.Equal((5_000_000L, 420L), (loaded.Contracts[1].DeliveredAtGameTimeMs!.Value, loaded.Contracts[1].PaidCredits!.Value));
        Assert.Equal(3, loaded.PassengersAboard);
        Assert.Equal(5_000_000, loaded.ObservedAtGameTimeMs);
        Assert.Equal(ContractId, Assert.Single(loaded.Active).ContractId);
        Assert.Equal(PassengerDeliveryStates.Aboard, PassengerContractReasons.DeliveryState(loaded, ContractId));
        Assert.Equal(PassengerDeliveryStates.Delivered, PassengerContractReasons.DeliveryState(loaded, "contract:jobs:SPC-0005:1:0"));

        // Acceptance stays the single typed board command: no client price, deadline or passenger list.
        var json = JsonSerializer.Serialize(new AcceptJobOfferCommand("cmd-1", 3, "jobs:SPC-0002:0:0"));
        Assert.DoesNotContain("Payout", json);
        Assert.DoesNotContain("Passenger", json);
    }

    [Fact]
    public void LegacyPayload_UsesSafeDefaults()
    {
        // Earlier transport sections have no passenger contracts: unavailable, never delivered or paid.
        var legacy = JsonSerializer.Deserialize<DemoTransportSnapshot>("""{"PassengerCapacity":null,"StationJobs":null}""")!;
        Assert.Null(legacy.PassengerContracts);
        Assert.Equal(DemoCampaignReasons.Unavailable, PassengerContractReasons.DeliveryState(legacy.PassengerContracts, ContractId));

        // A partial section lists nothing and grants nothing.
        var partial = JsonSerializer.Deserialize<PassengerContractsSnapshot>("""{"ObservedAtGameTimeMs":5}""")!;
        Assert.True(partial.Contracts.IsDefaultOrEmpty);
        Assert.Empty(partial.Active);
        Assert.Equal(0, partial.PassengersAboard);
        Assert.Equal(DemoCampaignReasons.Unavailable, PassengerContractReasons.DeliveryState(partial, ContractId));

        // A contract with a missing state or ids is not a delivery, and default arrays serialize as empty lists.
        var unknownState = new PassengerContractsSnapshot([Contract() with { DeliveryState = null! }], 0, 0);
        Assert.Equal(DemoCampaignReasons.Unavailable, PassengerContractReasons.DeliveryState(unknownState, ContractId));
        var noIds = JsonSerializer.Deserialize<PassengerContract>(JsonSerializer.Serialize(Contract() with { PassengerIds = default }))!;
        Assert.True(noIds.PassengerIds.IsEmpty);
        Assert.Null(noIds.PaidCredits);
        Assert.Contains("\"Contracts\":[]", JsonSerializer.Serialize(new PassengerContractsSnapshot(default, 0, 0)));
    }

    [Fact]
    public void ReasonCodes_AreStable()
    {
        Assert.Equal(["aboard", "overdue", "delivered"],
            new[] { PassengerDeliveryStates.Aboard, PassengerDeliveryStates.Overdue, PassengerDeliveryStates.Delivered });
        Assert.False(PassengerDeliveryStates.IsKnown("Delivered"));
        Assert.Equal("passenger:" + ContractId, PassengerContractReasons.PostingIdFor(ContractId));
        Assert.Equal("passenger:", PassengerContractReasons.PostingPrefix);

        // Acceptance refusals reuse the board and capacity codes verbatim.
        Assert.Equal("no_free_cabins", PassengerCapacityReasons.NoFreeCabins);
        Assert.Equal("not_docked_at_origin", StationJobReasons.NotDockedAtOrigin);
        Assert.Equal("offer_already_accepted", StationJobReasons.OfferAlreadyAccepted);
    }
}
