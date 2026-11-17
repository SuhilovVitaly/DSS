using System.Runtime.CompilerServices;
using System.Text.Json.Nodes;
using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Engine.Content;
using DeepSpaceSaga.Engine.Scenario;

namespace DeepSpaceSaga.Engine.Tests.Demo;

/// <summary>
/// EP-0034-US-0006-TK-0003: the engine publishes a bounded, deterministic station job board per calendar day,
/// keeps it across screen refreshes and Save/Load, and turns an accepted offer into one journal obligation
/// through the ordinary campaign command path (identity, revision, receipt).
/// </summary>
public sealed class DemoEp0034Us0006Tk0003Tests
{
    private const string Ship = "SPC-0001";
    private const string Start = "SPC-0002";
    private static readonly string[] Cluster = ["SPC-0002", "SPC-0005", "SPC-0006", "SPC-0007", "SPC-0008"];

    private static string Root([CallerFilePath] string source = "") =>
        Path.GetFullPath(Path.Combine(Path.GetDirectoryName(source)!, "..", "..", "..", "src", "DeepSpaceSaga.Client"));

    private static string Settings => Path.Combine(Root(), "Settings.json");
    private static string DemoScenario => Path.Combine(Root(), "Scenarios", "Demo", "scenario.json");

    private static string NewRun()
    {
        string directory = Path.Combine("D:/DeepSpaceSaga/artifacts/validation/EP-0034/us0006-tk0003-fixtures", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        return directory;
    }

    private static SimulationEngine NewDemo(bool docked = true)
    {
        var engine = EngineContentLoader.CreateEngineFromScenarioFile(Settings, DemoScenario);
        engine.SetSpeed(SimulationSpeed.Speed0);
        if (!docked) return engine;
        // The demo starts in flight; dock at the start station through the production Load path.
        var save = engine.CaptureSaveStateForTests(0, SimulationSpeed.Speed0, 0);
        var ship = save.GameState.SpaceObjects.Single(s => s.ObjectId == Ship);
        var dockedShip = ship with
        {
            IsDocked = true,
            DockedStationObjectId = Start,
            FirstPortFeeGameTimeMs = 0,
            NextPortFeeDueGameTimeMs = GameCalendar.DayMs
        };
        engine.LoadScenario(save with
        {
            GameState = save.GameState with
            {
                SpaceObjects = save.GameState.SpaceObjects.Select(s => s.ObjectId == Ship ? dockedShip : s).ToArray()
            }
        }, isSave: true);
        engine.SetSpeed(SimulationSpeed.Speed0);
        return engine;
    }

    private static StationJobsSnapshot Board(SimulationEngine engine, long gameTimeMs = 0) =>
        engine.CaptureSnapshotForTests(gameTimeMs, SimulationSpeed.Speed0, gameTimeMs / 300).DemoCampaign!.Transport!.StationJobs!;

    /// <summary>Write the engine's real save, optionally edit it, and load it through the production save path.</summary>
    private static SimulationEngine Reload(SimulationEngine engine, string directory, long gameTimeMs = 0, Action<JsonNode>? edit = null)
    {
        var save = JsonNode.Parse(ScenarioLoader.Serialize(engine.CaptureSaveStateForTests(gameTimeMs, SimulationSpeed.Speed0, gameTimeMs / 300)))!;
        edit?.Invoke(save);
        string path = Path.Combine(directory, Guid.NewGuid().ToString("N") + ".json");
        File.WriteAllText(path, save.ToJsonString());
        var loaded = EngineContentLoader.CreateEngineFromSaveFile(Settings, path);
        loaded.SetSpeed(SimulationSpeed.Speed0);
        return loaded;
    }

    private static JsonNode PlayerShip(JsonNode save) =>
        save["gameState"]!["spaceObjects"]!.AsArray().Single(o => (string?)o!["objectId"] == Ship)!;

    private static void Undock(JsonNode save)
    {
        PlayerShip(save)["isDocked"] = false;
        PlayerShip(save)["dockedStationObjectId"] = null;
        save["gameState"]!.AsObject().Remove("voyageState");
    }

    [Fact]
    public void HappyPath_MatchesStory()
    {
        using var engine = NewDemo();
        var board = Board(engine);

        // Bounded board on every cluster station, published from the calendar (bucket 0) even while paused.
        Assert.Equal(Start, board.DockedStationId);
        Assert.All(board.Offers.GroupBy(o => o.OriginStationId), g => Assert.InRange(g.Count(), 1, 6));
        Assert.Equal(Cluster.Order(StringComparer.Ordinal), board.Offers.Select(o => o.OriginStationId).Distinct().Order(StringComparer.Ordinal));
        Assert.All(board.Offers, o =>
        {
            Assert.Contains(o.DestinationStationId, Cluster);
            Assert.NotEqual(o.OriginStationId, o.DestinationStationId);
            Assert.Contains(o.Type, new[] { StationJobTypes.Passenger, StationJobTypes.UrgentPassenger, StationJobTypes.Cargo });
            Assert.Equal(0, o.PublishedAtGameTimeMs);
            Assert.Equal(2 * GameCalendar.DayMs, o.ExpiresAtGameTimeMs);
            Assert.True(o.DeadlineDurationMs >= GameCalendar.HourMs);
            // Explainable payout: distance, load, urgency and route risk; whole Tokens rounded up.
            long sum = o.Terms.BaseTokens + o.Terms.LoadTokens + o.Terms.DistanceTokens;
            Assert.Equal((sum * o.Terms.UrgencyPermille * o.Terms.RiskPermille + 999_999) / 1_000_000, o.Payout);
            Assert.Equal(3 * o.RouteDistanceKm, o.Terms.DistanceTokens);
            Assert.Equal(o.Type == StationJobTypes.UrgentPassenger ? 1500 : 1000, o.Terms.UrgencyPermille);
            Assert.Equal(o.RiskBand switch { "safe" => 1000, "normal" => 1150, _ => 1350 }, o.Terms.RiskPermille);
            if (StationJobTypes.CarriesPassengers(o.Type)) Assert.InRange(o.Count, 1, 4);
            else Assert.InRange(o.MassKg, 2000, 12000);
        });

        // The guaranteed first job: ordinary passengers at the start station, acceptable right away.
        var first = board.Offers.Single(o => o.Id == "jobs:SPC-0002:0:0");
        Assert.Equal(StationJobTypes.Passenger, first.Type);
        Assert.Equal(3, first.Count);
        Assert.Equal(StationJobRiskBands.Safe, first.RiskBand); // shortest, safest route for the first job
        Assert.Equal(board.Offers.Where(o => o.OriginStationId == Start).Min(o => o.RouteDistanceKm), first.RouteDistanceKm);
        Assert.Null(StationJobReasons.AcceptBlocker(board, first.Id));
        Assert.All(board.Offers.Where(o => o.OriginStationId != Start),
            o => Assert.Equal(StationJobReasons.NotDockedAtOrigin, StationJobReasons.AcceptBlocker(board, o.Id)));

        // Deterministic by seed and role: a second session publishes exactly the same board.
        using (var twin = NewDemo())
            Assert.Equal(board.Offers.ToArray(), Board(twin).Offers.ToArray());

        // Reopening the screen (new snapshots at the same time) never re-rolls the offers.
        Assert.Equal(board.Offers.ToArray(), Board(engine).Offers.ToArray());

        // Accept through the campaign command: one Active obligation with a deadline from acceptance.
        var response = engine.ExecuteDemoCampaign(new AcceptJobOfferCommand("accept-1", 0, first.Id));
        Assert.True(response.Applied, response.Reason);
        Assert.Equal(1, response.Revision);
        var after = Board(engine);
        Assert.DoesNotContain(after.Offers, o => o.Id == first.Id);
        var obligation = Assert.Single(after.Obligations);
        Assert.Equal(new JobObligation("contract:" + first.Id, first.Id, first.Type, first.OriginStationId, first.DestinationStationId,
            first.Count, 0, first.Payout, 0, first.DeadlineDurationMs, StationJobStatuses.Active, first.RiskBand), obligation);

        // Calendar refresh: day 1 publishes new offers up to the cap, day-0 offers live until day 2.
        var day1 = Board(engine, GameCalendar.DayMs);
        Assert.All(day1.Offers.GroupBy(o => o.OriginStationId), g => Assert.Equal(6, g.Count()));
        Assert.Contains(day1.Offers, o => o.PublishedAtGameTimeMs == 0);
        Assert.Contains(day1.Offers, o => o.PublishedAtGameTimeMs == GameCalendar.DayMs);
        var day2 = Board(engine, 2 * GameCalendar.DayMs);
        Assert.DoesNotContain(day2.Offers, o => o.PublishedAtGameTimeMs == 0);
        Assert.All(day2.Offers.GroupBy(o => o.OriginStationId), g => Assert.Equal(6, g.Count()));
        // The journal survives the refresh; the obligation's deadline is calendar time and does not move.
        Assert.Equal(obligation, Assert.Single(day2.Obligations));
    }

    [Fact]
    public void InvalidOrDuplicate_NoPartialMutation()
    {
        string directory = NewRun();
        try
        {
            using var engine = NewDemo();
            var board = Board(engine);
            var first = board.Offers.Single(o => o.Id == "jobs:SPC-0002:0:0");
            var remote = board.Offers.First(o => o.OriginStationId != Start);
            long credits = engine.PlayerCredits;

            // Wrong station, unknown offer and stale revision are rejected without state change.
            var notDocked = engine.ExecuteDemoCampaign(new AcceptJobOfferCommand("a-remote", 0, remote.Id));
            Assert.Equal((false, StationJobReasons.NotDockedAtOrigin), (notDocked.Applied, notDocked.Reason));
            var unknown = engine.ExecuteDemoCampaign(new AcceptJobOfferCommand("a-unknown", 0, "jobs:SPC-0002:0:99"));
            Assert.Equal(StationJobReasons.OfferNotFound, unknown.Reason);
            var stale = engine.ExecuteDemoCampaign(new AcceptJobOfferCommand("a-stale", 7, first.Id));
            Assert.Equal(DemoCampaignReasons.StaleRevision, stale.Reason);
            var unchanged = Board(engine).Offers;
            Assert.Equal(board.Offers.Select(o => o.Id), unchanged.Select(o => o.Id));
            Assert.Equal(board.Offers.ToArray(), unchanged.ToArray());
            Assert.Empty(Board(engine).Obligations);

            // Accept once; the duplicate returns the same receipt and creates nothing more.
            var applied = engine.ExecuteDemoCampaign(new AcceptJobOfferCommand("a-first", 0, first.Id));
            Assert.True(applied.Applied);
            Assert.Equal(applied, engine.ExecuteDemoCampaign(new AcceptJobOfferCommand("a-first", 0, first.Id)));
            Assert.Equal(DemoCampaignReasons.IdentityConflict,
                engine.ExecuteDemoCampaign(new AcceptJobOfferCommand("a-first", 0, remote.Id)).Reason);
            var again = engine.ExecuteDemoCampaign(new AcceptJobOfferCommand("a-again", applied.Revision, first.Id));
            Assert.Equal(StationJobReasons.OfferAlreadyAccepted, again.Reason);
            Assert.Single(Board(engine).Obligations);
            Assert.Equal(credits, engine.PlayerCredits); // acceptance pays nothing; payout belongs to delivery

            // Capacity: 3 of 4 free cabins are promised, so a larger passenger offer is blocked by the engine.
            var passengers = Board(engine).Offers.Where(o => o.OriginStationId == Start && StationJobTypes.CarriesPassengers(o.Type)).ToArray();
            foreach (var offer in passengers)
                Assert.Equal(offer.Count > 1 ? PassengerCapacityReasons.NoFreeCabins : null,
                    StationJobReasons.AcceptBlocker(Board(engine), offer.Id));

            // In flight the journal stays visible, but nothing can be accepted.
            using var flying = Reload(engine, directory, 0, Undock);
            var inFlight = Board(flying);
            Assert.Null(inFlight.DockedStationId);
            Assert.Single(inFlight.Obligations);
            Assert.All(inFlight.Offers, o => Assert.Equal(StationJobReasons.NotDockedAtOrigin, StationJobReasons.AcceptBlocker(inFlight, o.Id)));
            var rejected = flying.ExecuteDemoCampaign(new AcceptJobOfferCommand("a-fly", flying.CaptureSnapshot().DemoCampaign!.Revision, inFlight.Offers[0].Id));
            Assert.Equal(StationJobReasons.NotDockedAtOrigin, rejected.Reason);
            Assert.Single(Board(flying).Obligations);

            // An expired offer is stale: day 2 no longer shows day-0 offers and acceptance is refused.
            var expiredId = board.Offers.First(o => o.OriginStationId == Start && o.Id != first.Id).Id;
            Board(engine, 2 * GameCalendar.DayMs);
            var expired = engine.ExecuteDemoCampaign(new AcceptJobOfferCommand("a-expired", applied.Revision, expiredId));
            Assert.False(expired.Applied);
            Assert.Contains(expired.Reason, new[] { StationJobReasons.OfferNotFound, StationJobReasons.OfferExpired });
        }
        finally { Directory.Delete(directory, true); }
    }

    [Fact]
    public void SaveLoad_PreservesOutcome()
    {
        string directory = NewRun();
        try
        {
            using var engine = NewDemo();
            var first = Board(engine).Offers.Single(o => o.Id == "jobs:SPC-0002:0:0");
            var applied = engine.ExecuteDemoCampaign(new AcceptJobOfferCommand("save-1", 0, first.Id));
            Assert.True(applied.Applied);
            long noon = 12 * GameCalendar.HourMs; // before the first port fee falls due
            var before = Board(engine, noon);

            using var loaded = Reload(engine, directory, noon);
            var after = Board(loaded, noon);
            Assert.Equal(before.Offers.ToArray(), after.Offers.ToArray());
            Assert.Equal(before.Obligations.ToArray(), after.Obligations.ToArray());
            // The receipt survives: the same command is answered, not applied twice.
            Assert.Equal(applied, loaded.ExecuteDemoCampaign(new AcceptJobOfferCommand("save-1", 0, first.Id)));
            Assert.Single(Board(loaded, noon).Obligations);

            // Corrupted board data rejects the whole Load atomically.
            var tampered = Assert.ThrowsAny<Exception>(() => Reload(engine, directory, noon, save =>
            {
                var offer = save["gameState"]!["demoCampaign"]!["stationJobs"]!["offers"]![0]!;
                Assert.NotNull(offer["Payout"]);
                offer["Payout"] = 1;
            }));
            Assert.Contains("station jobs", tampered.Message);
            var duplicate = Assert.ThrowsAny<Exception>(() => Reload(engine, directory, noon, save =>
            {
                var obligation = save["gameState"]!["demoCampaign"]!["stationJobs"]!["obligations"]![0]!;
                save["gameState"]!["demoCampaign"]!["stationJobs"]!["obligations"]!.AsArray().Add(obligation.DeepClone());
            }));
            Assert.Contains("station jobs", duplicate.Message);
            Assert.ThrowsAny<Exception>(() => Reload(engine, directory, noon, save =>
                save["gameState"]!["demoCampaign"]!["transport"] = new JsonObject { ["StationJobs"] = new JsonObject() }));

            // An older demo save without a board publishes the current day on Load; nothing else is granted.
            // Such a save predates the passenger contracts of EP-0034-US-0002 too, so both sections are absent.
            using var legacy = Reload(engine, directory, noon, save =>
            {
                save["gameState"]!["demoCampaign"]!.AsObject().Remove("stationJobs");
                save["gameState"]!["demoCampaign"]!.AsObject().Remove("passengerContracts");
            });
            var fresh = Board(legacy, noon);
            Assert.Empty(fresh.Obligations);
            Assert.Contains(fresh.Offers, o => o.Id == "jobs:SPC-0002:0:0");

            // Continuing after Load equals continuing without it.
            Assert.Equal(Board(engine, 3 * GameCalendar.DayMs).Offers.ToArray(), Board(loaded, 3 * GameCalendar.DayMs).Offers.ToArray());
        }
        finally { Directory.Delete(directory, true); }
    }
}
