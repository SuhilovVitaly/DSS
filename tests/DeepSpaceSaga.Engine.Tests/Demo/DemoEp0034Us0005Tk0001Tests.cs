using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Nodes;
using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Engine.Content;
using DeepSpaceSaga.Engine.Scenario;

namespace DeepSpaceSaga.Engine.Tests.Demo;

/// <summary>
/// EP-0034-US-0005-TK-0001: a mixed peaceful voyage — ordinary and urgent passengers, sealed contract cargo, a cancelled
/// cargo order with contract debt and the player's own trade goods — is saved and loaded through real save files at
/// every boundary (before acceptance, docked after acceptance, in flight, during the docking dialogue, after payout,
/// after cancellation, finished) and continues with the same people, cargo, deadlines, money and ledger. A loaded
/// continuation ends exactly where the uninterrupted voyage ends; repeated arrival, reading the board, duplicate
/// commands and repeated Loads never pay twice; sealed cargo is never tradable; a corrupted save is rejected without
/// touching the running world.
/// </summary>
public sealed class DemoEp0034Us0005Tk0001Tests
{
    private const string Ship = "SPC-0001";
    /// <summary>Start station: ordinary and urgent passengers in the first publication, shortest calibrated routes.</summary>
    private const string Start = "SPC-0002";
    /// <summary>Station whose first publication lists cargo orders.</summary>
    private const string CargoPort = "SPC-0005";
    private const string Bridge = "MOD-PLAYER-BRIDGE-01";
    private const string Engine = "MOD-PLAYER-ENGINE-01";
    private const string Hold = "MOD-PLAYER-CARGO-01";
    private const string OwnGoods = "item.iron-ore";
    private const string DrainGoods = "item.magnesium-ore";
    private const long Ratio = 300;

    private static string Root([CallerFilePath] string source = "") =>
        Path.GetFullPath(Path.Combine(Path.GetDirectoryName(source)!, "..", "..", "..", "src", "DeepSpaceSaga.Client"));

    private static string Settings => Path.Combine(Root(), "Settings.json");
    private static string DemoScenario => Path.Combine(Root(), "Scenarios", "Demo", "scenario.json");

    private static string NewRun()
    {
        string directory = Path.Combine("D:/DeepSpaceSaga/artifacts/validation/EP-0034/us0005-tk0001-fixtures", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        return directory;
    }

    /// <summary>New demo game docked at <paramref name="station"/>, through a real save file.</summary>
    private static SimulationEngine NewDocked(string directory, string station)
    {
        using var fresh = EngineContentLoader.CreateEngineFromScenarioFile(Settings, DemoScenario);
        fresh.SetSpeed(SimulationSpeed.Speed0);
        var position = fresh.CaptureSnapshotForTests(0, SimulationSpeed.Speed0, 0).Objects.Single(o => o.ObjectId == station);
        return Reload(fresh, directory, 0, save =>
        {
            var ship = PlayerShip(save);
            ship["positionX"] = position.X;
            ship["positionY"] = position.Y;
            ship["speedMps"] = 0;
            ship["isDocked"] = true;
            ship["dockedStationObjectId"] = station;
            ship["firstPortFeeGameTimeMs"] = 0;
            ship["nextPortFeeDueGameTimeMs"] = GameCalendar.DayMs;
        });
    }

    /// <summary>Command-driven voyage in calendar time (physical = calendar / 300); can continue in a loaded engine.</summary>
    private sealed class Flight
    {
        private readonly SimulationEngine _engine;
        private long _motion;
        private long _next;

        public Flight(SimulationEngine engine, long motion = 0, long next = 0)
        {
            _engine = engine;
            _motion = motion;
            _next = next;
            Capture();
        }

        public AuthoritativeSnapshot Snapshot { get; private set; } = null!;
        public long GameTime => _motion * Ratio;

        /// <summary>The same voyage continued by another engine (for example one loaded from a save of this one).</summary>
        public Flight Continue(SimulationEngine loaded) => new(loaded, _motion, _next);

        public AuthoritativeSnapshot Capture() => Snapshot = _engine.CaptureSnapshotForTests(_motion * Ratio, SimulationSpeed.Speed0, _motion);

        private CommandResult? Send(PlayerCommand command)
        {
            _engine.ReceiveCommand(command);
            return Capture().CommandResults.SingleOrDefault(r => r.CommandId == command.CommandId);
        }

        private CommandResult? Command(string module, string type, string? target = null) =>
            Send(new PlayerCommand($"flight-{++_next}", (ulong)_next, Ship, module, type, TargetObjectId: target));

        public DemoCampaignResponse Campaign(Func<string, long, DemoCampaignCommand> create)
        {
            var response = _engine.ExecuteDemoCampaign(create($"campaign-{++_next}", Capture().DemoCampaign!.Revision));
            Capture();
            return response;
        }

        /// <summary>A quoted trade of the player's own goods through the ordinary trade path.</summary>
        public void Trade(string type, string itemTypeId, long quantity)
        {
            var quote = _engine.GetTradeQuote(new TradeQuoteRequest($"quote-{_next + 1}", Ship, Hold, type, itemTypeId, quantity));
            Assert.Null(quote.DisabledReason);
            Assert.Equal(quantity, quote.ExecutableQuantity);
            var result = Send(new PlayerCommand($"flight-{++_next}", (ulong)_next, Ship, Hold, type,
                ItemTypeId: itemTypeId, Quantity: quantity, QuoteId: quote.QuoteId, MarketRevision: quote.MarketRevision));
            Assert.Equal(CommandResultStatus.Executed, result?.Status);
        }

        /// <summary>Spend Tokens on own goods until fewer than <paramref name="below"/> are left.</summary>
        public void SpendBelow(long below)
        {
            for (int i = 0; i < 20 && Snapshot.PlayerCredits >= below; i++)
            {
                var one = _engine.GetTradeQuote(new TradeQuoteRequest($"probe-{_next}-{i}", Ship, Hold, TradeCommandTypes.Buy, DrainGoods, 1));
                long price = one.Curve[0].UnitPriceCredits;
                Trade(TradeCommandTypes.Buy, DrainGoods, Math.Max(1, Math.Min(one.MaximumQuantity, (Snapshot.PlayerCredits - below / 2) / price)));
            }
            Assert.True(Snapshot.PlayerCredits < below, $"{Snapshot.PlayerCredits} Tokens left, expected fewer than {below}");
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
            Assert.Equal(CommandResultStatus.Executed, Command(Bridge, NavigationComputerCommandTypes.Undock, destination)?.Status);
            Assert.False(Me.IsDocked);
        }

        public void Cruise()
        {
            for (int burn = 0; burn < 3; burn++)
            {
                double before = Me.SpeedKmS;
                Assert.NotEqual(CommandResultStatus.Rejected, Command(Engine, ShipEngineCommandTypes.Accelerate)?.Status);
                WaitUntil(() => Me.SpeedKmS > before, "acceleration");
            }
        }

        /// <summary>Approach and dock; the docking dialogue is left open (the docking is not confirmed yet).</summary>
        public void Dock(string destination)
        {
            Command(Engine, NavigationComputerCommandTypes.Approach, destination);
            WaitUntil(() => Me.ApproachRoute is not null, "approach plan");
            _motion += (long)Math.Ceiling(Me.ApproachRoute!.DurationMs);
            Capture();
            Command(Engine, ShipEngineCommandTypes.SpeedSynchronization, destination);
            WaitUntil(() => Me.SpeedKmS == 0, "speed synchronization");
            if (Math.Abs(Me.Direction) > 1e-6)
            {
                Command(Engine, ShipEngineCommandTypes.DirectionSynchronization, destination);
                WaitUntil(() => Math.Abs(Me.Direction) < 1e-6, "direction synchronization");
            }
            var dock = Command(Bridge, NavigationComputerCommandTypes.Dock, destination);
            Assert.True(dock?.Status == CommandResultStatus.Executed, $"dock: {dock?.Status} {dock?.ReasonCode}");
            Assert.NotNull(Snapshot.ActiveDialogue);
        }

        /// <summary>Finish the docking dialogue (identify, pay the fee, continue): the docking becomes confirmed.</summary>
        public void Confirm()
        {
            string[] preferred = ["truthful_id", "accept_fee", "continue"];
            for (int step = 0; step < 10 && Snapshot.ActiveDialogue is { } dialogue; step++)
            {
                var choice = dialogue.Choices.Where(c => c.Enabled)
                    .OrderBy(c => Array.IndexOf(preferred, c.ChoiceId) is var i && i >= 0 ? i : int.MaxValue).First();
                _engine.ReceiveDialogueCommand(new($"dialogue-{++_next}", DialogueAction.Choose, dialogue.InstanceId, dialogue.Revision, choice.ChoiceId));
                Capture();
            }
            Assert.Null(Snapshot.ActiveDialogue);
            Assert.True(Me.IsDocked);
        }

        public void Arrive(string destination)
        {
            Cruise();
            Dock(destination);
            Confirm();
        }
    }

    private static DemoTransportSnapshot Transport(AuthoritativeSnapshot s) => s.DemoCampaign!.Transport!;
    private static StationJobsSnapshot Board(AuthoritativeSnapshot s) => Transport(s).StationJobs!;
    private static PassengerContractsSnapshot Passengers(AuthoritativeSnapshot s) => Transport(s).PassengerContracts!;
    private static CargoContractsSnapshot Cargo(AuthoritativeSnapshot s) => Transport(s).CargoContracts!;
    private static TransportFailuresSnapshot Failures(AuthoritativeSnapshot s) => Transport(s).TransportFailures!;

    private static PassengerContract PassengerContract(AuthoritativeSnapshot s, string contractId) =>
        Passengers(s).Contracts.Single(c => c.ContractId == contractId);

    private static CargoContract CargoContract(AuthoritativeSnapshot s, string contractId) =>
        Cargo(s).Contracts.Single(c => c.ContractId == contractId);

    private static AuthoritativeSnapshot At(SimulationEngine engine, long gameTimeMs) =>
        engine.CaptureSnapshotForTests(gameTimeMs, SimulationSpeed.Speed0, gameTimeMs / Ratio);

    private static long Penalty(long payout) => (payout * 250 + 999) / 1000;

    /// <summary>
    /// Everything the player continues with: money, campaign revision, the whole transport projection (board, journal,
    /// cabins, passenger and cargo contracts, failures and debt), the people aboard, the hold stacks and the voyage ledger.
    /// </summary>
    private static string Continuation(SimulationEngine engine, long gameTimeMs)
    {
        var s = At(engine, gameTimeMs);
        var ship = engine.CaptureSaveStateForTests(gameTimeMs, SimulationSpeed.Speed0, gameTimeMs / Ratio).GameState.SpaceObjects
            .Single(o => o.ObjectId == Ship);
        return JsonSerializer.Serialize(new
        {
            s.PlayerCredits,
            s.DemoCampaign!.Revision,
            s.DemoCampaign.CommitSequence,
            Transport = Transport(s),
            ship.IsDocked,
            ship.DockedStationObjectId,
            ship.Passengers,
            Cargo = ship.Modules!.Single(m => m.ModuleId == Hold).Cargo,
            Ledger = engine.VoyageFinancesForTests
        });
    }

    private static string SaveFile(SimulationEngine engine, string directory, long gameTimeMs, Action<JsonNode>? edit = null)
    {
        var save = JsonNode.Parse(ScenarioLoader.Serialize(engine.CaptureSaveStateForTests(gameTimeMs, SimulationSpeed.Speed0, gameTimeMs / Ratio)))!;
        edit?.Invoke(save);
        string path = Path.Combine(directory, Guid.NewGuid().ToString("N") + ".json");
        File.WriteAllText(path, save.ToJsonString());
        return path;
    }

    private static SimulationEngine Reload(SimulationEngine engine, string directory, long gameTimeMs, Action<JsonNode>? edit = null)
    {
        var loaded = EngineContentLoader.CreateEngineFromSaveFile(Settings, SaveFile(engine, directory, gameTimeMs, edit));
        loaded.SetSpeed(SimulationSpeed.Speed0);
        return loaded;
    }

    /// <summary>Save, load from the file and require the loaded game to continue with exactly the same state.</summary>
    private static SimulationEngine RoundTrip(SimulationEngine engine, string directory, long gameTimeMs, string checkpoint)
    {
        string before = Continuation(engine, gameTimeMs);
        var loaded = Reload(engine, directory, gameTimeMs);
        Assert.True(before == Continuation(loaded, gameTimeMs), $"checkpoint '{checkpoint}' changed across Save/Load");
        return loaded;
    }

    private static JsonNode PlayerShip(JsonNode save) =>
        save["gameState"]!["spaceObjects"]!.AsArray().Single(o => (string?)o!["objectId"] == Ship)!;

    private static JsonNode DemoCampaign(JsonNode save) => save["gameState"]!["demoCampaign"]!;

    private static Action<JsonNode> DockedAt(string station) => save =>
    {
        PlayerShip(save)["isDocked"] = true;
        PlayerShip(save)["dockedStationObjectId"] = station;
    };

    /// <summary>Acceptable offers of a station, shortest deadline first (deterministic from the scenario seed).</summary>
    private static JobOffer[] Offers(AuthoritativeSnapshot snapshot, string station)
    {
        var board = Board(snapshot);
        return board.Offers.Where(o => o.OriginStationId == station && StationJobReasons.AcceptBlocker(board, o.Id) is null)
            .OrderBy(o => o.DeadlineDurationMs).ThenBy(o => o.Id, StringComparer.Ordinal).ToArray();
    }

    /// <summary>Urgent passengers and, in the remaining cabins, ordinary passengers to another station.</summary>
    private static (JobOffer Urgent, JobOffer Ordinary) ChoosePassengers(AuthoritativeSnapshot snapshot)
    {
        var offers = Offers(snapshot, Start);
        int cabins = Transport(snapshot).PassengerCapacity!.FreeCabins;
        var urgent = offers.First(o => o.Type == StationJobTypes.UrgentPassenger && o.Count < cabins);
        var ordinary = offers.First(o => o.Type == StationJobTypes.Passenger && o.Count <= cabins - urgent.Count &&
            o.DestinationStationId != urgent.DestinationStationId);
        return (urgent, ordinary);
    }

    /// <summary>Two cargo orders of <see cref="CargoPort"/> to <paramref name="destination"/>, lighter first.</summary>
    private static (JobOffer Sealed, JobOffer Cancelled) ChooseCargo(AuthoritativeSnapshot snapshot, string? destination = null)
    {
        var cargo = Offers(snapshot, CargoPort).Where(o => o.Type == StationJobTypes.Cargo &&
                (destination is null || o.DestinationStationId == destination))
            .OrderBy(o => o.MassKg).ThenBy(o => o.Id, StringComparer.Ordinal).ToArray();
        return (cargo[0], cargo[1]);
    }

    [Fact]
    public void MixedVoyage_RoundTripsEveryBoundary()
    {
        string directory = NewRun();
        try
        {
            using var start = NewDocked(directory, Start);
            var flight = new Flight(start);
            var (urgentOffer, ordinaryOffer) = ChoosePassengers(flight.Snapshot);
            string urgentId = "contract:" + urgentOffer.Id, ordinaryId = "contract:" + ordinaryOffer.Id;

            // Checkpoint: before acceptance.
            using (RoundTrip(start, directory, 0, "before acceptance")) { }

            // Ordinary and urgent passengers board; the player buys own trade goods.
            foreach (var offer in new[] { urgentOffer, ordinaryOffer })
            {
                var accepted = flight.Campaign((id, revision) => new AcceptJobOfferCommand(id, revision, offer.Id));
                Assert.True(accepted.Applied, accepted.Reason);
            }
            flight.Trade(TradeCommandTypes.Buy, OwnGoods, 5);
            Assert.Equal(urgentOffer.Count + ordinaryOffer.Count, Passengers(flight.Snapshot).PassengersAboard);

            // Checkpoint: docked after acceptance.
            using (RoundTrip(start, directory, 0, "accepted at origin")) { }

            // First leg, saved and loaded in flight; the loaded continuation and the uninterrupted voyage end identically.
            flight.Undock(urgentOffer.DestinationStationId);
            flight.Cruise();
            using var loaded = RoundTrip(start, directory, flight.GameTime, "in flight with passengers");
            var voyage = flight.Continue(loaded);
            flight.Dock(urgentOffer.DestinationStationId);
            flight.Confirm();
            voyage.Dock(urgentOffer.DestinationStationId);
            long sequenceBeforeConfirm = voyage.Snapshot.DemoCampaign!.CommitSequence;
            voyage.Confirm();
            Assert.Equal(flight.GameTime, voyage.GameTime);
            Assert.Equal(Continuation(start, flight.GameTime), Continuation(loaded, voyage.GameTime));

            // Urgent passengers delivered once by their deadline; ordinary passengers stay aboard.
            var first = voyage.Snapshot;
            var urgent = PassengerContract(first, urgentId);
            Assert.Equal((PassengerDeliveryStates.Delivered, urgentOffer.Payout), (urgent.DeliveryState, urgent.PaidCredits!.Value));
            Assert.True(urgent.DeliveredAtGameTimeMs <= urgent.DeadlineGameTimeMs);
            Assert.Equal((PassengerDeliveryStates.Aboard, ordinaryOffer.Count),
                (PassengerContract(first, ordinaryId).DeliveryState, Passengers(first).PassengersAboard));
            // A passenger payout is a campaign commit like every other transport transition: it drives the autosave.
            Assert.True(first.DemoCampaign!.CommitSequence > sequenceBeforeConfirm, "passenger payout must be a campaign commit");
            var firstLedger = loaded.VoyageFinancesForTests[^1];
            Assert.Equal((urgentOffer.DestinationStationId, urgentOffer.Payout),
                (firstLedger.DestinationStationObjectId!, firstLedger.PassengerPayoutCredits));

            // Own goods are sold at the destination (gross sales of the arrival voyage).
            voyage.Trade(TradeCommandTypes.Sell, OwnGoods, 5);
            Assert.True(loaded.VoyageFinancesForTests[^1].GrossSalesCredits > 0);

            // Cargo orders to the ordinary destination: one sealed, one to be cancelled; own goods of the sealed item type.
            Assert.Equal(CargoPort, urgentOffer.DestinationStationId);
            var (sealedOffer, cancelledOffer) = ChooseCargo(voyage.Snapshot, ordinaryOffer.DestinationStationId);
            string sealedId = "contract:" + sealedOffer.Id, cancelledId = "contract:" + cancelledOffer.Id;
            foreach (var offer in new[] { sealedOffer, cancelledOffer })
                Assert.True(voyage.Campaign((id, revision) => new AcceptJobOfferCommand(id, revision, offer.Id)).Applied);
            voyage.Trade(TradeCommandTypes.Buy, sealedOffer.ItemTypeId!, 3);
            long penalty = Penalty(cancelledOffer.Payout);
            voyage.SpendBelow(penalty); // the cancellation below creates contract debt
            Assert.Equal(sealedOffer.MassKg + cancelledOffer.MassKg, Cargo(voyage.Snapshot).ReservedMassKg);

            // Checkpoint: after the first payout, docked with new cargo obligations.
            using var docked = RoundTrip(loaded, directory, voyage.GameTime, "after payout");
            var secondLeg = voyage.Continue(docked);

            // Second leg: the second cargo order is cancelled in flight without enough Tokens.
            secondLeg.Undock(ordinaryOffer.DestinationStationId);
            long credits = secondLeg.Snapshot.PlayerCredits;
            var cancel = secondLeg.Campaign((id, revision) => new CancelTransportCommand(id, revision, cancelledId));
            Assert.True(cancel.Applied, cancel.Reason);
            long debt = penalty - credits;
            Assert.Equal((debt, CargoHoldStates.ReturnPending),
                (Failures(secondLeg.Snapshot).ContractDebt, CargoContract(secondLeg.Snapshot, cancelledId).HoldState));

            // Checkpoint: after cancellation, in flight with every kind of obligation at once.
            secondLeg.Cruise();
            using var inFlight = RoundTrip(docked, directory, secondLeg.GameTime, "in flight after cancellation");
            var resumed = secondLeg.Continue(inFlight);
            secondLeg.Dock(ordinaryOffer.DestinationStationId);
            secondLeg.Confirm();
            resumed.Dock(ordinaryOffer.DestinationStationId);
            resumed.Confirm();
            Assert.Equal(Continuation(docked, secondLeg.GameTime), Continuation(inFlight, resumed.GameTime));

            // Settled once at the destination: passengers and sealed cargo delivered by their deadlines, payouts repay the
            // contract debt first, the cancelled cargo is handed back without payment.
            var last = resumed.Snapshot;
            var ordinary = PassengerContract(last, ordinaryId);
            Assert.Equal((PassengerDeliveryStates.Delivered, ordinaryOffer.Payout), (ordinary.DeliveryState, ordinary.PaidCredits!.Value));
            Assert.True(ordinary.DeliveredAtGameTimeMs <= ordinary.DeadlineGameTimeMs);
            var sealedCargo = CargoContract(last, sealedId);
            Assert.Equal((CargoHoldStates.Delivered, sealedOffer.Payout), (sealedCargo.HoldState, sealedCargo.PaidCredits!.Value));
            Assert.True(sealedCargo.DeliveredAtGameTimeMs <= sealedCargo.DeadlineGameTimeMs);
            Assert.Equal(CargoHoldStates.Returned, CargoContract(last, cancelledId).HoldState);
            var failure = Assert.Single(Failures(last).Failures);
            Assert.Equal((debt, 0L), (failure.RepaidCredits, failure.Debt));
            Assert.Equal((0L, 0, 0L), (Failures(last).ContractDebt, Passengers(last).PassengersAboard, Cargo(last).ReservedMassKg));
            var lastLedger = inFlight.VoyageFinancesForTests[^1];
            Assert.Equal((ordinaryOffer.DestinationStationId, ordinaryOffer.Payout + sealedOffer.Payout, penalty),
                (lastLedger.DestinationStationObjectId!, lastLedger.PassengerPayoutCredits, lastLedger.PassengerPenaltyCredits));

            // The journal holds every outcome once; the ledger holds each contract posting once.
            Assert.Equal(new[] { (ordinaryId, StationJobStatuses.Completed), (urgentId, StationJobStatuses.Completed),
                    (sealedId, StationJobStatuses.Completed), (cancelledId, StationJobStatuses.Cancelled) }.OrderBy(x => x.Item1, StringComparer.Ordinal),
                Board(last).Obligations.Select(o => (o.ContractId, o.Status)).OrderBy(x => x.Item1, StringComparer.Ordinal));
            Assert.Equal(urgentOffer.Payout + ordinaryOffer.Payout + sealedOffer.Payout,
                inFlight.VoyageFinancesForTests.Sum(v => v.PassengerPayoutCredits));

            // Own goods of the sealed item type stayed the player's: exactly those 3 units can be sold.
            Assert.Equal(3, inFlight.GetTradeQuote(new("sell-own", Ship, Hold, TradeCommandTypes.Sell, sealedOffer.ItemTypeId!, 3)).ExecutableQuantity);
            Assert.Equal(0, inFlight.GetTradeQuote(new("sell-more", Ship, Hold, TradeCommandTypes.Sell, sealedOffer.ItemTypeId!, 4)).ExecutableQuantity);

            // Checkpoint: the finished voyage.
            using (RoundTrip(inFlight, directory, resumed.GameTime, "voyage finished")) { }
        }
        finally { Directory.Delete(directory, true); }
    }

    [Fact]
    public void ArrivalReplay_DoesNotPayAgain()
    {
        string directory = NewRun();
        try
        {
            using var engine = NewDocked(directory, Start);
            var flight = new Flight(engine);
            var (offer, _) = ChoosePassengers(flight.Snapshot);
            string contractId = "contract:" + offer.Id;
            var accepted = engine.ExecuteDemoCampaign(new AcceptJobOfferCommand("accept", flight.Snapshot.DemoCampaign!.Revision, offer.Id));
            Assert.True(accepted.Applied, accepted.Reason);
            flight.Undock(offer.DestinationStationId);
            flight.Cruise();
            flight.Dock(offer.DestinationStationId);

            // Saved while the docking dialogue is still open: nothing is paid before the docking is confirmed.
            Assert.Equal(PassengerDeliveryStates.Aboard, PassengerContract(flight.Snapshot, contractId).DeliveryState);
            using var loaded = RoundTrip(engine, directory, flight.GameTime, "docking dialogue");
            var resumed = flight.Continue(loaded);
            flight.Confirm();
            resumed.Confirm();
            Assert.Equal(Continuation(engine, flight.GameTime), Continuation(loaded, resumed.GameTime));
            var settled = resumed.Snapshot;
            Assert.Equal(offer.Payout, PassengerContract(settled, contractId).PaidCredits);
            int ledgers = loaded.VoyageFinancesForTests.Length;
            long credits = settled.PlayerCredits;

            // Repeated snapshots and reading the board/journal never pay again.
            for (int i = 0; i < 5; i++)
            {
                var again = resumed.Capture();
                Assert.Equal(credits, again.PlayerCredits);
                Assert.Equal(StationJobStatuses.Completed, Board(again).Obligations.Single(o => o.ContractId == contractId).Status);
            }

            // A duplicate acceptance returns the stored receipt; a new identity is refused; neither pays.
            Assert.Equal(accepted, loaded.ExecuteDemoCampaign(new AcceptJobOfferCommand("accept", 0, offer.Id)));
            Assert.Equal(StationJobReasons.OfferAlreadyAccepted, resumed.Campaign((id, revision) => new AcceptJobOfferCommand(id, revision, offer.Id)).Reason);
            Assert.Equal(credits, resumed.Capture().PlayerCredits);

            // Loading the docked game again replays the arrival tick: no second payout, no second ledger posting.
            using (var replay = Reload(loaded, directory, resumed.GameTime))
            using (var replayAgain = Reload(replay, directory, resumed.GameTime, DockedAt(offer.DestinationStationId)))
            {
                Assert.Equal(credits, At(replayAgain, resumed.GameTime).PlayerCredits);
                Assert.Equal(offer.Payout, replayAgain.VoyageFinancesForTests.Sum(v => v.PassengerPayoutCredits));
            }

            // A real second arrival at the same station (a round trip; a voyage always targets another station):
            // new voyages, port fees only, no contract income.
            resumed.Undock(Start);
            resumed.Arrive(Start);
            resumed.Undock(offer.DestinationStationId);
            resumed.Arrive(offer.DestinationStationId);
            var back = resumed.Snapshot;
            Assert.Equal(offer.Payout, PassengerContract(back, contractId).PaidCredits);
            Assert.True(loaded.VoyageFinancesForTests.Length > ledgers);
            Assert.Equal(offer.Payout, loaded.VoyageFinancesForTests.Sum(v => v.PassengerPayoutCredits));
            Assert.True(back.PlayerCredits <= credits);
        }
        finally { Directory.Delete(directory, true); }
    }

    [Fact]
    public void ReservedCargo_IsNotTradable()
    {
        string directory = NewRun();
        try
        {
            using var engine = NewDocked(directory, CargoPort);
            var flight = new Flight(engine);
            var (sealedOffer, _) = ChooseCargo(flight.Snapshot);
            string sealedId = "contract:" + sealedOffer.Id;
            string item = sealedOffer.ItemTypeId!;
            Assert.True(flight.Campaign((id, revision) => new AcceptJobOfferCommand(id, revision, sealedOffer.Id)).Applied);
            flight.Trade(TradeCommandTypes.Buy, item, 3);

            void AssertOnlyOwnGoodsTradable(SimulationEngine e)
            {
                var hold = Cargo(At(e, 0));
                Assert.Equal(sealedOffer.MassKg, hold.ReservedMassKg);
                Assert.Equal(hold.HoldCapacityKg - hold.ReservedMassKg - hold.PlayerCargoMassKg, hold.FreeHoldKg);
                var four = e.GetTradeQuote(new("sell-4", Ship, Hold, TradeCommandTypes.Sell, item, 4));
                Assert.Equal((3L, 0L), (four.MaximumQuantity, four.ExecutableQuantity));
                Assert.Equal(3, e.GetTradeQuote(new("sell-3", Ship, Hold, TradeCommandTypes.Sell, item, 3)).ExecutableQuantity);
            }

            AssertOnlyOwnGoodsTradable(engine);
            using var loaded = RoundTrip(engine, directory, 0, "sealed cargo and own goods");
            AssertOnlyOwnGoodsTradable(loaded);

            // A damaged hold below the reservation still loads: the sealed cargo stays sealed, there is no trade space,
            // and the cargo is still delivered and paid exactly once.
            using (var damaged = Reload(engine, directory, 0, save =>
                   {
                       var module = PlayerShip(save)["modules"]!.AsArray().Single(m => (string?)m!["moduleId"] == Hold)!;
                       module["structurePoints"] = 0;
                       module["destroyed"] = true;
                   }))
            {
                var hold = Cargo(At(damaged, 0));
                Assert.Equal((sealedOffer.MassKg, 0L, 0L), (hold.ReservedMassKg, hold.HoldCapacityKg, hold.FreeHoldKg));
                Assert.NotNull(damaged.GetTradeQuote(new("buy-1", Ship, Hold, TradeCommandTypes.Buy, "item.iron-ore", 1)).DisabledReason);
                long before = At(damaged, 0).PlayerCredits;
                using var arrived = Reload(damaged, directory, 0, DockedAt(sealedOffer.DestinationStationId));
                var s = At(arrived, 0);
                Assert.Equal((CargoHoldStates.Delivered, before + sealedOffer.Payout), (CargoContract(s, sealedId).HoldState, s.PlayerCredits));
                using var again = Reload(arrived, directory, 0);
                Assert.Equal(s.PlayerCredits, At(again, 0).PlayerCredits);
            }

            // A corrupted save is rejected before the running world is replaced: the live game continues unchanged.
            string live = Continuation(loaded, 0);
            string corrupt = SaveFile(loaded, directory, 0, save => DemoCampaign(save)["cargoContracts"]![0]!["postingId"] = "cargo:other");
            Assert.Contains("cargo contracts", Assert.ThrowsAny<Exception>(() => EngineContentLoader.CreateEngineFromSaveFile(Settings, corrupt)).Message);
            Assert.ThrowsAny<Exception>(() => loaded.LoadScenario(ScenarioLoader.LoadFromJson(File.ReadAllText(corrupt), true, isSave: true), isSave: true));
            string orphan = SaveFile(loaded, directory, 0, save => DemoCampaign(save)["stationJobs"]!["obligations"]!.AsArray().Clear());
            Assert.ThrowsAny<Exception>(() => loaded.LoadScenario(ScenarioLoader.LoadFromJson(File.ReadAllText(orphan), true, isSave: true), isSave: true));
            Assert.Equal(live, Continuation(loaded, 0));
            AssertOnlyOwnGoodsTradable(loaded);
        }
        finally { Directory.Delete(directory, true); }
    }
}
