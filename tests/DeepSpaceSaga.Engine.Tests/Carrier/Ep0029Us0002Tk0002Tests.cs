using System.Text.Json.Nodes;
using DeepSpaceSaga.Engine.Content;
using DeepSpaceSaga.Engine.Scenario;

namespace DeepSpaceSaga.Engine.Tests;

public sealed class Ep0029Us0002Tk0002Tests : IDisposable
{
    private readonly string _directory = Path.Combine(Path.GetTempPath(), $"dss-ep0029-us0002-tk0002-{Guid.NewGuid():N}");
    private string SettingsPath => Path.Combine(_directory, "settings.json");
    private string ScenarioPath => Path.Combine(_directory, "scenario.json");

    public Ep0029Us0002Tk0002Tests()
    {
        Directory.CreateDirectory(_directory);
        File.WriteAllText(Path.Combine(_directory, "categories.json"),
            """{"moduleTypes":[{"typeId":"living.quarters","displayName":"Living quarters","slotSize":1,"commandTypeIds":[]}]}""");
        File.WriteAllText(Path.Combine(_directory, "implementations.json"),
            """{"moduleImplementations":[{"typeId":"living.quarters.four-cabins","displayName":"Living","type":"living.quarters","massKg":2000,"structurePointsMax":150,"powerConsumptionW":0,"cabines":4}]}""");
        File.WriteAllText(Path.Combine(_directory, "commands.json"), """{"commandDefinitions":[]}""");
        File.WriteAllText(Path.Combine(_directory, "items.json"),
            """{"itemTypes":[{"typeId":"item.ice","displayName":"Ice","unitMassKg":1}]}""");
        File.WriteAllText(Path.Combine(_directory, "ships.json"),
            """{"shipClasses":[{"typeId":"ship.tetrarch","hullMax":400}]}""");
        File.WriteAllText(SettingsPath, """
            {"typeData":{"moduleTypes":"categories.json","moduleImplementations":"implementations.json",
              "commandDefinitions":"commands.json","itemTypes":"items.json","shipClasses":"ships.json"},
              "defaultScenario":"scenario.json"}
            """);
    }

    public void Dispose()
    {
        try { Directory.Delete(_directory, recursive: true); } catch { /* best effort */ }
    }

    private SimulationEngine Load() => EngineContentLoader.CreateEngineFromSettingsFile(SettingsPath);

    private static JsonObject HullLayout()
    {
        var cells = new JsonArray();
        for (int y = 0; y < 5; y++)
            for (int x = 0; x < 5; x++)
                cells.Add(new JsonObject { ["x"] = x, ["y"] = y });
        return new JsonObject { ["width"] = 5, ["height"] = 5, ["cells"] = cells };
    }

    private static JsonObject LivingModule(string moduleId, int x, int sp = 150, bool destroyed = false) =>
        new()
        {
            ["moduleId"] = moduleId,
            ["moduleTypeId"] = "living.quarters.four-cabins",
            ["occupiedCells"] = new JsonArray(new JsonObject { ["x"] = x, ["y"] = 0 }),
            ["structurePoints"] = sp,
            ["powerState"] = "On",
            ["operationalState"] = "Ready",
            ["cargo"] = new JsonArray(),
            ["destroyed"] = destroyed,
        };

    private static JsonObject CrewMember(string id, string name) => new()
    {
        ["crewId"] = id,
        ["displayName"] = name,
        ["accuracySkill"] = 0,
        ["maneuverabilitySkill"] = 0,
    };

    private void WriteScenario(JsonObject[] modules, JsonObject[] crew)
    {
        var ship = new JsonObject
        {
            ["objectId"] = "SPC-0001",
            ["objectType"] = "PlayerShip",
            ["shipClassId"] = "ship.tetrarch",
            ["persistenceType"] = "Permanent",
            ["name"] = "Tetrarch",
            ["positionX"] = 0,
            ["positionY"] = 0,
            ["speedMps"] = 0,
            ["directionDegrees"] = 0,
            ["movementType"] = "Stationary",
            ["isKnown"] = true,
            ["hullLayout"] = HullLayout(),
            ["modules"] = new JsonArray(modules.Select(m => (JsonNode?)m).ToArray()),
            ["crew"] = new JsonArray(crew.Select(c => (JsonNode?)c).ToArray()),
        };
        var scenario = new JsonObject
        {
            ["scenarioMetadata"] = new JsonObject { ["scenarioId"] = "tk0002", ["name"] = "tk0002" },
            ["gameState"] = new JsonObject
            {
                ["masterSeed"] = 42,
                ["gameTimeMs"] = 0,
                ["currentSpeed"] = "Speed0",
                ["playerShipObjectId"] = "SPC-0001",
                ["spaceObjects"] = new JsonArray(ship),
            },
        };
        File.WriteAllText(ScenarioPath, scenario.ToJsonString());
    }

    private static JsonObject[] SixCrew() => new[]
    {
        CrewMember("CHR-0001", "Dunkan Su"),
        CrewMember("CHR-PLAYER-DEFENSE", "Irina Volskaya"),
        CrewMember("CHR-PLAYER-EVELYN", "Evelyn Ward"),
        CrewMember("CHR-PLAYER-AUDREY", "Audrey Bennett"),
        CrewMember("CHR-PLAYER-CLAIRE", "Claire Morgan"),
        CrewMember("CHR-PLAYER-RACHEL", "Rachel Hayes"),
    };

    [Fact]
    public void EightCabinsSixPeopleTwoReserved()
    {
        WriteScenario(new[] { LivingModule("MOD-LQ-01", 0), LivingModule("MOD-LQ-02", 1) }, SixCrew());
        using var engine = Load();
        var snapshot = engine.CaptureSnapshot();

        var reservation = snapshot.CarrierOperations!.CabinReservation!;
        Assert.Equal(8, reservation.TotalCabins);
        Assert.Equal(6, reservation.OccupiedCabins);
        Assert.Equal(2, reservation.ReservedCabins);
        Assert.Equal(0, reservation.FreeCabins);
        Assert.Equal(0, reservation.PrisonerCount);
        Assert.Equal(6, snapshot.CarrierOperations.Crew.Length);
    }

    [Fact]
    public void DuplicateCrewRejected()
    {
        WriteScenario(new[] { LivingModule("MOD-LQ-01", 0) },
            new[] { CrewMember("CHR-0001", "A"), CrewMember("CHR-0001", "B") });
        Assert.Throws<ScenarioException>(() => Load());
    }

    [Fact]
    public void HousingDamagePreservesCrew()
    {
        WriteScenario(new[] { LivingModule("MOD-LQ-01", 0, sp: 0), LivingModule("MOD-LQ-02", 1) }, SixCrew());
        using var engine = Load();
        var snapshot = engine.CaptureSnapshot();

        var reservation = snapshot.CarrierOperations!.CabinReservation!;
        Assert.Equal(4, reservation.TotalCabins); // only one four-cabin module is operational
        Assert.Equal(6, reservation.OccupiedCabins);
        Assert.Equal(2, reservation.ReservedCabins);
        Assert.Equal(0, reservation.FreeCabins);
        Assert.True(reservation.TotalCabins < reservation.OccupiedCabins, "damage shows a cabin deficit");

        var crew = snapshot.CarrierOperations.Crew;
        Assert.Equal(6, crew.Length);
        Assert.Equal(new[] { "CHR-0001", "CHR-PLAYER-DEFENSE", "CHR-PLAYER-EVELYN", "CHR-PLAYER-AUDREY", "CHR-PLAYER-CLAIRE", "CHR-PLAYER-RACHEL" },
            crew.Select(c => c.Id));
    }
}
