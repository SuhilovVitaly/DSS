using System.Runtime.CompilerServices;
using System.Text.Json.Nodes;
using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Engine.Content;
using DeepSpaceSaga.Engine.Scenario;

namespace DeepSpaceSaga.Engine.Tests.Demo;

/// <summary>
/// EP-0034-US-0001-TK-0003: the engine projects passenger capacity of the demo carrier from real modules,
/// crew and people aboard; it is never persisted, never granted to other scenarios or on Load, and
/// passengers really consume rations during a physical flight.
/// </summary>
public sealed class DemoEp0034Us0001Tk0003Tests
{
    private const string Ship = "SPC-0001";
    private const string Rations = "item.food-rations";

    private static string Root([CallerFilePath] string source = "") =>
        Path.GetFullPath(Path.Combine(Path.GetDirectoryName(source)!, "..", "..", "..", "src", "DeepSpaceSaga.Client"));

    private static string Settings => Path.Combine(Root(), "Settings.json");
    private static string DemoScenario => Path.Combine(Root(), "Scenarios", "Demo", "scenario.json");

    private static string NewRun()
    {
        string directory = Path.Combine("D:/DeepSpaceSaga/artifacts/validation/ep-0036/20261117-execution/capacity-fixtures", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        return directory;
    }

    private static PassengerCapacitySnapshot? Capacity(SimulationEngine engine) =>
        engine.CaptureSnapshot().DemoCampaign?.Transport?.PassengerCapacity;

    private static long RationsAboard(AuthoritativeSnapshot snapshot) =>
        snapshot.InstalledModules.SelectMany(m => m.Cargo).Where(c => c.ItemTypeId == Rations).Sum(c => c.Quantity);

    /// <summary>New demo game with people already aboard (the demo scenario file plus an inline manifest).</summary>
    private static SimulationEngine DemoWithPassengers(string directory, int passengers)
    {
        var scenario = JsonNode.Parse(File.ReadAllText(DemoScenario))!;
        var ship = scenario["gameState"]!["spaceObjects"]!.AsArray().Single(o => (string?)o!["objectId"] == Ship)!;
        var list = new JsonArray();
        for (int i = 1; i <= passengers; i++)
            list.Add(new JsonObject { ["passengerId"] = $"PAX-{i:00}", ["displayName"] = $"Passenger {i}" });
        ship["passengers"] = list;
        // Folder name stays "Demo" so the production generation rules match the shipped scenario.
        string folder = Path.Combine(directory, "Demo");
        Directory.CreateDirectory(folder);
        string path = Path.Combine(folder, "scenario.json");
        File.WriteAllText(path, scenario.ToJsonString());
        return EngineContentLoader.CreateEngineFromScenarioFile(Settings, path);
    }

    /// <summary>Write the engine's real save, optionally edit it, and load it through the production save path.</summary>
    private static SimulationEngine Reload(SimulationEngine engine, string directory, Action<JsonNode>? edit = null)
    {
        var save = JsonNode.Parse(ScenarioLoader.Serialize(engine.CaptureSaveState()))!;
        edit?.Invoke(save);
        string path = Path.Combine(directory, Guid.NewGuid().ToString("N") + ".json");
        File.WriteAllText(path, save.ToJsonString());
        return EngineContentLoader.CreateEngineFromSaveFile(Settings, path);
    }

    private static JsonNode PlayerShip(JsonNode save) =>
        save["gameState"]!["spaceObjects"]!.AsArray().Single(o => (string?)o!["objectId"] == Ship)!;

    [Fact]
    public void HappyPath_MatchesStory()
    {
        using var engine = EngineContentLoader.CreateEngineFromScenarioFile(Settings, DemoScenario);
        engine.SetSpeed(SimulationSpeed.Speed0);
        var capacity = Capacity(engine);
        Assert.Equal(new PassengerCapacitySnapshot(12, 7, 0, 0, 5, null), capacity);
        Assert.Null(PassengerCapacityReasons.BoardingBlocker(capacity));
        // The ordinary carrier surface agrees: the free cabins are real, not a UI scale.
        Assert.Equal(5, engine.CaptureSnapshot().CarrierOperations!.CabinReservation!.FreeCabins);

        string directory = NewRun();
        try
        {
            // Physical first voyage with five passengers: the ship moves and the hold feeds twelve people.
            using var voyage = DemoWithPassengers(directory, 5);
            voyage.SetSpeed(SimulationSpeed.Speed0);
            Assert.Equal(new PassengerCapacitySnapshot(12, 7, 5, 0, 0, null), Capacity(voyage));
            Assert.Equal(PassengerCapacityReasons.NoFreeCabins, PassengerCapacityReasons.BoardingBlocker(Capacity(voyage)));
            var before = voyage.CaptureSnapshotForTests(0, SimulationSpeed.Speed0, 0);
            var start = before.Objects.Single(o => o.ObjectId == Ship);
            long day = GameCalendar.DayMs;
            var after = voyage.CaptureSnapshotForTests(day, SimulationSpeed.Speed0, day / 300);
            var end = after.Objects.Single(o => o.ObjectId == Ship);
            Assert.NotEqual((start.X, start.Y), (end.X, end.Y));
            // Two meals per calendar day for 7 crew + 5 passengers.
            Assert.Equal(RationsAboard(before) - 2 * 12, RationsAboard(after));
            Assert.Equal(new PassengerCapacitySnapshot(12, 7, 5, 0, 0, null), after.DemoCampaign!.Transport!.PassengerCapacity);
        }
        finally { Directory.Delete(directory, recursive: true); }
    }

    [Fact]
    public void InvalidOrDuplicate_NoPartialMutation()
    {
        string directory = NewRun();
        try
        {
            using var engine = EngineContentLoader.CreateEngineFromScenarioFile(Settings, DemoScenario);
            engine.SetSpeed(SimulationSpeed.Speed0);
            string saved = ScenarioLoader.Serialize(engine.CaptureSaveState());
            // Repeated projection is pure: no revision, receipt or world change.
            var first = engine.CaptureSnapshot().DemoCampaign!;
            var second = engine.CaptureSnapshot().DemoCampaign!;
            // Compared by content: the transport now also carries the job board (EP-0034-US-0006), whose
            // immutable arrays have reference equality inside records.
            Assert.Equal(System.Text.Json.JsonSerializer.Serialize(first.Transport), System.Text.Json.JsonSerializer.Serialize(second.Transport));
            Assert.Equal(first.Transport!.PassengerCapacity, second.Transport!.PassengerCapacity);
            Assert.Equal((first.Revision, first.CommitSequence), (second.Revision, second.CommitSequence));
            Assert.Equal(saved, ScenarioLoader.Serialize(engine.CaptureSaveState()));

            // Destroyed quarters: free never goes negative; the deficit is an explicit reason.
            using var damaged = Reload(engine, directory, save =>
            {
                foreach (var module in PlayerShip(save)["modules"]!.AsArray())
                    if (((string?)module!["moduleId"])!.StartsWith("MOD-PLAYER-LIVING-QUARTERS-0", StringComparison.Ordinal) &&
                        (string?)module["moduleId"] != "MOD-PLAYER-LIVING-QUARTERS-01")
                        module["structurePoints"] = 0;
            });
            var deficit = Capacity(damaged)!;
            Assert.Equal(new PassengerCapacitySnapshot(4, 7, 0, 0, 0, PassengerCapacityReasons.CabinsOvercommitted), deficit);

            // Empty hold: cabins exist but life support cannot feed a full complement.
            using var hungry = Reload(engine, directory, save =>
            {
                foreach (var module in PlayerShip(save)["modules"]!.AsArray())
                {
                    var cargo = module!["cargo"]?.AsArray();
                    var stack = cargo?.FirstOrDefault(c => (string?)c!["itemTypeId"] == Rations);
                    if (stack is not null) cargo!.Remove(stack);
                }
            });
            var starving = Capacity(hungry)!;
            Assert.Equal(5, starving.FreeCabins);
            Assert.Equal(PassengerCapacityReasons.InsufficientFoodRations, starving.LifeSupportReason);
            Assert.Equal(PassengerCapacityReasons.InsufficientFoodRations, PassengerCapacityReasons.BoardingBlocker(starving));

            // Other scenarios get no transport capacity at all.
            using var tutorial = EngineContentLoader.CreateEngineFromScenarioFile(Settings, Path.Combine(Root(), "Scenarios", "Tutorial", "scenario.json"));
            Assert.Null(tutorial.CaptureSnapshot().DemoCampaign?.Transport?.PassengerCapacity);
            Assert.Equal(8, tutorial.CaptureSnapshot().CarrierOperations!.CabinReservation!.TotalCabins);
        }
        finally { Directory.Delete(directory, recursive: true); }
    }

    [Fact]
    public void SaveLoad_PreservesOutcome()
    {
        string directory = NewRun();
        try
        {
            using var engine = DemoWithPassengers(directory, 2);
            engine.SetSpeed(SimulationSpeed.Speed0);
            var expected = Capacity(engine);
            Assert.Equal(new PassengerCapacitySnapshot(12, 7, 2, 0, 3, null), expected);

            // The derived capacity is not written into the save.
            var save = JsonNode.Parse(ScenarioLoader.Serialize(engine.CaptureSaveState()))!;
            Assert.Null(save["gameState"]!["demoCampaign"]!["transport"]);

            using (var restored = Reload(engine, directory))
                Assert.Equal(expected, Capacity(restored));

            // Load never re-applies the demo manifest: a save without the third quarters stays at 8 cabins.
            using (var older = Reload(engine, directory, s =>
                   {
                       var modules = PlayerShip(s)["modules"]!.AsArray();
                       modules.Remove(modules.Single(m => (string?)m!["moduleId"] == "MOD-PLAYER-LIVING-QUARTERS-03"));
                   }))
            {
                var capacity = Capacity(older)!;
                Assert.Equal(8, capacity.TotalCabins);
                Assert.Equal(0, capacity.FreeCabins);
                Assert.Equal(PassengerCapacityReasons.CabinsOvercommitted, capacity.LifeSupportReason);
            }

            // A save that smuggles a capacity into persisted state is rejected before any session exists.
            Assert.Throws<ScenarioException>(() => Reload(engine, directory, s =>
                s["gameState"]!["demoCampaign"]!["transport"] = new JsonObject
                {
                    ["PassengerCapacity"] = new JsonObject
                    {
                        ["TotalCabins"] = 99, ["CrewCabins"] = 0, ["PassengerCabins"] = 0,
                        ["ReservedCabins"] = 0, ["FreeCabins"] = 99, ["LifeSupportReason"] = null,
                    },
                }).Dispose());
        }
        finally { Directory.Delete(directory, recursive: true); }
    }
}
