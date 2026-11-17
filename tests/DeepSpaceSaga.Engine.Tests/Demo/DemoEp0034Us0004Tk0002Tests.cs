using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Nodes;
using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Engine.Content;
using DeepSpaceSaga.Engine.Scenario;

namespace DeepSpaceSaga.Engine.Tests.Demo;

/// <summary>
/// EP-0034-US-0004-TK-0002: accepting a cargo order seals its real mass into the hold apart from the player's own
/// goods; ordinary trade can neither sell it nor use its space. A confirmed docking at the destination by the calendar
/// deadline frees the hold and pays once (repaying contract debt first, like passenger payouts). Cancelled or expired
/// cargo stays in the hold without a payout until the next human station takes it back; a lost carrier loses it.
/// Penalties are the shared US-0003 rule. Duplicates, repeated snapshots and Save/Load never repeat money or cargo,
/// and corrupted cargo data rejects Load atomically.
/// </summary>
public sealed class DemoEp0034Us0004Tk0002Tests
{
    private const string Ship = "SPC-0001";
    /// <summary>Origin with cargo offers in the first publication (the start station publishes passengers only).</summary>
    private const string Start = "SPC-0006";
    private const string Bridge = "MOD-PLAYER-BRIDGE-01";
    private const string Engine = "MOD-PLAYER-ENGINE-01";
    private const string Hold = "MOD-PLAYER-CARGO-01";
    private const long Ratio = 300;

    private static string Root([CallerFilePath] string source = "") =>
        Path.GetFullPath(Path.Combine(Path.GetDirectoryName(source)!, "..", "..", "..", "src", "DeepSpaceSaga.Client"));

    private static string Settings => Path.Combine(Root(), "Settings.json");
    private static string DemoScenario => Path.Combine(Root(), "Scenarios", "Demo", "scenario.json");

    private static string NewRun()
    {
        string directory = Path.Combine("D:/DeepSpaceSaga/artifacts/validation/EP-0034/us0004-tk0002-fixtures", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        return directory;
    }

    /// <summary>New demo game docked at the start station through the production Load path (as in US-0002/US-0003).</summary>
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
                // Nothing is handed over while the docking dialogue is still open.
                Assert.True(Cargo(Snapshot).Contracts.All(c => c.HoldState != CargoHoldStates.Delivered));
                engine.ReceiveDialogueCommand(new($"dialogue-{++_next}", DialogueAction.Choose, dialogue.InstanceId, dialogue.Revision, choice));
                Capture();
            }
            Assert.Null(Snapshot.ActiveDialogue);
            Assert.True(Me.IsDocked);
        }
    }

    private static CargoContractsSnapshot Cargo(AuthoritativeSnapshot snapshot) => snapshot.DemoCampaign!.Transport!.CargoContracts!;
    private static StationJobsSnapshot Board(AuthoritativeSnapshot snapshot) => snapshot.DemoCampaign!.Transport!.StationJobs!;
    private static TransportFailuresSnapshot Failures(AuthoritativeSnapshot snapshot) => snapshot.DemoCampaign!.Transport!.TransportFailures!;

    private static AuthoritativeSnapshot At(SimulationEngine engine, long gameTimeMs) =>
        engine.CaptureSnapshotForTests(gameTimeMs, SimulationSpeed.Speed0, gameTimeMs / Ratio);

    private static long Penalty(long payout) => (payout * 250 + 999) / 1000;

    /// <summary>Acceptable cargo offers of the origin, shortest deadline first (deterministic from the scenario seed).</summary>
    private static JobOffer[] CargoOffers(AuthoritativeSnapshot snapshot) => Board(snapshot).Offers
        .Where(o => o.Type == StationJobTypes.Cargo && o.OriginStationId == Start && StationJobReasons.AcceptBlocker(Board(snapshot), o.Id) is null)
        .OrderBy(o => o.DeadlineDurationMs).ThenBy(o => o.Id, StringComparer.Ordinal).ToArray();

    private static JobOffer CargoOffer(AuthoritativeSnapshot snapshot) => CargoOffers(snapshot)[0];

    private static string HoldStacks(SimulationEngine engine, long gameTimeMs) => JsonSerializer.Serialize(
        engine.CaptureSaveStateForTests(gameTimeMs, SimulationSpeed.Speed0, gameTimeMs / Ratio).GameState.SpaceObjects
            .Single(o => o.ObjectId == Ship).Modules!.Single(m => m.ModuleId == Hold).Cargo);

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

    private static JsonNode DemoCampaign(JsonNode save) => save["gameState"]!["demoCampaign"]!;

    private static void InSpace(JsonNode save)
    {
        PlayerShip(save)["isDocked"] = false;
        PlayerShip(save).AsObject().Remove("dockedStationObjectId");
        save["gameState"]!.AsObject().Remove("voyageState"); // a parked carrier in space has no voyage
    }

    private static Action<JsonNode> DockedAt(string station) => save =>
    {
        PlayerShip(save)["isDocked"] = true;
        PlayerShip(save)["dockedStationObjectId"] = station;
    };

    /// <summary>Adds the player's own iron ore (1 kg per unit) so exactly <paramref name="freeKg"/> of the hold stays free.</summary>
    private static Action<JsonNode> FreeHold(CargoContractsSnapshot hold, long freeKg) => save =>
        PlayerShip(save)["modules"]!.AsArray().Single(m => (string?)m!["moduleId"] == Hold)!["cargo"]!.AsArray().Add(new JsonObject
        {
            ["itemTypeId"] = "item.iron-ore",
            ["quantity"] = hold.HoldCapacityKg - hold.ReservedMassKg - hold.PlayerCargoMassKg - freeKg,
            ["costBasisCredits"] = 0,
            ["acquisitionSources"] = new JsonArray("dialogue-grant")
        });

    [Fact]
    public void HappyPath_MatchesStory()
    {
        using var engine = NewDocked();
        var flight = new Flight(engine);
        var before = Cargo(flight.Snapshot);
        Assert.Empty(before.Contracts);
        Assert.Equal(0, before.ReservedMassKg);
        Assert.True(before.HoldCapacityKg > 0);
        var offer = CargoOffer(flight.Snapshot);
        string contractId = "contract:" + offer.Id;
        string stacks = HoldStacks(engine, 0);

        var accepted = engine.ExecuteDemoCampaign(new AcceptJobOfferCommand("accept", 0, offer.Id));
        Assert.True(accepted.Applied, accepted.Reason);

        // The sealed cargo reserves its real mass, separately from the player's own goods.
        var loaded = flight.Capture();
        var contract = Assert.Single(Cargo(loaded).Contracts);
        Assert.Equal((contractId, offer.Id, offer.ItemTypeId!, offer.MassKg, (long)offer.Count, offer.OriginStationId, offer.DestinationStationId),
            (contract.ContractId, contract.OfferId, contract.ItemTypeId, contract.MassKg, contract.Quantity, contract.OriginStationId, contract.DestinationStationId));
        Assert.Equal((offer.Payout, StationJobStatuses.Active, CargoHoldStates.Aboard, CargoContractReasons.PostingIdFor(contractId)),
            (contract.Payout, contract.Status, contract.HoldState, contract.PostingId));
        Assert.Equal(offer.DeadlineDurationMs, contract.DeadlineGameTimeMs - contract.AcceptedAtGameTimeMs);
        Assert.Equal((offer.MassKg, before.PlayerCargoMassKg, before.HoldCapacityKg),
            (Cargo(loaded).ReservedMassKg, Cargo(loaded).PlayerCargoMassKg, Cargo(loaded).HoldCapacityKg));
        Assert.Equal(before.FreeHoldKg - offer.MassKg, Cargo(loaded).FreeHoldKg);
        Assert.Equal(stacks, HoldStacks(engine, 0)); // not a player stack: nothing appears in the module cargo

        // Ordinary trade cannot sell sealed cargo.
        var sell = engine.GetTradeQuote(new("sell-sealed", Ship, Hold, TradeCommandTypes.Sell, offer.ItemTypeId!, 1));
        Assert.Equal(0, sell.ExecutableQuantity);
        Assert.NotNull(sell.DisabledReason);

        // Duplicate acceptance returns its receipt and reserves nothing more.
        Assert.Equal(accepted, engine.ExecuteDemoCampaign(new AcceptJobOfferCommand("accept", 0, offer.Id)));
        Assert.Equal(offer.MassKg, Cargo(flight.Capture()).ReservedMassKg);

        // A real flight reaches the destination before the calendar deadline (content calibration, A-01).
        flight.Undock(offer.DestinationStationId);
        Assert.Equal(CargoHoldStates.Aboard, Assert.Single(Cargo(flight.Snapshot).Contracts).HoldState);
        flight.Arrive(offer.DestinationStationId);
        Assert.True(flight.GameTime <= contract.DeadlineGameTimeMs,
            $"arrival {flight.GameTime} ms after deadline {contract.DeadlineGameTimeMs} ms");

        // Handed over once: the hold is free again and the payout is credited and posted to the arrival voyage.
        var docked = flight.Snapshot;
        var delivered = Assert.Single(Cargo(docked).Contracts);
        Assert.Equal((StationJobStatuses.Completed, CargoHoldStates.Delivered, offer.Payout),
            (delivered.Status, delivered.HoldState, delivered.PaidCredits!.Value));
        Assert.True(delivered.DeliveredAtGameTimeMs <= delivered.DeadlineGameTimeMs);
        Assert.Equal(0, Cargo(docked).ReservedMassKg);
        Assert.Equal(StationJobStatuses.Completed, Assert.Single(Board(docked).Obligations).Status);
        Assert.Equal(offer.Payout, engine.VoyageFinancesForTests[^1].PassengerPayoutCredits);
        Assert.Empty(Failures(docked).Failures);
        using (var twin = NewDocked())
        {
            var twinFlight = new Flight(twin);
            twinFlight.Undock(offer.DestinationStationId);
            twinFlight.Arrive(offer.DestinationStationId);
            Assert.Equal(twinFlight.Snapshot.PlayerCredits + offer.Payout, docked.PlayerCredits);
        }

        // Repeated snapshots never pay again.
        long settled = docked.PlayerCredits;
        flight.Capture();
        Assert.Equal(settled, flight.Snapshot.PlayerCredits);
        Assert.Equal(JsonSerializer.Serialize(delivered), JsonSerializer.Serialize(Assert.Single(Cargo(flight.Snapshot).Contracts)));
    }

    [Fact]
    public void InvalidOrDuplicate_NoPartialMutation()
    {
        string directory = NewRun();
        try
        {
            using var engine = NewDocked();
            var start = At(engine, 0);
            var offer = CargoOffer(start);
            string contractId = "contract:" + offer.Id;
            long penalty = Penalty(offer.Payout);

            // Not enough free hold: refused with a stable code and no partial reservation.
            using (var full = Reload(engine, directory, 0, FreeHold(Cargo(start), offer.MassKg - 1)))
            {
                var s = At(full, 0);
                Assert.Equal(StationJobReasons.InsufficientCargoSpace, StationJobReasons.AcceptBlocker(Board(s), offer.Id));
                var refused = full.ExecuteDemoCampaign(new AcceptJobOfferCommand("full", 0, offer.Id));
                Assert.Equal((false, StationJobReasons.InsufficientCargoSpace, 0L), (refused.Applied, refused.Reason, refused.Revision));
                Assert.Empty(Cargo(At(full, 0)).Contracts);
                Assert.Equal(0, Cargo(At(full, 0)).ReservedMassKg);
                Assert.Empty(Board(At(full, 0)).Obligations);
            }

            // Exactly enough: accepted, and ordinary trade may not use the reserved space.
            using (var tight = Reload(engine, directory, 0, FreeHold(Cargo(start), offer.MassKg + 5)))
            {
                Assert.True(tight.ExecuteDemoCampaign(new AcceptJobOfferCommand("tight", 0, offer.Id)).Applied);
                Assert.Equal(5, Cargo(At(tight, 0)).FreeHoldKg);
                var buy = tight.GetTradeQuote(new("buy-ore", Ship, Hold, TradeCommandTypes.Buy, "item.iron-ore", 6));
                Assert.Equal((5L, 0L, CommandReasonCodes.CargoCapacityExceeded), (buy.MaximumQuantity, buy.ExecutableQuantity, buy.DisabledReason));
                var five = tight.GetTradeQuote(new("buy-five", Ship, Hold, TradeCommandTypes.Buy, "item.iron-ore", 5));
                Assert.Equal(5, five.ExecutableQuantity);
            }

            Assert.True(engine.ExecuteDemoCampaign(new AcceptJobOfferCommand("accept", 0, offer.Id)).Applied);
            var deadline = Assert.Single(Cargo(At(engine, 0)).Contracts).DeadlineGameTimeMs;
            // Every branch starts from the same accepted state at t = 0 (time never runs backwards in one engine).
            using var space = Reload(engine, directory, 0, InSpace);
            using var poor = Reload(engine, directory, 0, save => save["gameState"]!["playerTokens"] = 10);

            // A-01 boundary: docked at the destination exactly at the deadline delivers; one millisecond later expires,
            // and the expired cargo is taken back at that human station without a payout.
            using (var onTime = Reload(space, directory, deadline, DockedAt(offer.DestinationStationId)))
            {
                var s = At(onTime, deadline);
                Assert.Equal(CargoHoldStates.Delivered, Assert.Single(Cargo(s).Contracts).HoldState);
                Assert.Empty(Failures(s).Failures);
            }
            long creditsAtDeadline = At(space, deadline).PlayerCredits;
            Assert.Equal(CargoHoldStates.Aboard, Assert.Single(Cargo(At(space, deadline)).Contracts).HoldState);
            using (var late = Reload(space, directory, deadline + 1, DockedAt(offer.DestinationStationId)))
            {
                var s = At(late, deadline + 1);
                var f = Assert.Single(Failures(s).Failures);
                Assert.Equal((TransportFailureReasons.Expired, deadline + 1, penalty), (f.Reason, f.FailedAtGameTimeMs, f.PenaltyCredits));
                var c = Assert.Single(Cargo(s).Contracts);
                Assert.Equal((StationJobStatuses.Failed, CargoHoldStates.Returned, offer.DestinationStationId, (long?)null),
                    (c.Status, c.HoldState, c.ReturnStationId, c.PaidCredits));
                Assert.Equal(0, Cargo(s).ReservedMassKg);
                Assert.Equal(Math.Max(0, creditsAtDeadline - penalty), s.PlayerCredits);
            }

            // Overdue in open space: the cargo stays sealed in the hold until a human station takes it back.
            var overdue = At(space, deadline + GameCalendar.HourMs);
            Assert.Equal((CargoHoldStates.ReturnPending, offer.MassKg),
                (Assert.Single(Cargo(overdue).Contracts).HoldState, Cargo(overdue).ReservedMassKg));
            Assert.Equal(overdue.PlayerCredits, At(space, deadline + 2 * GameCalendar.HourMs).PlayerCredits);

            // Cancellation docked at a human station: penalty once, cargo returned at once, never paid.
            long credits = At(engine, 0).PlayerCredits;
            var cancelled = engine.ExecuteDemoCampaign(new CancelTransportCommand("cancel", 1, contractId));
            Assert.True(cancelled.Applied, cancelled.Reason);
            var afterCancel = At(engine, 0);
            var returned = Assert.Single(Cargo(afterCancel).Contracts);
            Assert.Equal((StationJobStatuses.Cancelled, CargoHoldStates.Returned, Start, 0L),
                (returned.Status, returned.HoldState, returned.ReturnStationId!, returned.ReturnedAtGameTimeMs!.Value));
            Assert.Equal((0L, credits - penalty), (Cargo(afterCancel).ReservedMassKg, afterCancel.PlayerCredits));
            Assert.Equal(cancelled, engine.ExecuteDemoCampaign(new CancelTransportCommand("cancel", 1, contractId)));
            Assert.Equal(TransportFailureReasons.ContractNotActive,
                engine.ExecuteDemoCampaign(new CancelTransportCommand("cancel-2", cancelled.Revision, contractId)).Reason);
            Assert.Equal(afterCancel.PlayerCredits, At(engine, 0).PlayerCredits);
            Assert.Equal(StationJobReasons.OfferAlreadyAccepted,
                engine.ExecuteDemoCampaign(new AcceptJobOfferCommand("again", cancelled.Revision, offer.Id)).Reason);

            // Debt from another failed contract is repaid from the cargo payout first (shared US-0003 rule).
            var poorStart = At(poor, 0);
            var other = CargoOffers(poorStart)[0];
            var second = poor.ExecuteDemoCampaign(new AcceptJobOfferCommand("second", poorStart.DemoCampaign!.Revision, other.Id));
            Assert.True(second.Applied, second.Reason);
            var secondCancel = poor.ExecuteDemoCampaign(new CancelTransportCommand("second-cancel", second.Revision, "contract:" + other.Id));
            Assert.True(secondCancel.Applied, secondCancel.Reason);
            long debt = Failures(At(poor, 0)).ContractDebt;
            Assert.True(debt > 0);
            using var repaid = Reload(poor, directory, GameCalendar.HourMs / 2, DockedAt(offer.DestinationStationId));
            var paid = At(repaid, GameCalendar.HourMs / 2);
            Assert.Equal(CargoHoldStates.Delivered, CargoContractReasons.HoldState(Cargo(paid), contractId));
            Assert.Equal(CargoHoldStates.Returned, CargoContractReasons.HoldState(Cargo(paid), "contract:" + other.Id));
            long repayment = Math.Min(debt, offer.Payout);
            Assert.Equal(debt - repayment, Failures(paid).ContractDebt);
            Assert.Equal(repayment, Assert.Single(Failures(paid).Failures).RepaidCredits);
            Assert.Equal(offer.Payout - repayment, paid.PlayerCredits - At(poor, GameCalendar.HourMs / 2).PlayerCredits);
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
            var offer = CargoOffer(At(engine, 0));
            string contractId = "contract:" + offer.Id;
            var accepted = engine.ExecuteDemoCampaign(new AcceptJobOfferCommand("accept", 0, offer.Id));
            Assert.True(accepted.Applied, accepted.Reason);
            long t = GameCalendar.HourMs / 4;
            using var space = Reload(engine, directory, 0, InSpace); // branches start from the accepted state at t = 0

            // Sealed cargo, its reservation and the receipt survive Save/Load; the duplicate reserves nothing more.
            var before = At(engine, t);
            using (var loaded = Reload(engine, directory, t))
            {
                var after = At(loaded, t);
                Assert.Equal(JsonSerializer.Serialize(Cargo(before)), JsonSerializer.Serialize(Cargo(after)));
                Assert.Equal(accepted, loaded.ExecuteDemoCampaign(new AcceptJobOfferCommand("accept", 0, offer.Id)));
                Assert.Equal(offer.MassKg, Cargo(At(loaded, t)).ReservedMassKg);
            }

            // Delivered through Load pays once; another Load does not pay again.
            using (var delivered = Reload(engine, directory, t, DockedAt(offer.DestinationStationId)))
            {
                var s = At(delivered, t);
                Assert.Equal(CargoHoldStates.Delivered, Assert.Single(Cargo(s).Contracts).HoldState);
                Assert.Equal(before.PlayerCredits + offer.Payout, s.PlayerCredits);
                Assert.True(s.DemoCampaign!.CommitSequence > before.DemoCampaign!.CommitSequence); // drives the autosave
                using var again = Reload(delivered, directory, t);
                var s2 = At(again, t);
                Assert.Equal(s.PlayerCredits, s2.PlayerCredits);
                Assert.Equal(JsonSerializer.Serialize(Cargo(s)), JsonSerializer.Serialize(Cargo(s2)));
                Assert.Contains("cargo contracts", Assert.ThrowsAny<Exception>(() => Reload(delivered, directory, t, save =>
                    DemoCampaign(save)["cargoContracts"] = new JsonArray())).Message);
                Assert.Contains("cargo contracts", Assert.ThrowsAny<Exception>(() => Reload(delivered, directory, t, save =>
                    DemoCampaign(save)["cargoContracts"]![0]!["paidCredits"] = offer.Payout + 1)).Message);
            }

            // Cancelled in space: waiting for return survives Load; a human docking reached through Load returns it once.
            Assert.True(space.ExecuteDemoCampaign(new CancelTransportCommand("cancel", 1, contractId)).Applied);
            var pending = At(space, t);
            Assert.Equal((CargoHoldStates.ReturnPending, offer.MassKg), (Assert.Single(Cargo(pending).Contracts).HoldState, Cargo(pending).ReservedMassKg));
            using (var loaded = Reload(space, directory, t))
                Assert.Equal(JsonSerializer.Serialize(Cargo(pending)), JsonSerializer.Serialize(Cargo(At(loaded, t))));
            using (var home = Reload(space, directory, t, DockedAt(Start)))
            {
                var s = At(home, t);
                var c = Assert.Single(Cargo(s).Contracts);
                Assert.Equal((CargoHoldStates.Returned, Start, 0L), (c.HoldState, c.ReturnStationId!, Cargo(s).ReservedMassKg));
                Assert.Equal(pending.PlayerCredits, s.PlayerCredits);
                using var again = Reload(home, directory, t);
                Assert.Equal(JsonSerializer.Serialize(Cargo(s)), JsonSerializer.Serialize(Cargo(At(again, t))));
            }

            // A destroyed carrier loses the cargo: no reservation, no penalty, no return.
            using (var lost = Reload(engine, directory, t, save => { InSpace(save); PlayerShip(save)["isDestroyed"] = true; }))
            {
                var wreck = At(lost, t);
                Assert.Equal(CargoHoldStates.Lost, Assert.Single(Cargo(wreck).Contracts).HoldState);
                Assert.Equal(0, Cargo(wreck).ReservedMassKg);
                Assert.Equal(0, Assert.Single(Failures(wreck).Failures).PenaltyCredits);
            }

            // An older save (US-0006) with an active cargo obligation but no cargo section keeps its reservation
            // and still pays exactly once on delivery.
            using (var legacy = Reload(engine, directory, t, save => DemoCampaign(save).AsObject().Remove("cargoContracts")))
            {
                Assert.Equal(offer.MassKg, Cargo(At(legacy, t)).ReservedMassKg);
                using var arrived = Reload(legacy, directory, t, DockedAt(offer.DestinationStationId));
                var s = At(arrived, t);
                Assert.Equal((CargoHoldStates.Delivered, before.PlayerCredits + offer.Payout),
                    (Assert.Single(Cargo(s).Contracts).HoldState, s.PlayerCredits));
            }

            // Corrupted cargo data rejects the whole Load atomically.
            Assert.Contains("cargo contracts", Assert.ThrowsAny<Exception>(() => Reload(engine, directory, t, save =>
                DemoCampaign(save)["cargoContracts"]![0]!["postingId"] = "cargo:other")).Message);
            Assert.Contains("cargo contracts", Assert.ThrowsAny<Exception>(() => Reload(engine, directory, t, save =>
                DemoCampaign(save)["cargoContracts"]![0]!["returnedAtGameTimeMs"] = 0)).Message);
            Assert.Contains("cargo contracts", Assert.ThrowsAny<Exception>(() => Reload(engine, directory, t, save =>
                DemoCampaign(save)["cargoContracts"]!.AsArray().Add(JsonNode.Parse(DemoCampaign(save)["cargoContracts"]![0]!.ToJsonString())))).Message);
            Assert.Contains("cargo contracts", Assert.ThrowsAny<Exception>(() => Reload(engine, directory, t, save =>
                DemoCampaign(save)["stationJobs"]!["obligations"]![0]!["ItemTypeId"] = "item.unknown")).Message);
            Assert.ThrowsAny<Exception>(() => Reload(engine, directory, t, save =>
                DemoCampaign(save)["transport"] = new JsonObject { ["CargoContracts"] = new JsonObject() }));
        }
        finally { Directory.Delete(directory, true); }
    }
}
