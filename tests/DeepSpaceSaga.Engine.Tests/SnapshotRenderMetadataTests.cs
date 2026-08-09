using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Engine.Scenario;

namespace DeepSpaceSaga.Engine.Tests;

/// <summary>
/// TЗ-09: BuildSnapshot must project render metadata through the
/// IsKnown gate — the client may only see factual data (type, relation,
/// name) for known objects; unknown objects get the sentinel
/// RenderObjectType and null factual fields. The player ship is always
/// known, even for legacy scenarios without isKnown.
/// </summary>
public class SnapshotRenderMetadataTests
{
    private const string ProjectionScenarioJson = """
    {
      "scenarioMetadata": { "scenarioId": "render-projection", "name": "Render Projection" },
      "gameState": {
        "gameTimeMs": 0, "currentSpeed": "Speed0",
        "playerShipObjectId": "SPC-0001",
        "spaceObjects": [
          { "objectId": "SPC-0001", "objectType": "PlayerShip", "persistenceType": "Permanent",
            "name": "Player Ship",
            "positionX": 10000, "positionY": 10000, "speedMps": 0, "directionDegrees": 0,
            "movementType": "Stationary" },
          { "objectId": "SPC-0002", "objectType": "Station", "persistenceType": "Permanent",
            "name": "Start Station",
            "positionX": 11000, "positionY": 11000, "speedMps": 0, "directionDegrees": 0,
            "movementType": "Stationary", "isKnown": true },
          { "objectId": "SPC-0003", "objectType": "Asteroid", "persistenceType": "Temporary",
            "positionX": 10400, "positionY": 10000, "speedMps": 100, "directionDegrees": 270,
            "movementType": "Linear", "massKg": 1000000, "compositionType": "Silicate" },
          { "objectId": "SPC-0004", "objectType": "Asteroid", "persistenceType": "Temporary",
            "positionX": 12000, "positionY": 12000, "speedMps": 300, "directionDegrees": 45,
            "movementType": "Linear", "massKg": 2000000, "compositionType": "Silicate",
            "isKnown": true }
        ]
      }
    }
    """;

    private static AuthoritativeSnapshot CaptureSnapshot()
    {
        var scenario = ScenarioLoader.LoadFromJson(ProjectionScenarioJson);
        var engine = new SimulationEngine();
        engine.LoadScenario(scenario);
        return engine.CaptureSnapshotForTests(gameTimeMs: 0, SimulationSpeed.Speed0);
    }

    [Fact]
    public void Unknown_asteroid_projects_sentinel_and_null_factuals()
    {
        var snapshot = CaptureSnapshot();
        var asteroid = snapshot.Objects.Single(o => o.ObjectId == "SPC-0003");

        Assert.Null(asteroid.ObjectType);
        Assert.Null(asteroid.DisplayName);
        Assert.Null(asteroid.RelationToPlayer);
        Assert.Equal(SpaceObjectType.UnknownSpaceObject, asteroid.RenderObjectType);
    }

    [Fact]
    public void Known_station_projects_factual_type_and_name()
    {
        var snapshot = CaptureSnapshot();
        var station = snapshot.Objects.Single(o => o.ObjectId == "SPC-0002");

        Assert.Equal(SpaceObjectType.Station, station.ObjectType);
        Assert.Equal(SpaceObjectType.Station, station.RenderObjectType);
        Assert.Equal("Start Station", station.DisplayName);
    }

    [Fact]
    public void Player_ship_without_isKnown_is_always_known()
    {
        var snapshot = CaptureSnapshot();
        var ship = snapshot.Objects.Single(o => o.ObjectId == "SPC-0001");

        Assert.Equal(SpaceObjectType.PlayerShip, ship.ObjectType);
        Assert.Equal(SpaceObjectType.PlayerShip, ship.RenderObjectType);
        Assert.Equal("Player Ship", ship.DisplayName);
        Assert.Equal(PlayerRelation.Self, ship.RelationToPlayer);
    }

    [Fact]
    public void Known_asteroid_projects_factual_type()
    {
        var snapshot = CaptureSnapshot();
        var asteroid = snapshot.Objects.Single(o => o.ObjectId == "SPC-0004");

        Assert.Equal(SpaceObjectType.Asteroid, asteroid.ObjectType);
        Assert.Equal(SpaceObjectType.Asteroid, asteroid.RenderObjectType);
    }

    // Note: NpcShip objects cannot be loaded through ScenarioLoader — the
    // validator's KnownObjectTypes is limited to PlayerShip/Station/Asteroid
    // (domain model, out of scope for TЗ-09). The known → neutral relation
    // mapping lives in SimulationEngine.GetRelationToPlayer and is exercised
    // for the player ship (self) above.
}
