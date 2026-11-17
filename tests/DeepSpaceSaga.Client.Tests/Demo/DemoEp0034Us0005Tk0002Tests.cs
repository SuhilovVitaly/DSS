using System.Text.Json.Nodes;
using DeepSpaceSaga.Client.UI.Controls;
using DeepSpaceSaga.Client.UI.Demo;
using DeepSpaceSaga.Client.UI.Screens;
using DeepSpaceSaga.Client.UI.Screens.Contracts;
using DeepSpaceSaga.Client.UI.Screens.Finance;
using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Engine;
using DeepSpaceSaga.Engine.Content;
using DeepSpaceSaga.Engine.LocalClient;
using DeepSpaceSaga.Engine.Scenario;
using SkiaSharp;

namespace DeepSpaceSaga.Client.Tests.Demo;

/// <summary>
/// EP-0034-US-0005-TK-0002: the transport voyage through the player's screens. Every session is opened from a real save
/// slot file through <see cref="LocalGameSessionConnection.CreateFromSaveFile"/> (the Load/Continue path of
/// SkiaWindow) and saved back through the session; the Contracts and Finance screens are wired as SkiaWindow wires them.
/// The voyage itself (undock, burns, approach, docking dialogue) runs on the authoritative engine between slots. Checks:
/// the journal survives screen changes and Save/Load without resubmission; a confirmed arrival shows exactly one payout
/// in the contract result, the journal and the voyage finance report; a command built on a campaign revision that a
/// tick transition (the payout) already replaced is refused with a readable reason and the retry goes through; a game
/// loaded in flight shows the same deadlines, cancellation terms and, once late, the expiry outcome.
/// </summary>
public sealed class DemoEp0034Us0005Tk0002Tests
{
    private const string Ship = "SPC-0001";
    private const string Start = "SPC-0002";
    private const string Bridge = "MOD-PLAYER-BRIDGE-01";
    private const string Engine = "MOD-PLAYER-ENGINE-01";
    private const string Slot = "us0005-voyage";
    private const long Ratio = 300;
    private static string SettingsPath => Path.Combine(AppContext.BaseDirectory, "Settings.json");
    private static string DemoScenarioPath => Path.Combine(AppContext.BaseDirectory, "Scenarios", "Demo", "scenario.json");

    private static string NewRun()
    {
        string directory = Path.Combine("D:/DeepSpaceSaga/artifacts/validation/EP-0034/us0005-tk0002-fixtures", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        return directory;
    }

    private static string SlotPath(string saves) => Path.Combine(saves, SaveSlotNaming.ToFileName(Slot));

    /// <summary>Write the engine state at <paramref name="gameTimeMs"/> into the voyage slot (paused, as a save is).</summary>
    private static void WriteSlot(SimulationEngine engine, string saves, long gameTimeMs, Action<JsonNode>? edit = null)
    {
        var save = JsonNode.Parse(ScenarioLoader.Serialize(engine.CaptureSaveStateForTests(gameTimeMs, SimulationSpeed.Speed0, gameTimeMs / Ratio)))!;
        edit?.Invoke(save);
        File.WriteAllText(SlotPath(saves), save.ToJsonString());
    }

    /// <summary>New demo game docked at the start station, written to the voyage slot.</summary>
    private static SimulationEngine NewDocked(string saves)
    {
        var engine = EngineContentLoader.CreateEngineFromScenarioFile(SettingsPath, DemoScenarioPath);
        engine.SetSpeed(SimulationSpeed.Speed0);
        var station = engine.CaptureSnapshotForTests(0, SimulationSpeed.Speed0, 0).Objects.Single(o => o.ObjectId == Start);
        WriteSlot(engine, saves, 0, save =>
        {
            var ship = save["gameState"]!["spaceObjects"]!.AsArray().Single(o => (string?)o!["objectId"] == Ship)!;
            ship["positionX"] = station.X;
            ship["positionY"] = station.Y;
            ship["speedMps"] = 0;
            ship["isDocked"] = true;
            ship["dockedStationObjectId"] = Start;
            ship["firstPortFeeGameTimeMs"] = 0;
            ship["nextPortFeeDueGameTimeMs"] = GameCalendar.DayMs;
        });
        engine.Dispose();
        return EngineFromSlot(saves);
    }

    private static SimulationEngine EngineFromSlot(string saves)
    {
        var engine = EngineContentLoader.CreateEngineFromSaveFile(SettingsPath, SlotPath(saves));
        engine.SetSpeed(SimulationSpeed.Speed0);
        return engine;
    }

    /// <summary>A player session opened from the voyage slot exactly like Load/Continue does.</summary>
    private sealed class Session : IAsyncDisposable
    {
        public required LocalGameSessionConnection Connection { get; init; }
        public required GameSessionHandle Handle { get; init; }
        public int Commands;

        public static async Task<Session> Open(string saves)
        {
            var connection = LocalGameSessionConnection.CreateFromSaveFile(SettingsPath, SlotPath(saves), saves);
            var session = new Session { Connection = connection, Handle = new GameSessionHandle(connection) };
            await WaitFor(() => session.Handle.DemoCampaign?.Transport?.StationJobs is not null && session.Handle.Buffer.Latest is not null);
            return session;
        }

        /// <summary>Contracts screen as SkiaWindow.OpenContractsAsync builds it (optionally with a delayed campaign view).</summary>
        public ContractsScreen Contracts(Func<DemoCampaignSnapshot?>? campaign = null)
        {
            var screen = new ContractsScreen(Handle.Buffer, campaign ?? (() => Handle.DemoCampaign), command =>
            {
                Interlocked.Increment(ref Commands);
                return Handle.ExecuteDemoCampaignAsync(command);
            });
            Render(screen);
            return screen;
        }

        public FinanceScreen Finance()
        {
            var screen = new FinanceScreen(Handle.Buffer);
            Render(screen);
            return screen;
        }

        public AuthoritativeSnapshot Latest => Handle.Buffer.Latest!.Snapshot;

        public async ValueTask DisposeAsync()
        {
            await Handle.DisposeAsync();
            await Connection.DisposeAsync();
        }
    }

    /// <summary>Command-driven voyage on the authoritative engine (physical = calendar / 300).</summary>
    private sealed class Flight(SimulationEngine engine)
    {
        private long _motion;
        private long _next;
        public AuthoritativeSnapshot Snapshot { get; private set; } = engine.CaptureSnapshotForTests(0, SimulationSpeed.Speed0, 0);
        public long GameTime => _motion * Ratio;

        public AuthoritativeSnapshot Capture() => Snapshot = engine.CaptureSnapshotForTests(_motion * Ratio, SimulationSpeed.Speed0, _motion);

        public void Advance(long gameMs)
        {
            _motion += (gameMs + Ratio - 1) / Ratio;
            Capture();
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

        public void Accept(string offerId) =>
            Assert.True(engine.ExecuteDemoCampaign(new AcceptJobOfferCommand($"accept-{offerId}", Capture().DemoCampaign!.Revision, offerId)).Applied);

        public void Depart(string destination)
        {
            Assert.Equal(CommandResultStatus.Executed, Send(Bridge, NavigationComputerCommandTypes.Undock, destination)?.Status);
            for (int burn = 0; burn < 3; burn++)
            {
                double before = Me.SpeedKmS;
                Assert.NotEqual(CommandResultStatus.Rejected, Send(Engine, ShipEngineCommandTypes.Accelerate)?.Status);
                WaitUntil(() => Me.SpeedKmS > before, "acceleration");
            }
        }

        /// <summary>Approach and dock; the docking dialogue stays open (the arrival is not confirmed yet).</summary>
        public void Dock(string destination)
        {
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
            Assert.NotNull(Snapshot.ActiveDialogue);
        }
    }

    private static void Render(IScreen screen)
    {
        using var bitmap = new SKBitmap(1920, 1080);
        using var canvas = new SKCanvas(bitmap);
        screen.OnActivated();
        screen.Render(canvas, 1920, 1080);
    }

    private static async Task WaitFor(Func<bool> condition)
    {
        var deadline = DateTime.UtcNow.AddSeconds(15);
        while (!condition())
        {
            Assert.True(DateTime.UtcNow < deadline, "timed out waiting for the session");
            await Task.Delay(50);
        }
    }

    /// <summary>Everything the Contracts screen shows about obligations (board offers excluded).</summary>
    private static string Shown(ContractsScreen screen)
    {
        var jobs = screen.JobsView;
        var passengers = screen.PassengersView;
        var cargo = screen.CargoView;
        var failures = screen.FailuresView;
        return string.Join("\n", new[] { jobs.Notice, jobs.JournalEmpty, passengers.Summary, passengers.Empty, cargo.Hold, cargo.Empty, failures.Debt, failures.Empty }
            .Concat(jobs.Journal).Concat(passengers.Lines).Concat(cargo.Lines)
            .Concat(failures.Active.SelectMany(r => new[] { r.Headline, r.Terms, r.Blocker }))
            .Concat(failures.Lines).Select(l => l ?? "-"));
    }

    private static (JobOffer Urgent, JobOffer Ordinary) ChoosePassengers(DemoCampaignSnapshot campaign)
    {
        var board = campaign.Transport!.StationJobs!;
        var offers = board.Offers.Where(o => o.OriginStationId == Start && StationJobReasons.AcceptBlocker(board, o.Id) is null)
            .OrderBy(o => o.DeadlineDurationMs).ThenBy(o => o.Id, StringComparer.Ordinal).ToArray();
        int cabins = campaign.Transport.PassengerCapacity!.FreeCabins;
        var urgent = offers.First(o => o.Type == StationJobTypes.UrgentPassenger && o.Count < cabins);
        var ordinary = offers.First(o => o.Type == StationJobTypes.Passenger && o.Count <= cabins - urgent.Count &&
            o.DestinationStationId != urgent.DestinationStationId);
        return (urgent, ordinary);
    }

    private static string Name(Session session, string id) => session.Latest.Objects.Single(o => o.ObjectId == id).DisplayName!;

    [Theory]
    [InlineData("English")]
    [InlineData("Russian")]
    public async Task ContractJournal_SurvivesScreenChanges(string language)
    {
        using var locale = Localization.UseLocaleForTests(language);
        string saves = NewRun();
        try
        {
            NewDocked(saves).Dispose();
            string shown;
            JobOffer urgent, ordinary;
            await using (var session = await Session.Open(saves))
            {
                var screen = session.Contracts();
                (urgent, ordinary) = ChoosePassengers(session.Handle.DemoCampaign!);

                // Both orders are accepted through the drawn buttons; each result is confirmed by the snapshot.
                foreach (var offer in new[] { urgent, ordinary })
                {
                    var button = screen.AcceptButtons.Single(b => b.OfferId == offer.Id).Rect;
                    Assert.Equal(ScreenEvent.None, screen.OnMouseDown(button.MidX, button.MidY));
                    await WaitFor(() => !screen.IsAcceptPending &&
                        session.Handle.DemoCampaign!.Transport!.PassengerContracts!.Contracts.Any(c => c.OfferId == offer.Id));
                    Render(screen);
                    Assert.Equal(string.Format(Localization.Get("Demo.PassengerContracts.Boarded"), offer.Count,
                        Name(session, offer.DestinationStationId), StationJobsPresentation.Time(offer.DeadlineDurationMs)), screen.PassengerFeedback);
                }
                Assert.Equal(2, session.Commands);
                shown = Shown(screen);
                Assert.Equal(2, screen.JobsView.Journal.Length);
                Assert.Contains(string.Format(Localization.Get("Demo.PassengerContracts.Aboard"), urgent.Count + ordinary.Count), shown);

                // Leaving for other screens and reopening Contracts shows the same journal and sends nothing again.
                screen.OnDeactivated();
                session.Finance();
                var reopened = session.Contracts();
                Assert.Equal(shown, Shown(reopened));
                Assert.False(reopened.IsAcceptPending);
                Assert.Null(reopened.JobsFeedback);
                Assert.Null(reopened.PassengerFeedback);
                Assert.Equal(2, session.Commands);

                // Saved through the session into the slot file.
                await session.Handle.SaveAsync(Slot);
            }

            // Loaded from the slot: the same journal, cabins and terms, still without resubmission.
            await using (var session = await Session.Open(saves))
            {
                var loaded = session.Contracts();
                Assert.Equal(shown, Shown(loaded));
                Assert.Null(loaded.JobsFeedback);
                Assert.Equal(0, session.Commands);
                Assert.All(new[] { urgent, ordinary }, offer =>
                    Assert.DoesNotContain(loaded.JobsView.Offers, row => row.OfferId == offer.Id));
            }
        }
        finally { Directory.Delete(saves, true); }
    }

    [Theory]
    [InlineData("English")]
    [InlineData("Russian")]
    public async Task Settlement_ShowsOnePayout(string language)
    {
        using var locale = Localization.UseLocaleForTests(language);
        string saves = NewRun();
        try
        {
            string destination;
            using (var engine = NewDocked(saves))
            {
                var flight = new Flight(engine);
                var (urgent, ordinary) = ChoosePassengers(flight.Snapshot.DemoCampaign!);
                flight.Accept(urgent.Id);
                flight.Accept(ordinary.Id);
                destination = urgent.DestinationStationId;
                flight.Depart(destination);
                flight.Dock(urgent.DestinationStationId);
                WriteSlot(engine, saves, flight.GameTime); // saved with the docking dialogue still open
            }

            long credits, payout;
            string result, journal;
            await using (var session = await Session.Open(saves))
            {
                var before = session.Handle.DemoCampaign!;
                var contract = before.Transport!.PassengerContracts!.Contracts.Single(c => c.Type == StationJobTypes.UrgentPassenger);
                payout = contract.Payout;
                Assert.Equal(PassengerDeliveryStates.Aboard, contract.DeliveryState);

                // The screen still holds the campaign view from before the payout (the 1 Hz snapshot has not arrived).
                DemoCampaignSnapshot? shownCampaign = before;
                var screen = session.Contracts(() => shownCampaign);

                // The player finishes the docking dialogue through the session: the payout is a tick transition.
                for (int step = 0; step < 10 && session.Latest.ActiveDialogue is { } dialogue; step++)
                {
                    string[] preferred = ["truthful_id", "accept_fee", "continue"];
                    var choice = dialogue.Choices.Where(c => c.Enabled)
                        .OrderBy(c => Array.IndexOf(preferred, c.ChoiceId) is var i && i >= 0 ? i : int.MaxValue).First();
                    await session.Handle.SendDialogueCommandAsync(new($"dialogue-{step}", DialogueAction.Choose, dialogue.InstanceId, dialogue.Revision, choice.ChoiceId));
                    await WaitFor(() => session.Latest.ActiveDialogue?.Revision != dialogue.Revision);
                }
                await WaitFor(() => session.Handle.DemoCampaign!.Transport!.PassengerContracts!.Contracts
                    .Single(c => c.ContractId == contract.ContractId).DeliveryState == PassengerDeliveryStates.Delivered);
                var settled = session.Handle.DemoCampaign!;
                Assert.True(settled.Revision > before.Revision);

                // A cancellation confirmed on the view from before the payout carries the replaced revision: the engine
                // refuses it with a readable reason, charges nothing, cancels nothing, and the screen is free again.
                string ordinaryId = before.Transport.PassengerContracts.Contracts.Single(c => c.Type == StationJobTypes.Passenger).ContractId;
                long creditsBefore = session.Latest.PlayerCredits;
                Assert.Null(screen.TryCancelContract(ordinaryId)); // first click shows the published penalty
                await screen.TryCancelContract(ordinaryId)!;
                Assert.Equal(1, session.Commands);
                Assert.Equal(TransportFailurePresentation.Reason(DemoCampaignReasons.StaleRevision), screen.JobsFeedback);
                Assert.DoesNotContain("stale_revision", screen.JobsFeedback);
                Assert.False(screen.IsAcceptPending);
                Assert.Empty(session.Handle.DemoCampaign!.Transport!.TransportFailures!.Failures);
                Assert.Equal(creditsBefore, session.Latest.PlayerCredits);

                // With the current snapshot the player carries on: a board offer of this station is accepted at once.
                shownCampaign = session.Handle.DemoCampaign;
                var offer = settled.Transport!.StationJobs!.Offers.First(o => o.OriginStationId == destination &&
                    StationJobReasons.AcceptBlocker(settled.Transport.StationJobs, o.Id) is null);
                await screen.TryAcceptOffer(offer.Id)!;
                Assert.Equal(2, session.Commands);
                Assert.Equal(StationJobsPresentation.Accepted, screen.JobsFeedback);
                await WaitFor(() => session.Handle.DemoCampaign!.Transport!.StationJobs!.Obligations.Any(o => o.OfferId == offer.Id));
                shownCampaign = session.Handle.DemoCampaign;

                // One payout: the contract result, the journal and the finance report of the arrival voyage show it once.
                var delivered = session.Handle.DemoCampaign!.Transport!.PassengerContracts!.Contracts.Single(c => c.ContractId == contract.ContractId);
                result = PassengerContractsPresentation.Delivered(delivered)!;
                Assert.Equal(string.Format(Localization.Get("Demo.PassengerContracts.Delivered"),
                    StationJobsPresentation.Time(delivered.DeliveredAtGameTimeMs!.Value), payout), result);
                Assert.Single(screen.PassengersView.Lines, l => l == result);
                journal = string.Join("\n", screen.JobsView.Journal);
                Assert.Contains(StationJobsPresentation.Obligation(session.Handle.DemoCampaign!.Transport!.StationJobs!.Obligations
                    .Single(o => o.ContractId == contract.ContractId), id => Name(session, id)), screen.JobsView.Journal);

                var finance = session.Finance();
                Assert.Equal(destination, finance.SelectedReport!.DestinationStationObjectId);
                var payoutRow = Assert.Single(finance.VoyageRows, r => r.Label == Localization.Get("Finance.PassengerPayout"));
                Assert.Equal(FinanceScreen.MoneyText(payout), payoutRow.Value);
                // The ledger column holds every contract payout (passengers and cargo, US-0004 A-04): the label says so.
                Assert.Equal(language == "Russian" ? "Выплаты по контрактам" : "Contract payouts", payoutRow.Label);

                // Reopening the screens and further snapshots never pay again.
                credits = session.Latest.PlayerCredits;
                ulong sequence = session.Latest.SnapshotSequence;
                for (int i = 0; i < 3; i++)
                {
                    screen.OnDeactivated();
                    screen = session.Contracts();
                    session.Finance();
                }
                await WaitFor(() => session.Latest.SnapshotSequence > sequence + 1);
                Assert.Equal(credits, session.Latest.PlayerCredits);
                Assert.Single(screen.PassengersView.Lines, l => l == result);
                Assert.Equal(StationToolbar.ResolveCreditsCount(session.Latest), credits);

                await session.Handle.SaveAsync(Slot);
            }

            // After Load: the same balance, result, journal and finance row; still one payout.
            await using (var session = await Session.Open(saves))
            {
                var screen = session.Contracts();
                Assert.Equal(credits, session.Latest.PlayerCredits);
                Assert.Single(screen.PassengersView.Lines, l => l == result);
                Assert.Equal(journal, string.Join("\n", screen.JobsView.Journal));
                var finance = session.Finance();
                Assert.Equal(FinanceScreen.MoneyText(payout),
                    Assert.Single(finance.VoyageRows, r => r.Label == Localization.Get("Finance.PassengerPayout")).Value);
                Assert.Equal(0, session.Commands);
            }
        }
        finally { Directory.Delete(saves, true); }
    }

    [Theory]
    [InlineData("English")]
    [InlineData("Russian")]
    public async Task LoadedVoyage_RestoresVisibleDeadline(string language)
    {
        using var locale = Localization.UseLocaleForTests(language);
        string saves = NewRun();
        try
        {
            using var engine = NewDocked(saves);
            var flight = new Flight(engine);
            var (urgent, ordinary) = ChoosePassengers(flight.Snapshot.DemoCampaign!);
            flight.Accept(urgent.Id);
            flight.Accept(ordinary.Id);
            flight.Depart(urgent.DestinationStationId);
            var inFlight = flight.Capture();
            WriteSlot(engine, saves, flight.GameTime);

            // Loaded in flight: no board, the journal and passenger lines show the engine's deadlines and states,
            // both orders keep their cancellation terms, and the balance is the engine's.
            await using (var session = await Session.Open(saves))
            {
                var screen = session.Contracts();
                Assert.Equal(Localization.Get("Demo.Jobs.InFlight"), screen.JobsView.Notice);
                Assert.Empty(screen.JobsView.Offers);
                string StationName(string id) => Name(session, id);
                Assert.Equal(inFlight.DemoCampaign!.Transport!.StationJobs!.Obligations.Select(o => StationJobsPresentation.Obligation(o, StationName)),
                    screen.JobsView.Journal);
                foreach (var contract in inFlight.DemoCampaign.Transport.PassengerContracts!.Contracts)
                {
                    Assert.Contains(PassengerContractsPresentation.Line(contract, StationName), screen.PassengersView.Lines);
                    Assert.Contains(StationJobsPresentation.Time(contract.DeadlineGameTimeMs), PassengerContractsPresentation.Line(contract, StationName));
                    Assert.Equal(PassengerDeliveryStates.Aboard, contract.DeliveryState);
                }
                Assert.Equal(2, screen.FailuresView.Active.Length);
                Assert.All(screen.FailuresView.Active, row => Assert.True(row.CanCancel));
                Assert.Equal(inFlight.PlayerCredits, session.Latest.PlayerCredits);
                Assert.Equal(0, session.Commands);
            }

            // Still in flight after the urgent deadline: the loaded game names the expiry, the penalty and that the
            // passengers stay aboard until the next docking.
            var deadline = inFlight.DemoCampaign.Transport.PassengerContracts.Contracts.Single(c => c.Type == StationJobTypes.UrgentPassenger).DeadlineGameTimeMs;
            flight.Advance(deadline + 60_000 - flight.GameTime);
            var late = flight.Capture();
            var failure = Assert.Single(late.DemoCampaign!.Transport!.TransportFailures!.Failures);
            Assert.Equal(TransportFailureReasons.Expired, failure.Reason);
            WriteSlot(engine, saves, flight.GameTime);
            await using (var session = await Session.Open(saves))
            {
                var screen = session.Contracts();
                string StationName(string id) => Name(session, id);
                var expected = TransportFailurePresentation.Build(late.DemoCampaign.Transport.TransportFailures, late.DemoCampaign.Transport.StationJobs, StationName);
                Assert.Equal(string.Join("\n", expected.Lines), string.Join("\n", screen.FailuresView.Lines));
                Assert.Contains(screen.FailuresView.Lines, l => l.Contains(TransportFailurePresentation.Outcome(TransportFailureReasons.Expired), StringComparison.Ordinal));
                Assert.Contains(screen.PassengersView.Lines, l => l.EndsWith(PassengerContractsPresentation.State(PassengerDeliveryStates.Failed), StringComparison.Ordinal));
                Assert.Single(screen.FailuresView.Active); // only the ordinary order can still be cancelled
                Assert.Equal(late.PlayerCredits, session.Latest.PlayerCredits);
            }
        }
        finally { Directory.Delete(saves, true); }
    }
}
