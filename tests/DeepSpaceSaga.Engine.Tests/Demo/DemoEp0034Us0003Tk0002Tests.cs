using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Nodes;
using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Engine.Content;
using DeepSpaceSaga.Engine.Scenario;

namespace DeepSpaceSaga.Engine.Tests.Demo;

/// <summary>
/// EP-0034-US-0003-TK-0002: an active transport obligation ends exactly once as Completed, Expired, Cancelled or
/// Lost. Arrival at the deadline still delivers; expiry happens only after the calendar deadline, independently of how
/// time is split. Cancellation and expiry cost 25% of the promised payout (rounded up): available Tokens are taken,
/// the rest becomes contract debt repaid from later contract payouts and never blocks peaceful work. Passengers of a
/// failed contract stay aboard until the next confirmed docking and only then leave, freeing their cabins. Duplicates,
/// repeated snapshots and Save/Load never repeat a penalty, and corrupted failure data rejects Load atomically.
/// </summary>
public sealed class DemoEp0034Us0003Tk0002Tests
{
    private const string Ship = "SPC-0001";
    private const string Start = "SPC-0002";
    private const string Bridge = "MOD-PLAYER-BRIDGE-01";
    private const string Engine = "MOD-PLAYER-ENGINE-01";
    private const string FirstOffer = "jobs:SPC-0002:0:0";
    private const string ContractId = "contract:" + FirstOffer;
    private const long Ratio = 300;

    private static string Root([CallerFilePath] string source = "") =>
        Path.GetFullPath(Path.Combine(Path.GetDirectoryName(source)!, "..", "..", "..", "src", "DeepSpaceSaga.Client"));

    private static string Settings => Path.Combine(Root(), "Settings.json");
    private static string DemoScenario => Path.Combine(Root(), "Scenarios", "Demo", "scenario.json");

    private static string NewRun()
    {
        string directory = Path.Combine("D:/DeepSpaceSaga/artifacts/validation/EP-0034/us0003-tk0002-fixtures", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        return directory;
    }

    /// <summary>New demo game docked at the start station through the production Load path (as in US-0002).</summary>
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

    /// <summary>Command-driven flight in calendar time (physical = calendar / 300), split into undock and arrival.</summary>
    private sealed class Flight(SimulationEngine engine)
    {
        private long _motion;
        private long _next;
        public AuthoritativeSnapshot Snapshot { get; private set; } = engine.CaptureSnapshotForTests(0, SimulationSpeed.Speed0, 0);
        public long GameTime => _motion * Ratio;

        public AuthoritativeSnapshot Capture() => Snapshot = engine.CaptureSnapshotForTests(_motion * Ratio, SimulationSpeed.Speed0, _motion);

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

        public void Undock(string destination)
        {
            Assert.Equal(CommandResultStatus.Executed, Send(Bridge, NavigationComputerCommandTypes.Undock, destination)?.Status);
            Assert.False(Me.IsDocked);
        }

        public void Arrive(string destination)
        {
            for (int burn = 0; burn < 3; burn++)
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
                // Nobody leaves the carrier while the docking dialogue is still open.
                Assert.True(Failures(Snapshot).Failures.All(f => f.DisembarkedAtGameTimeMs is null));
                engine.ReceiveDialogueCommand(new($"dialogue-{++_next}", DialogueAction.Choose, dialogue.InstanceId, dialogue.Revision, choice));
                Capture();
            }
            Assert.Null(Snapshot.ActiveDialogue);
            Assert.True(Me.IsDocked);
        }
    }

    private static TransportFailuresSnapshot Failures(AuthoritativeSnapshot snapshot) => snapshot.DemoCampaign!.Transport!.TransportFailures!;
    private static PassengerContractsSnapshot Contracts(AuthoritativeSnapshot snapshot) => snapshot.DemoCampaign!.Transport!.PassengerContracts!;
    private static PassengerCapacitySnapshot Capacity(AuthoritativeSnapshot snapshot) => snapshot.DemoCampaign!.Transport!.PassengerCapacity!;
    private static StationJobsSnapshot Board(AuthoritativeSnapshot snapshot) => snapshot.DemoCampaign!.Transport!.StationJobs!;

    private static AuthoritativeSnapshot At(SimulationEngine engine, long gameTimeMs) =>
        engine.CaptureSnapshotForTests(gameTimeMs, SimulationSpeed.Speed0, gameTimeMs / Ratio);

    private static long Penalty(long payout) => (payout * 250 + 999) / 1000;

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

    private static void InSpace(JsonNode save)
    {
        PlayerShip(save)["isDocked"] = false;
        PlayerShip(save).AsObject().Remove("dockedStationObjectId");
        save["gameState"]!.AsObject().Remove("voyageState"); // a parked carrier in space has no voyage
    }

    private static Action<JsonNode> DockedAt(string station) => save => PlayerShip(save)["dockedStationObjectId"] = station;

    [Fact]
    public void HappyPath_MatchesStory()
    {
        using var engine = NewDocked();
        var flight = new Flight(engine);
        var offer = Board(flight.Snapshot).Offers.Single(o => o.Id == FirstOffer);
        int freeBefore = Capacity(flight.Snapshot).FreeCabins;
        Assert.Empty(Failures(flight.Snapshot).Failures);
        Assert.True(engine.ExecuteDemoCampaign(new AcceptJobOfferCommand("accept", 0, FirstOffer)).Applied);

        // Consequences are published before the player acts.
        var accepted = flight.Capture();
        var terms = Assert.Single(Failures(accepted).Terms);
        long penalty = Penalty(offer.Payout);
        Assert.Equal(new TransportCancellationTerms(ContractId, penalty, penalty, offer.DeadlineDurationMs, null), terms);
        Assert.Equal(0, Failures(accepted).ContractDebt);

        // Cancellation in open space: the penalty is paid, nobody is put out into vacuum.
        flight.Undock(offer.DestinationStationId);
        long credits = flight.Snapshot.PlayerCredits;
        long revision = flight.Snapshot.DemoCampaign!.Revision;
        var cancelled = engine.ExecuteDemoCampaign(new CancelTransportCommand("cancel", revision, ContractId));
        Assert.True(cancelled.Applied, cancelled.Reason);
        var afterCancel = flight.Capture();
        var failure = Assert.Single(Failures(afterCancel).Failures);
        Assert.Equal((TransportFailureReasons.Cancelled, penalty, penalty, 0L, true, flight.GameTime),
            (failure.Reason, failure.PenaltyCredits, failure.PenaltyPaid, failure.Debt, failure.DisembarkPending, failure.FailedAtGameTimeMs));
        Assert.Equal(TransportFailureReasons.PostingIdFor(ContractId), failure.PostingId);
        Assert.Equal(credits - penalty, afterCancel.PlayerCredits);
        Assert.Equal(StationJobStatuses.Cancelled, Assert.Single(Board(afterCancel).Obligations).Status);
        var contract = Assert.Single(Contracts(afterCancel).Contracts);
        Assert.Equal((StationJobStatuses.Cancelled, PassengerDeliveryStates.Failed, 3), (contract.Status, contract.DeliveryState, Contracts(afterCancel).PassengersAboard));
        Assert.Equal(contract.PassengerIds.ToArray(), PassengersAboard(engine, flight.GameTime));
        Assert.Equal(freeBefore - 3, Capacity(afterCancel).FreeCabins);
        Assert.Empty(Failures(afterCancel).Terms);
        Assert.Equal(penalty, engine.VoyageFinancesForTests[^1].PassengerPenaltyCredits);

        // The duplicate returns its receipt; a second cancellation is refused; no second penalty.
        Assert.Equal(cancelled, engine.ExecuteDemoCampaign(new CancelTransportCommand("cancel", revision, ContractId)));
        Assert.Equal(TransportFailureReasons.ContractNotActive,
            engine.ExecuteDemoCampaign(new CancelTransportCommand("cancel-2", cancelled.Revision, ContractId)).Reason);
        Assert.Equal(afterCancel.PlayerCredits, flight.Capture().PlayerCredits);

        // The next confirmed human docking lets them leave: cabins are free again, nothing is paid.
        flight.Arrive(offer.DestinationStationId);
        var docked = flight.Snapshot;
        var landed = Assert.Single(Failures(docked).Failures);
        Assert.Equal((false, offer.DestinationStationId), (landed.DisembarkPending, landed.DisembarkStationId));
        Assert.True(landed.DisembarkedAtGameTimeMs >= landed.FailedAtGameTimeMs);
        Assert.Empty(PassengersAboard(engine, flight.GameTime));
        Assert.Equal(0, Contracts(docked).PassengersAboard);
        Assert.Equal(freeBefore, Capacity(docked).FreeCabins);
        Assert.Equal(0, engine.VoyageFinancesForTests[^1].PassengerPayoutCredits);
        using (var twin = NewDocked())
        {
            var twinFlight = new Flight(twin);
            twinFlight.Undock(offer.DestinationStationId);
            twinFlight.Arrive(offer.DestinationStationId);
            Assert.Equal(twinFlight.Snapshot.PlayerCredits - penalty, docked.PlayerCredits);
        }
        long settled = docked.PlayerCredits;
        flight.Capture();
        Assert.Equal(settled, flight.Snapshot.PlayerCredits);
        Assert.Equal(JsonSerializer.Serialize(landed), JsonSerializer.Serialize(Assert.Single(Failures(flight.Snapshot).Failures)));
    }

    [Fact]
    public void InvalidOrDuplicate_NoPartialMutation()
    {
        string directory = NewRun();
        try
        {
            using var engine = NewDocked();
            var offer = Board(At(engine, 0)).Offers.Single(o => o.Id == FirstOffer);
            long penalty = Penalty(offer.Payout);

            // Refusals change nothing.
            Assert.Equal(TransportFailureReasons.ContractNotFound,
                engine.ExecuteDemoCampaign(new CancelTransportCommand("none", 0, ContractId)).Reason);
            var accepted = engine.ExecuteDemoCampaign(new AcceptJobOfferCommand("accept", 0, FirstOffer));
            Assert.True(accepted.Applied, accepted.Reason);
            Assert.Equal(DemoCampaignReasons.StaleRevision,
                engine.ExecuteDemoCampaign(new CancelTransportCommand("stale", 0, ContractId)).Reason);
            var start = At(engine, 0);
            Assert.Equal(StationJobStatuses.Active, Assert.Single(Board(start).Obligations).Status);
            Assert.Empty(Failures(start).Failures);
            long deadline = Assert.Single(Board(start).Obligations).DeadlineGameTimeMs;

            // Every branch starts from the same accepted state at t = 0 (time never runs backwards in one engine).
            using var stepped = Reload(engine, directory, 0, InSpace);
            using var jumped = Reload(engine, directory, 0, InSpace);
            using var space = Reload(engine, directory, 0, InSpace);
            using var poor = Reload(engine, directory, 0, save => save["gameState"]!["playerTokens"] = 10);

            // A-01 boundary: arrival exactly at the deadline delivers and pays; one millisecond later expires.
            using (var onTime = Reload(engine, directory, deadline, DockedAt(offer.DestinationStationId)))
            {
                var s = At(onTime, deadline);
                Assert.Equal(PassengerDeliveryStates.Delivered, Assert.Single(Contracts(s).Contracts).DeliveryState);
                Assert.Empty(Failures(s).Failures);
            }
            long creditsAtDeadline = At(space, deadline).PlayerCredits;
            using (var late = Reload(space, directory, deadline + 1, save =>
                   { PlayerShip(save)["isDocked"] = true; PlayerShip(save)["dockedStationObjectId"] = offer.DestinationStationId; }))
            {
                var s = At(late, deadline + 1);
                var f = Assert.Single(Failures(s).Failures);
                Assert.Equal((TransportFailureReasons.Expired, deadline + 1, penalty, false, offer.DestinationStationId),
                    (f.Reason, f.FailedAtGameTimeMs, f.PenaltyCredits, f.DisembarkPending, f.DisembarkStationId));
                Assert.Equal(StationJobStatuses.Failed, Assert.Single(Board(s).Obligations).Status);
                Assert.Equal(PassengerDeliveryStates.Failed, Assert.Single(Contracts(s).Contracts).DeliveryState);
                Assert.Empty(PassengersAboard(late, deadline + 1));
                Assert.Equal(creditsAtDeadline - penalty, s.PlayerCredits);
            }

            // Pause and time split: expiry is the same whether time arrives in one step or hour by hour,
            // and repeated snapshots (also at a stopped clock) never charge again.
            long end = deadline + 2 * GameCalendar.HourMs;
            for (long t = 0; t < deadline; t += GameCalendar.HourMs / 4)
                Assert.Empty(Failures(At(stepped, t)).Failures);
            Assert.Empty(Failures(At(stepped, deadline)).Failures);
            Assert.Empty(Failures(At(stepped, deadline)).Failures);
            for (long t = deadline + 1; t <= end; t += GameCalendar.HourMs / 2) At(stepped, t);
            var a = At(stepped, end);
            var b = At(jumped, end);
            Assert.Equal(JsonSerializer.Serialize(Failures(a)), JsonSerializer.Serialize(Failures(b)));
            Assert.Equal(a.PlayerCredits, b.PlayerCredits);
            var pending = Assert.Single(Failures(b).Failures);
            Assert.Equal((TransportFailureReasons.Expired, deadline + 1, true), (pending.Reason, pending.FailedAtGameTimeMs, pending.DisembarkPending));
            Assert.Equal(3, PassengersAboard(jumped, end).Length);
            Assert.Equal(a.PlayerCredits, At(stepped, end).PlayerCredits);

            // Debt: with too few Tokens the rest is owed, peaceful work stays open, and a later payout repays it.
            var poorStart = At(poor, 0);
            Assert.Equal(10, poorStart.PlayerCredits);
            var cancel = poor.ExecuteDemoCampaign(new CancelTransportCommand("poor-cancel", poorStart.DemoCampaign!.Revision, ContractId));
            Assert.True(cancel.Applied, cancel.Reason);
            var owed = At(poor, 0);
            var debtFailure = Assert.Single(Failures(owed).Failures);
            Assert.Equal((penalty, 10L, penalty - 10, false, Start),
                (debtFailure.PenaltyCredits, debtFailure.PenaltyPaid, debtFailure.Debt, debtFailure.DisembarkPending, debtFailure.DisembarkStationId));
            Assert.Equal(0, owed.PlayerCredits);
            Assert.Equal(penalty - 10, Failures(owed).ContractDebt);
            Assert.Empty(PassengersAboard(poor, 0)); // docked: they left at once
            var next = Board(owed).Offers.First(o => o.OriginStationId == Start && StationJobTypes.CarriesPassengers(o.Type) &&
                StationJobReasons.AcceptBlocker(Board(owed), o.Id) is null);
            var second = poor.ExecuteDemoCampaign(new AcceptJobOfferCommand("poor-accept", cancel.Revision, next.Id));
            Assert.True(second.Applied, second.Reason);
            using var repaid = Reload(poor, directory, GameCalendar.HourMs / 2, DockedAt(next.DestinationStationId));
            var paid = At(repaid, GameCalendar.HourMs / 2);
            long repayment = Math.Min(penalty - 10, next.Payout);
            var settledFailure = Assert.Single(Failures(paid).Failures);
            Assert.Equal((repayment, penalty - 10 - repayment), (settledFailure.RepaidCredits, settledFailure.Debt));
            Assert.Equal(next.Payout - repayment, paid.PlayerCredits - At(poor, GameCalendar.HourMs / 2).PlayerCredits);
            Assert.Equal(penalty, settledFailure.PenaltyPaid + settledFailure.RepaidCredits + settledFailure.Debt);
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
            Assert.True(engine.ExecuteDemoCampaign(new AcceptJobOfferCommand("accept", 0, FirstOffer)).Applied);
            using var space = Reload(engine, directory, 0, InSpace);
            long t = GameCalendar.HourMs / 4;
            var cancel = space.ExecuteDemoCampaign(new CancelTransportCommand("cancel", 1, ContractId));
            Assert.True(cancel.Applied, cancel.Reason);
            var before = At(space, t);
            Assert.True(Assert.Single(Failures(before).Failures).DisembarkPending);
            Assert.True(before.DemoCampaign!.CommitSequence >= 2);

            // Pending passengers, the penalty and the receipt survive Save/Load; the duplicate does not charge again.
            using (var loaded = Reload(space, directory, t))
            {
                var after = At(loaded, t);
                Assert.Equal(JsonSerializer.Serialize(Failures(before)), JsonSerializer.Serialize(Failures(after)));
                Assert.Equal(before.PlayerCredits, after.PlayerCredits);
                Assert.Equal(PassengersAboard(space, t), PassengersAboard(loaded, t));
                Assert.Equal(cancel, loaded.ExecuteDemoCampaign(new CancelTransportCommand("cancel", 1, ContractId)));
                Assert.Equal(before.PlayerCredits, At(loaded, t).PlayerCredits);
            }

            // A docking reached through Load lets them leave; that state survives another Load.
            using (var docked = Reload(space, directory, t, save => { PlayerShip(save)["isDocked"] = true; PlayerShip(save)["dockedStationObjectId"] = Start; }))
            {
                var landed = At(docked, t);
                Assert.Equal((false, Start), (Assert.Single(Failures(landed).Failures).DisembarkPending, Failures(landed).Failures[0].DisembarkStationId));
                Assert.Empty(PassengersAboard(docked, t));
                using var again = Reload(docked, directory, t);
                Assert.Equal(JsonSerializer.Serialize(Failures(landed)), JsonSerializer.Serialize(Failures(At(again, t))));
                Assert.Contains("transport failures", Assert.ThrowsAny<Exception>(() => Reload(docked, directory, t, save =>
                    PlayerShip(save)["passengers"] = JsonNode.Parse(JsonSerializer.Serialize(new[] { new { passengerId = ContractId + ":pax-1", displayName = "x" } })))).Message);
            }

            // A terminal carrier closes active obligations as lost, without a penalty or a second command.
            using (var lost = Reload(engine, directory, t, save => { InSpace(save); PlayerShip(save)["isDestroyed"] = true; }))
            {
                var wreck = At(lost, t);
                var f = Assert.Single(Failures(wreck).Failures);
                Assert.Equal((TransportFailureReasons.CarrierLost, 0L, false), (f.Reason, f.PenaltyCredits, f.DisembarkPending));
                Assert.Equal(StationJobStatuses.Failed, Assert.Single(Board(wreck).Obligations).Status);
            }

            // Corrupted failure data rejects the whole Load atomically.
            Assert.Contains("transport failures", Assert.ThrowsAny<Exception>(() => Reload(space, directory, t, save =>
                save["gameState"]!["demoCampaign"]!["transportFailures"]![0]!["penaltyPaid"] = 1)).Message);
            Assert.Contains("transport failures", Assert.ThrowsAny<Exception>(() => Reload(space, directory, t, save =>
                save["gameState"]!["demoCampaign"]!["transportFailures"] = new JsonArray())).Message);
            Assert.Contains("transport failures", Assert.ThrowsAny<Exception>(() => Reload(space, directory, t, save =>
                PlayerShip(save)["passengers"] = new JsonArray())).Message);
            Assert.Contains("transport failures", Assert.ThrowsAny<Exception>(() => Reload(space, directory, t, save =>
                save["gameState"]!["demoCampaign"]!["transportFailures"]![0]!["reason"] = "expired")).Message);
            Assert.ThrowsAny<Exception>(() => Reload(space, directory, t, save =>
                save["gameState"]!["demoCampaign"]!["transport"] = new JsonObject { ["TransportFailures"] = new JsonObject() }));
        }
        finally { Directory.Delete(directory, true); }
    }
}
