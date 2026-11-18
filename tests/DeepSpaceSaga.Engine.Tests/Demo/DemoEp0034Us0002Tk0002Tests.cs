using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Nodes;
using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Engine.Content;
using DeepSpaceSaga.Engine.Scenario;

namespace DeepSpaceSaga.Engine.Tests.Demo;

/// <summary>
/// EP-0034-US-0002-TK-0002: accepting a passenger offer boards real passengers who hold cabins and eat rations;
/// a confirmed docking at the destination by the calendar deadline disembarks exactly them and pays once, with the
/// payout posted to the arrival voyage ledger. Duplicates, other stations, late arrival, Save/Load and repeated
/// snapshots never pay twice; corrupted contract data rejects Load atomically.
/// </summary>
public sealed class DemoEp0034Us0002Tk0002Tests
{
    private const string Ship = "SPC-0001";
    private const string Start = "SPC-0002";
    private const string Bridge = "MOD-PLAYER-BRIDGE-01";
    private const string Engine = "MOD-PLAYER-ENGINE-01";
    private const string Rations = "item.food-rations";
    private const string FirstOffer = "jobs:SPC-0002:0:0";
    private const long Ratio = 300;

    private static string Root([CallerFilePath] string source = "") =>
        Path.GetFullPath(Path.Combine(Path.GetDirectoryName(source)!, "..", "..", "..", "src", "DeepSpaceSaga.Client"));

    private static string Settings => Path.Combine(Root(), "Settings.json");
    private static string DemoScenario => Path.Combine(Root(), "Scenarios", "Demo", "scenario.json");

    private static string NewRun()
    {
        string directory = Path.Combine("D:/DeepSpaceSaga/artifacts/validation/EP-0034/us0002-tk0002-fixtures", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        return directory;
    }

    /// <summary>New demo game docked at the start station through the production Load path (as in US-0006).</summary>
    private static SimulationEngine NewDocked()
    {
        var engine = EngineContentLoader.CreateEngineFromScenarioFile(Settings, DemoScenario);
        engine.SetSpeed(SimulationSpeed.Speed0);
        var save = engine.CaptureSaveStateForTests(0, SimulationSpeed.Speed0, 0);
        var station = engine.CaptureSnapshotForTests(0, SimulationSpeed.Speed0, 0).Objects.Single(o => o.ObjectId == Start);
        var ship = save.GameState.SpaceObjects.Single(s => s.ObjectId == Ship) with
        {
            PositionX = station.X,
            PositionY = station.Y,
            SpeedMps = 0,
            IsDocked = true,
            DockedStationObjectId = Start,
            FirstPortFeeGameTimeMs = 0,
            NextPortFeeDueGameTimeMs = GameCalendar.DayMs
        };
        engine.LoadScenario(save with
        {
            GameState = save.GameState with { SpaceObjects = save.GameState.SpaceObjects.Select(s => s.ObjectId == Ship ? ship : s).ToArray() }
        }, isSave: true);
        engine.SetSpeed(SimulationSpeed.Speed0);
        return engine;
    }

    /// <summary>Command-driven flight in calendar time (physical = calendar / 300), as the trading voyage fixture does.</summary>
    private sealed class Flight(SimulationEngine engine)
    {
        private long _motion;
        private long _next;
        public AuthoritativeSnapshot Snapshot { get; private set; } = engine.CaptureSnapshotForTests(0, SimulationSpeed.Speed0, 0);
        public long GameTime => _motion * Ratio;

        public AuthoritativeSnapshot Capture() => Snapshot = engine.CaptureSnapshotForTests(_motion * Ratio, SimulationSpeed.Speed0, _motion);

        public AuthoritativeSnapshot AdvanceGame(long gameMs)
        {
            _motion += (gameMs + Ratio - 1) / Ratio;
            return Capture();
        }

        private CommandResult? Send(string module, string type, string? target = null)
        {
            var command = new PlayerCommand($"flight-{++_next}", (ulong)_next, Ship, module, type, TargetObjectId: target);
            engine.ReceiveCommand(command);
            return Capture().CommandResults.SingleOrDefault(r => r.CommandId == command.CommandId);
        }

        private ObjectMotionSnapshot Me => Snapshot.Objects.Single(o => o.ObjectId == Ship);

        private void WaitUntil(Func<bool> done, string phase)
        {
            for (int i = 0; i < 5000 && !done(); i++)
            {
                _motion += 1000;
                Capture();
            }
            Assert.True(done(), phase);
        }

        public void FlyTo(string destination)
        {
            Assert.Equal(CommandResultStatus.Executed, Send(Bridge, NavigationComputerCommandTypes.Undock, destination)?.Status);
            for (int burn = 0; burn < 3; burn++) // a modest three-burn pilot, far below the maximum speed
            {
                double before = Me.SpeedKmS;
                Assert.NotEqual(CommandResultStatus.Rejected, Send(Engine, ShipEngineCommandTypes.Accelerate)?.Status);
                WaitUntil(() => Me.SpeedKmS > before, "acceleration");
            }
            Send(Engine, NavigationComputerCommandTypes.Approach, destination);
            WaitUntil(() => Me.ApproachRoute is not null, "approach plan");
            _motion += (long)Math.Ceiling(Me.ApproachRoute!.DurationMs);
            Capture();
            Send(Engine, ShipEngineCommandTypes.SpeedSynchronization, destination);
            WaitUntil(() => Me.SpeedKmS == 0, "speed synchronization");
            if (Math.Abs(Me.Direction) > 1e-6)
            {
                Send(Engine, ShipEngineCommandTypes.DirectionSynchronization, destination);
                WaitUntil(() => Math.Abs(Me.Direction) < 1e-6, "direction synchronization");
            }
            Assert.Equal(CommandResultStatus.Executed, Send(Bridge, NavigationComputerCommandTypes.Dock, destination)?.Status);
            foreach (string choice in new[] { "truthful_id", "accept_fee", "continue" })
            {
                var dialogue = Assert.IsType<DialogueState>(Snapshot.ActiveDialogue);
                // Delivery waits for the confirmed docking: nothing is paid while the docking dialogue is open.
                Assert.All(Contracts(Snapshot).Contracts, c => Assert.Equal(StationJobStatuses.Active, c.Status));
                engine.ReceiveDialogueCommand(new($"dialogue-{++_next}", DialogueAction.Choose, dialogue.InstanceId, dialogue.Revision, choice));
                Capture();
            }
            Assert.Null(Snapshot.ActiveDialogue);
            Assert.True(Me.IsDocked);
            Assert.Equal(destination, Me.DockedStationObjectId);
        }
    }

    private static PassengerContractsSnapshot Contracts(AuthoritativeSnapshot snapshot) => snapshot.DemoCampaign!.Transport!.PassengerContracts!;
    private static PassengerCapacitySnapshot Capacity(AuthoritativeSnapshot snapshot) => snapshot.DemoCampaign!.Transport!.PassengerCapacity!;
    private static StationJobsSnapshot Board(AuthoritativeSnapshot snapshot) => snapshot.DemoCampaign!.Transport!.StationJobs!;

    private static long RationsAboard(AuthoritativeSnapshot snapshot) =>
        snapshot.InstalledModules.SelectMany(m => m.Cargo).Where(c => c.ItemTypeId == Rations).Sum(c => c.Quantity);

    private static string[] PassengersAboard(SimulationEngine engine, long gameTimeMs) =>
        (engine.CaptureSaveStateForTests(gameTimeMs, SimulationSpeed.Speed0, gameTimeMs / Ratio).GameState.SpaceObjects
            .Single(o => o.ObjectId == Ship).Passengers ?? []).Select(p => p.PassengerId).ToArray();

    private static SimulationEngine Reload(SimulationEngine engine, string directory, long gameTimeMs, Action<JsonNode>? edit = null)
    {
        var save = JsonNode.Parse(ScenarioLoader.Serialize(engine.CaptureSaveStateForTests(gameTimeMs, SimulationSpeed.Speed0, gameTimeMs / Ratio)))!;
        edit?.Invoke(save);
        string path = Path.Combine(directory, Guid.NewGuid().ToString("N") + ".json");
        File.WriteAllText(path, save.ToJsonString());
        var loaded = EngineContentLoader.CreateEngineFromSaveFile(Settings, path);
        loaded.SetSpeed(SimulationSpeed.Speed0);
        return loaded;
    }

    private static JsonNode PlayerShip(JsonNode save) =>
        save["gameState"]!["spaceObjects"]!.AsArray().Single(o => (string?)o!["objectId"] == Ship)!;

    [Fact]
    public void HappyPath_MatchesStory()
    {
        using var engine = NewDocked();
        var flight = new Flight(engine);
        var offer = Board(flight.Snapshot).Offers.Single(o => o.Id == FirstOffer);
        long credits = flight.Snapshot.PlayerCredits;
        Assert.Empty(Contracts(flight.Snapshot).Contracts);

        // Acceptance boards three real passengers in the same commit; nothing is charged.
        var accepted = engine.ExecuteDemoCampaign(new AcceptJobOfferCommand("pax-accept", 0, FirstOffer));
        Assert.True(accepted.Applied, accepted.Reason);
        var after = flight.Capture();
        var contract = Assert.Single(Contracts(after).Contracts);
        string id = "contract:" + FirstOffer;
        Assert.Equal((id, offer.DestinationStationId, 3, offer.Payout, StationJobStatuses.Active, PassengerDeliveryStates.Aboard),
            (contract.ContractId, contract.DestinationStationId, contract.Count, contract.Payout, contract.Status, contract.DeliveryState));
        Assert.Equal(offer.DeadlineDurationMs, contract.DeadlineGameTimeMs);
        Assert.Equal("passenger:" + id, contract.PostingId);
        Assert.Equal([id + ":pax-1", id + ":pax-2", id + ":pax-3"], contract.PassengerIds.ToArray());
        Assert.Equal(contract.PassengerIds.ToArray(), PassengersAboard(engine, 0));
        Assert.Equal(3, Contracts(after).PassengersAboard);
        Assert.Equal(new PassengerCapacitySnapshot(12, 7, 3, 0, 2, null), Capacity(after));
        Assert.Equal((10, 2), (after.CarrierOperations!.CabinReservation!.OccupiedCabins, after.CarrierOperations.CabinReservation.FreeCabins));
        Assert.Equal(credits, after.PlayerCredits);
        // Boarded passengers are not counted twice: the remaining two cabins still take a two-person offer.
        Assert.All(Board(after).Offers.Where(o => o.OriginStationId == Start && StationJobTypes.CarriesPassengers(o.Type)),
            o => Assert.Equal(o.Count > 2 ? PassengerCapacityReasons.NoFreeCabins : null, StationJobReasons.AcceptBlocker(Board(after), o.Id)));

        // Real flight to the destination: delivery disembarks exactly these people and pays once with a ledger posting.
        flight.FlyTo(offer.DestinationStationId);
        Assert.True(flight.GameTime <= contract.DeadlineGameTimeMs, $"flight {flight.GameTime} > deadline {contract.DeadlineGameTimeMs}");
        var arrived = flight.Snapshot;
        var delivered = Assert.Single(Contracts(arrived).Contracts);
        Assert.Equal((StationJobStatuses.Completed, PassengerDeliveryStates.Delivered, offer.Payout),
            (delivered.Status, delivered.DeliveryState, delivered.PaidCredits!.Value));
        Assert.Equal(0, Contracts(arrived).PassengersAboard);
        Assert.Empty(PassengersAboard(engine, flight.GameTime));
        Assert.Equal(StationJobStatuses.Completed, Assert.Single(Board(arrived).Obligations).Status);
        var ledger = engine.VoyageFinancesForTests[^1];
        Assert.Equal((offer.DestinationStationId, VoyageFinanceStates.AwaitingRealization, offer.Payout),
            (ledger.DestinationStationObjectId, ledger.State, ledger.PassengerPayoutCredits));
        Assert.Equal(ledger.CompletedGameTimeMs, delivered.DeliveredAtGameTimeMs);
        // The same flight without the contract differs by exactly the payout (docking fees are equal).
        using (var twin = NewDocked())
        {
            var twinFlight = new Flight(twin);
            twinFlight.FlyTo(offer.DestinationStationId);
            Assert.Equal(twinFlight.Snapshot.PlayerCredits + offer.Payout, arrived.PlayerCredits);
            Assert.Equal(0, twin.VoyageFinancesForTests[^1].PassengerPayoutCredits);
        }

        // Repeated snapshots and time at the station never pay again.
        long paid = arrived.PlayerCredits;
        string ledgerJson = JsonSerializer.Serialize(engine.VoyageFinancesForTests[^1]);
        flight.Capture();
        flight.AdvanceGame(GameCalendar.HourMs);
        Assert.Equal(paid, flight.Snapshot.PlayerCredits);
        Assert.Equal(ledgerJson, JsonSerializer.Serialize(engine.VoyageFinancesForTests[^1]));
    }

    [Fact]
    public void InvalidOrDuplicate_NoPartialMutation()
    {
        string directory = NewRun();
        try
        {
            using var engine = NewDocked();
            var start = engine.CaptureSnapshotForTests(0, SimulationSpeed.Speed0, 0);
            var offer = Board(start).Offers.Single(o => o.Id == FirstOffer);

            // Life support: the twin without passengers eats 7 rations per meal, the carrier with passengers 10.
            using var twin = NewDocked();
            var accepted = engine.ExecuteDemoCampaign(new AcceptJobOfferCommand("pax-1", 0, FirstOffer));
            Assert.True(accepted.Applied);

            // Docking at another cluster station (before the deadline) delivers nothing.
            long hour = GameCalendar.HourMs;
            long creditsAtHour = engine.CaptureSnapshotForTests(hour, SimulationSpeed.Speed0, hour / Ratio).PlayerCredits;
            string other = Board(start).Offers.Select(o => o.OriginStationId).First(s => s != Start && s != offer.DestinationStationId);
            using (var wrong = Reload(engine, directory, hour, save => PlayerShip(save)["dockedStationObjectId"] = other))
            {
                var there = wrong.CaptureSnapshotForTests(hour, SimulationSpeed.Speed0, hour / Ratio);
                Assert.Equal(PassengerDeliveryStates.Aboard, Assert.Single(Contracts(there).Contracts).DeliveryState);
                Assert.Equal(3, PassengersAboard(wrong, hour).Length);
                Assert.Equal(creditsAtHour, there.PlayerCredits);
            }

            // The duplicate command returns its receipt and boards nobody twice; a second accept is refused.
            Assert.Equal(accepted, engine.ExecuteDemoCampaign(new AcceptJobOfferCommand("pax-1", 0, FirstOffer)));
            Assert.Equal(StationJobReasons.OfferAlreadyAccepted,
                engine.ExecuteDemoCampaign(new AcceptJobOfferCommand("pax-2", accepted.Revision, FirstOffer)).Reason);
            Assert.Equal(3, PassengersAboard(engine, hour).Length);

            // Not enough cabins: a larger passenger offer is refused before anything is boarded or charged.
            var snapshot = engine.CaptureSnapshotForTests(hour, SimulationSpeed.Speed0, hour / Ratio);
            var tooBig = Board(snapshot).Offers.FirstOrDefault(o => o.OriginStationId == Start && StationJobTypes.CarriesPassengers(o.Type) && o.Count > 1);
            if (tooBig is not null)
            {
                var refused = engine.ExecuteDemoCampaign(new AcceptJobOfferCommand("pax-big", accepted.Revision, tooBig.Id));
                Assert.Equal((false, PassengerCapacityReasons.NoFreeCabins), (refused.Applied, refused.Reason));
                Assert.Equal(3, PassengersAboard(engine, hour).Length);
                Assert.Equal(snapshot.PlayerCredits, engine.CaptureSnapshotForTests(hour, SimulationSpeed.Speed0, hour / Ratio).PlayerCredits);
            }

            long meal = 13 * GameCalendar.HourMs; // one 12-hour meal boundary, before the first daily port fee
            long withPassengers = RationsAboard(start) - RationsAboard(engine.CaptureSnapshotForTests(meal, SimulationSpeed.Speed0, meal / Ratio));
            var twinStart = twin.CaptureSnapshotForTests(0, SimulationSpeed.Speed0, 0);
            long crewOnly = RationsAboard(twinStart) - RationsAboard(twin.CaptureSnapshotForTests(meal, SimulationSpeed.Speed0, meal / Ratio));
            Assert.Equal((7L, 10L), (crewOnly, withPassengers));

            // Arriving at the destination after the deadline pays nothing. Since US-0003 the contract has expired
            // (Failed, penalty in TransportFailures) and its passengers leave at this confirmed docking without payout.
            long late = Assert.Single(Contracts(snapshot).Contracts).DeadlineGameTimeMs + 1;
            long at = Math.Max(late, meal);
            // The carrier spends the time in space (a docked carrier would let them leave at the start station).
            using var fresh = NewDocked();
            Assert.True(fresh.ExecuteDemoCampaign(new AcceptJobOfferCommand("pax-late", 0, FirstOffer)).Applied);
            using var inSpace = Reload(fresh, directory, 0, save => { PlayerShip(save)["isDocked"] = false; PlayerShip(save).AsObject().Remove("dockedStationObjectId"); save["gameState"]!.AsObject().Remove("voyageState"); });
            using (var overdue = Reload(inSpace, directory, at, save =>
                   { PlayerShip(save)["isDocked"] = true; PlayerShip(save)["dockedStationObjectId"] = offer.DestinationStationId; }))
            {
                var lateSnapshot = overdue.CaptureSnapshotForTests(at, SimulationSpeed.Speed0, at / Ratio);
                var c = Assert.Single(Contracts(lateSnapshot).Contracts);
                Assert.Equal((StationJobStatuses.Failed, PassengerDeliveryStates.Failed, (long?)null), (c.Status, c.DeliveryState, c.PaidCredits));
                Assert.Equal(TransportFailureReasons.Expired, Assert.Single(lateSnapshot.DemoCampaign!.Transport!.TransportFailures!.Failures).Reason);
                Assert.Equal(offer.DestinationStationId, lateSnapshot.DemoCampaign.Transport.TransportFailures.Failures[0].DisembarkStationId);
                Assert.Empty(PassengersAboard(overdue, at));
            }
        }
        finally { Directory.Delete(directory, true); }
    }

    [Fact]
    public void SaveLoad_PreservesOutcome()
    {
        string directory = NewRun();
        try
        {
            using var engine = NewDocked();
            var offer = Board(engine.CaptureSnapshotForTests(0, SimulationSpeed.Speed0, 0)).Offers.Single(o => o.Id == FirstOffer);
            Assert.True(engine.ExecuteDemoCampaign(new AcceptJobOfferCommand("save-pax", 0, FirstOffer)).Applied);
            long noon = GameCalendar.HourMs; // before the contract deadline
            var before = engine.CaptureSnapshotForTests(noon, SimulationSpeed.Speed0, noon / Ratio);

            // The boarded passengers and the contract survive Save/Load unchanged.
            using (var loaded = Reload(engine, directory, noon))
            {
                var after = loaded.CaptureSnapshotForTests(noon, SimulationSpeed.Speed0, noon / Ratio);
                Assert.Equal(JsonSerializer.Serialize(Contracts(before)), JsonSerializer.Serialize(Contracts(after)));
                Assert.Equal(Capacity(before), Capacity(after));
                Assert.Equal(PassengersAboard(engine, noon), PassengersAboard(loaded, noon));
            }

            // Delivery on Load at the destination (docked through the Load path, no voyage): paid exactly once,
            // and a save written after the payout never pays again when loaded.
            string deliveredSave;
            using (var arrived = Reload(engine, directory, noon, save => PlayerShip(save)["dockedStationObjectId"] = offer.DestinationStationId))
            {
                var first = arrived.CaptureSnapshotForTests(noon, SimulationSpeed.Speed0, noon / Ratio);
                Assert.Equal(before.PlayerCredits + offer.Payout, first.PlayerCredits);
                Assert.Equal(PassengerDeliveryStates.Delivered, Assert.Single(Contracts(first).Contracts).DeliveryState);
                Assert.Empty(PassengersAboard(arrived, noon));
                deliveredSave = ScenarioLoader.Serialize(arrived.CaptureSaveStateForTests(noon, SimulationSpeed.Speed0, noon / Ratio));
                using var again = Reload(arrived, directory, noon);
                var reloaded = again.CaptureSnapshotForTests(noon, SimulationSpeed.Speed0, noon / Ratio);
                Assert.Equal(first.PlayerCredits, reloaded.PlayerCredits);
                Assert.Equal(JsonSerializer.Serialize(Contracts(first).Contracts), JsonSerializer.Serialize(Contracts(reloaded).Contracts));
            }

            // Corrupted contract data rejects the whole Load atomically.
            Assert.Contains("passenger contracts", Assert.ThrowsAny<Exception>(() => Reload(engine, directory, noon, save =>
                save["gameState"]!["demoCampaign"]!["passengerContracts"]![0]!["postingId"] = "passenger:other")).Message);
            Assert.Contains("passenger contracts", Assert.ThrowsAny<Exception>(() => Reload(engine, directory, noon, save =>
                PlayerShip(save)["passengers"] = new JsonArray())).Message);
            Assert.Contains("passenger contracts", Assert.ThrowsAny<Exception>(() => Reload(engine, directory, noon, save =>
                save["gameState"]!["demoCampaign"]!["passengerContracts"]![0]!["paidCredits"] = 999)).Message);
            var delivered = JsonNode.Parse(deliveredSave)!;
            string tamperedPath = Path.Combine(directory, "delivered-unpaid.json");
            delivered["gameState"]!["demoCampaign"]!["passengerContracts"]![0]!.AsObject().Remove("deliveredAtGameTimeMs");
            delivered["gameState"]!["demoCampaign"]!["passengerContracts"]![0]!.AsObject().Remove("paidCredits");
            File.WriteAllText(tamperedPath, delivered.ToJsonString());
            Assert.Contains("passenger contracts", Assert.ThrowsAny<Exception>(() => EngineContentLoader.CreateEngineFromSaveFile(Settings, tamperedPath)).Message);
            Assert.ThrowsAny<Exception>(() => Reload(engine, directory, noon, save =>
                save["gameState"]!["demoCampaign"]!["transport"] = new JsonObject { ["PassengerContracts"] = new JsonObject() }));
        }
        finally { Directory.Delete(directory, true); }
    }
}
