using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text;
using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Engine.Content;
using Xunit.Abstractions;

namespace DeepSpaceSaga.Engine.Tests.Demo;

/// <summary>
/// EP-0034-US-0006-TK-0006: every deadline the station board can publish is achievable by a real command-driven flight
/// of the starter carrier. For each ordered pair of directly linked demo cluster stations the carrier undocks, makes the
/// three-burn cruise, approaches, synchronizes speed and heading, docks and confirms the docking dialogue; the calendar
/// time of that flight is compared with the deadline of an ordinary, urgent and cargo order on the same route.
/// Ordinary and cargo orders keep at least a 25% margin, urgent orders stay feasible but tighter than ordinary ones.
/// </summary>
public sealed class DemoEp0034Us0006Tk0006Tests(ITestOutputHelper output)
{
    private const string Ship = "SPC-0001";
    private const string Bridge = "MOD-PLAYER-BRIDGE-01";
    private const string Engine = "MOD-PLAYER-ENGINE-01";
    private const long Ratio = 300;

    private static string Root([CallerFilePath] string source = "") =>
        Path.GetFullPath(Path.Combine(Path.GetDirectoryName(source)!, "..", "..", "..", "src", "DeepSpaceSaga.Client"));

    private static string Settings => Path.Combine(Root(), "Settings.json");
    private static string DemoScenario => Path.Combine(Root(), "Scenarios", "Demo", "scenario.json");

    /// <summary>New demo game docked at <paramref name="station"/> (heading 0°, as after a real docking) through the Load path.</summary>
    private static SimulationEngine NewDocked(string station)
    {
        var engine = EngineContentLoader.CreateEngineFromScenarioFile(Settings, DemoScenario);
        engine.SetSpeed(SimulationSpeed.Speed0);
        var save = engine.CaptureSaveStateForTests(0, SimulationSpeed.Speed0, 0);
        var position = engine.CaptureSnapshotForTests(0, SimulationSpeed.Speed0, 0).Objects.Single(o => o.ObjectId == station);
        var ship = save.GameState.SpaceObjects.Single(s => s.ObjectId == Ship) with
        {
            PositionX = position.X,
            PositionY = position.Y,
            SpeedMps = 0,
            DirectionDegrees = 0,
            IsDocked = true,
            DockedStationObjectId = station,
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

    /// <summary>Command-driven flight in calendar time (physical = calendar / 300), the same pilot as US-0004/US-0005.</summary>
    private sealed class Flight(SimulationEngine engine)
    {
        private long _motion;
        private long _next;
        public AuthoritativeSnapshot Snapshot { get; private set; } = engine.CaptureSnapshotForTests(0, SimulationSpeed.Speed0, 0);
        public long GameTime => _motion * Ratio;

        private AuthoritativeSnapshot Capture() => Snapshot = engine.CaptureSnapshotForTests(_motion * Ratio, SimulationSpeed.Speed0, _motion);

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

        public void Fly(string destination)
        {
            Assert.Equal(CommandResultStatus.Executed, Send(Bridge, NavigationComputerCommandTypes.Undock, destination)?.Status);
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
            var dock = Send(Bridge, NavigationComputerCommandTypes.Dock, destination);
            Assert.True(dock?.Status == CommandResultStatus.Executed, $"dock: {dock?.Status} {dock?.ReasonCode}");
            string[] preferred = ["truthful_id", "accept_fee", "continue"];
            for (int step = 0; step < 10 && Snapshot.ActiveDialogue is { } dialogue; step++)
            {
                var choice = dialogue.Choices.Where(c => c.Enabled)
                    .OrderBy(c => Array.IndexOf(preferred, c.ChoiceId) is var i && i >= 0 ? i : int.MaxValue).First();
                engine.ReceiveDialogueCommand(new($"dialogue-{++_next}", DialogueAction.Choose, dialogue.InstanceId, dialogue.Revision, choice.ChoiceId));
                Capture();
            }
            Assert.Null(Snapshot.ActiveDialogue);
            Assert.True(Me.IsDocked);
            Assert.Equal(destination, Me.DockedStationObjectId);
        }
    }

    /// <summary>Ordered (origin, destination) pairs the board can publish: direct cluster links with a known risk, both ways.</summary>
    private static (string Origin, string Destination)[] PublishablePairs(SimulationEngine engine)
    {
        var map = engine.CaptureClusterVoyageMapForTools()!;
        string[] cluster = ["SPC-0002", "SPC-0005", "SPC-0006", "SPC-0007", "SPC-0008"];
        return map.Edges.Where(e => cluster.Contains(e.FromStationObjectId) && cluster.Contains(e.ToStationObjectId))
            .SelectMany(e => new[] { (e.FromStationObjectId, e.ToStationObjectId), (e.ToStationObjectId, e.FromStationObjectId) })
            .Distinct().OrderBy(p => p.Item1, StringComparer.Ordinal).ThenBy(p => p.Item2, StringComparer.Ordinal).ToArray();
    }

    [Fact]
    public void PublishedDeadlines_AreFeasibleForRealFlights()
    {
        using var probe = NewDocked("SPC-0002");
        var pairs = PublishablePairs(probe);
        Assert.NotEmpty(pairs);

        // The probe helper is the formula the board publishes: every live offer agrees with it.
        var board = probe.CaptureSnapshotForTests(0, SimulationSpeed.Speed0, 0).DemoCampaign!.Transport!.StationJobs!;
        Assert.NotEmpty(board.Offers);
        foreach (var offer in board.Offers)
            Assert.Equal(offer.DeadlineDurationMs, probe.StationJobDeadlineForTools(offer.Type, offer.OriginStationId, offer.DestinationStationId));

        var report = new StringBuilder();
        var failures = new List<string>();
        foreach (var (origin, destination) in pairs)
        {
            using var engine = NewDocked(origin);
            long ordinary = engine.StationJobDeadlineForTools(StationJobTypes.Passenger, origin, destination)!.Value;
            long urgent = engine.StationJobDeadlineForTools(StationJobTypes.UrgentPassenger, origin, destination)!.Value;
            long cargo = engine.StationJobDeadlineForTools(StationJobTypes.Cargo, origin, destination)!.Value;
            var flight = new Flight(engine);
            flight.Fly(destination);
            long actual = flight.GameTime;
            report.AppendLine(string.Create(CultureInfo.InvariantCulture,
                $"{origin}->{destination}: flight {actual / 60000.0:F1} min; ordinary {ordinary / 60000.0:F1}, urgent {urgent / 60000.0:F1}, cargo {cargo / 60000.0:F1} min"));
            // Ordinary and cargo: at least 25% above the real flight; urgent: feasible, tighter than ordinary.
            if (ordinary * 4 < actual * 5) failures.Add($"{origin}->{destination} ordinary");
            if (cargo * 4 < actual * 5) failures.Add($"{origin}->{destination} cargo");
            if (urgent <= actual) failures.Add($"{origin}->{destination} urgent");
            if (urgent >= ordinary) failures.Add($"{origin}->{destination} urgent not tighter");
        }
        output.WriteLine(report.ToString());
        Assert.True(failures.Count == 0, "infeasible deadlines: " + string.Join(", ", failures) + Environment.NewLine + report);
    }

    [Fact]
    public void DeadlineFormula_AddsManeuverAllowanceAndCruiseScale()
    {
        // 15 km at 1 km/s: ETA 15 s physical = 4 500 000 ms calendar.
        var plain = new DemoJobDeadlines(2000, 1500, 3000, 1);
        Assert.Equal(9_000_000, SimulationEngine.StationJobDeadlineMs(plain, StationJobTypes.Passenger, 15, 1.0));
        Assert.Equal(6_750_000, SimulationEngine.StationJobDeadlineMs(plain, StationJobTypes.UrgentPassenger, 15, 1.0));
        Assert.Equal(13_500_000, SimulationEngine.StationJobDeadlineMs(plain, StationJobTypes.Cargo, 15, 1.0));
        // (1 000 000 + 4 500 000 x 2) x 1.2 = 12 000 000; the minimum still wins when larger.
        var flight = new DemoJobDeadlines(1200, 1100, 1300, 1, ManeuverGameTimeMs: 1_000_000, CruisePermille: 2000);
        Assert.Equal(12_000_000, SimulationEngine.StationJobDeadlineMs(flight, StationJobTypes.Passenger, 15, 1.0));
        Assert.Equal(11_000_000, SimulationEngine.StationJobDeadlineMs(flight, StationJobTypes.UrgentPassenger, 15, 1.0));
        Assert.Equal(13_000_000, SimulationEngine.StationJobDeadlineMs(flight, StationJobTypes.Cargo, 15, 1.0));
        Assert.Equal(20_000_000, SimulationEngine.StationJobDeadlineMs(flight with { MinimumGameTimeMs = 20_000_000 }, StationJobTypes.Passenger, 15, 1.0));
    }

    [Theory]
    [InlineData("maneuverGameTimeMs", -1)]
    [InlineData("cruisePermille", 999)]
    public void InvalidFlightEstimate_RejectsContent(string field, long value)
    {
        string directory = Path.Combine("D:/DeepSpaceSaga/artifacts/validation/EP-0034/us0006-tk0006-fixtures", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Path.Combine(directory, "Data", "Demo"));
        try
        {
            File.Copy(Path.Combine(Root(), "Data", "Demo", "cluster.json"), Path.Combine(directory, "Data", "Demo", "cluster.json"));
            var jobs = System.Text.Json.Nodes.JsonNode.Parse(File.ReadAllText(Path.Combine(Root(), "Data", "Demo", "jobs.json")))!;
            File.WriteAllText(Path.Combine(directory, "Data", "Demo", "jobs.json"), jobs.ToJsonString());
            Assert.NotNull(DemoCampaignDefinitions.Load(directory).Jobs); // the shipped values are valid
            jobs["deadlines"]![field] = value;
            File.WriteAllText(Path.Combine(directory, "Data", "Demo", "jobs.json"), jobs.ToJsonString());
            Assert.Throws<DeepSpaceSaga.Engine.Scenario.ScenarioException>(() => DemoCampaignDefinitions.Load(directory));
        }
        finally { Directory.Delete(directory, true); }
    }

    [Fact]
    public void SavedOffersAndObligations_KeepFrozenDeadlinesOnLoad()
    {
        using var engine = NewDocked("SPC-0002");
        var board = engine.CaptureSnapshotForTests(0, SimulationSpeed.Speed0, 0).DemoCampaign!.Transport!.StationJobs!;
        var accepted = board.Offers.First(o => o.OriginStationId == "SPC-0002" && StationJobReasons.AcceptBlocker(board, o.Id) is null);
        Assert.True(engine.ExecuteDemoCampaign(new AcceptJobOfferCommand("accept", engine.CaptureSnapshotForTests(0, SimulationSpeed.Speed0, 0)
            .DemoCampaign!.Revision, accepted.Id)).Applied);

        // A save written under the previous formula (12 x the max-speed ETA): its frozen terms survive Load untouched.
        var save = engine.CaptureSaveStateForTests(0, SimulationSpeed.Speed0, 0);
        var jobs = save.GameState.DemoCampaign!.StationJobs!;
        var legacyOffer = jobs.Offers[0] with { DeadlineDurationMs = 9_000_000 };
        var legacyObligation = jobs.Obligations[0] with { DeadlineGameTimeMs = jobs.Obligations[0].AcceptedAtGameTimeMs + 9_000_000 };
        using var loaded = EngineContentLoader.CreateEngineFromScenarioFile(Settings, DemoScenario);
        loaded.LoadScenario(save with
        {
            GameState = save.GameState with
            {
                DemoCampaign = save.GameState.DemoCampaign with
                {
                    StationJobs = jobs with { Offers = jobs.Offers.SetItem(0, legacyOffer), Obligations = jobs.Obligations.SetItem(0, legacyObligation) }
                }
            }
        }, isSave: true);
        loaded.SetSpeed(SimulationSpeed.Speed0);
        foreach (long time in new[] { 0L, 3_000_000L })
        {
            var after = loaded.CaptureSnapshotForTests(time, SimulationSpeed.Speed0, time / Ratio).DemoCampaign!.Transport!.StationJobs!;
            Assert.Equal(9_000_000, after.Offers.Single(o => o.Id == legacyOffer.Id).DeadlineDurationMs);
            Assert.Equal(legacyObligation.DeadlineGameTimeMs, after.Obligations.Single(o => o.ContractId == legacyObligation.ContractId).DeadlineGameTimeMs);
        }
        Assert.NotEqual(9_000_000, loaded.StationJobDeadlineForTools(legacyOffer.Type, legacyOffer.OriginStationId, legacyOffer.DestinationStationId));
    }
}
