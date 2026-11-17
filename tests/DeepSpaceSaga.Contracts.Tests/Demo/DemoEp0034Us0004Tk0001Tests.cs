using System.Text.Json;

namespace DeepSpaceSaga.Contracts.Tests.Demo;

/// <summary>
/// EP-0034-US-0004-TK-0001: an accepted cargo order is an immutable, JSON-serializable projection inside the demo
/// transport section. It names the sealed item, its quantity and the hold mass it reserves, the route, the calendar
/// deadline, the payout and its durable posting; the hold split keeps contract cargo apart from the player's own
/// goods, and every hold state uses a stable code.
/// </summary>
public sealed class DemoEp0034Us0004Tk0001Tests
{
    private const string ContractId = "contract:jobs:SPC-0003:0:1";

    private static CargoContract Contract(string state = CargoHoldStates.Aboard) => new(
        ContractId, "jobs:SPC-0003:0:1", "item.steel", MassKg: 6_000, Quantity: 6, OriginStationId: "SPC-0003",
        DestinationStationId: "SPC-0005", AcceptedAtGameTimeMs: 1_000, DeadlineGameTimeMs: 9_001_000, Payout: 640,
        Status: state == CargoHoldStates.Delivered ? StationJobStatuses.Completed : StationJobStatuses.Active,
        PostingId: CargoContractReasons.PostingIdFor(ContractId), HoldState: state);

    private static CargoContractsSnapshot Section() => new(
        [Contract(), Contract(CargoHoldStates.Delivered) with
        {
            ContractId = "contract:jobs:SPC-0005:1:0", DeliveredAtGameTimeMs = 5_000_000, PaidCredits = 640
        }, Contract(CargoHoldStates.Returned) with
        {
            ContractId = "contract:jobs:SPC-0005:1:1", Status = StationJobStatuses.Cancelled,
            ReturnedAtGameTimeMs = 4_000_000, ReturnStationId = "SPC-0005"
        }],
        ObservedAtGameTimeMs: 5_000_000, ReservedMassKg: 6_000, HoldCapacityKg: 20_000, PlayerCargoMassKg: 3_000);

    [Fact]
    public void Payload_RoundTrips()
    {
        var snapshot = new DemoCampaignSnapshot(4, 6, Transport: new DemoTransportSnapshot { CargoContracts = Section() });

        var loaded = JsonSerializer.Deserialize<DemoCampaignSnapshot>(JsonSerializer.Serialize(snapshot))!.Transport!.CargoContracts!;

        Assert.Equal(Section().Contracts.ToArray(), loaded.Contracts.ToArray());
        Assert.Equal((5_000_000L, 6_000L, 20_000L, 3_000L),
            (loaded.ObservedAtGameTimeMs, loaded.ReservedMassKg, loaded.HoldCapacityKg, loaded.PlayerCargoMassKg));
        // The hold split is explicit: sealed contract mass, the player's own goods and what is still free for trade.
        Assert.Equal(11_000, loaded.FreeHoldKg);
        Assert.Equal(ContractId, Assert.Single(loaded.Active).ContractId);
        Assert.Equal(CargoHoldStates.Aboard, CargoContractReasons.HoldState(loaded, ContractId));
        Assert.Equal(CargoHoldStates.Delivered, CargoContractReasons.HoldState(loaded, "contract:jobs:SPC-0005:1:0"));
        Assert.Equal(CargoHoldStates.Returned, CargoContractReasons.HoldState(loaded, "contract:jobs:SPC-0005:1:1"));

        // Acceptance stays the single typed board command: no client price, item, mass or deadline.
        var json = JsonSerializer.Serialize(new AcceptJobOfferCommand("cmd-1", 4, "jobs:SPC-0003:0:1"));
        Assert.DoesNotContain("Payout", json);
        Assert.DoesNotContain("Mass", json);
        Assert.DoesNotContain("item.", json);
    }

    [Fact]
    public void LegacyPayload_UsesSafeDefaults()
    {
        // Earlier transport sections have no cargo section: unavailable, never delivered, paid or reserved.
        var legacy = JsonSerializer.Deserialize<DemoTransportSnapshot>(
            """{"PassengerCapacity":null,"StationJobs":null,"PassengerContracts":null,"TransportFailures":null}""")!;
        Assert.Null(legacy.CargoContracts);
        Assert.Equal(DemoCampaignReasons.Unavailable, CargoContractReasons.HoldState(legacy.CargoContracts, ContractId));

        // A partial section lists nothing, reserves nothing and grants no free hold it does not know.
        var partial = JsonSerializer.Deserialize<CargoContractsSnapshot>("""{"ObservedAtGameTimeMs":5}""")!;
        Assert.True(partial.Contracts.IsDefaultOrEmpty);
        Assert.Empty(partial.Active);
        Assert.Equal((0L, 0L, 0L), (partial.ReservedMassKg, partial.HoldCapacityKg, partial.FreeHoldKg));
        Assert.Equal(DemoCampaignReasons.Unavailable, CargoContractReasons.HoldState(partial, ContractId));

        // Free hold never goes negative, an unknown state is not guessed and default arrays serialize as empty lists.
        Assert.Equal(0, (Section() with { PlayerCargoMassKg = 30_000 }).FreeHoldKg);
        var unknown = new CargoContractsSnapshot([Contract() with { HoldState = "Delivered" }], 0, 0, 0, 0);
        Assert.Equal(DemoCampaignReasons.Unavailable, CargoContractReasons.HoldState(unknown, ContractId));
        Assert.Contains("\"Contracts\":[]", JsonSerializer.Serialize(new CargoContractsSnapshot(default, 0, 0, 0, 0)));
    }

    [Fact]
    public void ReasonCodes_AreStable()
    {
        Assert.Equal(["aboard", "overdue", "delivered", "return_pending", "returned", "lost"],
            new[]
            {
                CargoHoldStates.Aboard, CargoHoldStates.Overdue, CargoHoldStates.Delivered, CargoHoldStates.ReturnPending,
                CargoHoldStates.Returned, CargoHoldStates.Lost
            });
        Assert.True(CargoHoldStates.IsKnown(CargoHoldStates.ReturnPending));
        Assert.False(CargoHoldStates.IsKnown("Aboard"));
        // Sealed cargo occupies the hold while it is aboard, overdue or waiting to be returned.
        Assert.True(CargoHoldStates.OccupiesHold(CargoHoldStates.Aboard));
        Assert.True(CargoHoldStates.OccupiesHold(CargoHoldStates.Overdue));
        Assert.True(CargoHoldStates.OccupiesHold(CargoHoldStates.ReturnPending));
        Assert.False(CargoHoldStates.OccupiesHold(CargoHoldStates.Delivered));
        Assert.False(CargoHoldStates.OccupiesHold(CargoHoldStates.Returned));
        Assert.False(CargoHoldStates.OccupiesHold(CargoHoldStates.Lost));

        Assert.Equal("cargo:" + ContractId, CargoContractReasons.PostingIdFor(ContractId));
        Assert.Equal("cargo:", CargoContractReasons.PostingPrefix);

        // Acceptance and failure codes are shared with the board and the transport failure rules.
        Assert.Equal("insufficient_cargo_space", StationJobReasons.InsufficientCargoSpace);
        Assert.Equal("cargo", StationJobTypes.Cargo);
        Assert.Equal("penalty:" + ContractId, TransportFailureReasons.PostingIdFor(ContractId));
    }
}
