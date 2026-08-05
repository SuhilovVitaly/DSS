using DeepSpaceSaga.Engine.Scenario;

namespace DeepSpaceSaga.Engine.Tests;

public class ScenarioLoaderTests
{
    private const string ValidJson = """
    {
      "scenarioMetadata": { "scenarioId": "default", "name": "Default" },
      "gameState": {
        "gameTimeMs": 0,
        "currentSpeed": "Speed1",
        "playerShipObjectId": "SPC-0001",
        "focus": { "mode": "Attached", "objectId": "SPC-0001" },
        "spaceObjects": [
          {
            "objectId": "SPC-0001", "objectType": "PlayerShip",
            "persistenceType": "Permanent", "name": "Ship",
            "positionX": 10000.0, "positionY": 10000.0,
            "speedMps": 0, "directionDegrees": 0, "movementType": "Stationary"
          },
          {
            "objectId": "SPC-0002", "objectType": "Station",
            "persistenceType": "Permanent", "name": "Station",
            "positionX": 11000.0, "positionY": 11000.0,
            "speedMps": 0, "directionDegrees": 0, "movementType": "Stationary"
          }
        ]
      }
    }
    """;

    [Fact]
    public void LoadFromJson_reads_valid_scenario()
    {
        var scenario = ScenarioLoader.LoadFromJson(ValidJson);
        Assert.NotNull(scenario);
        Assert.Equal("default", scenario.Metadata.ScenarioId);
    }

    [Fact]
    public void LoadFromJson_has_correct_object_count()
    {
        var scenario = ScenarioLoader.LoadFromJson(ValidJson);
        Assert.Equal(2, scenario.GameState.SpaceObjects.Count);
    }

    [Fact]
    public void LoadFromJson_finds_player_ship()
    {
        var scenario = ScenarioLoader.LoadFromJson(ValidJson);
        var ship = scenario.GameState.SpaceObjects
            .First(o => o.ObjectId == scenario.GameState.PlayerShipObjectId);
        Assert.Equal("PlayerShip", ship.ObjectType);
    }

    [Fact]
    public void LoadFromJson_reads_speed_and_game_time()
    {
        var scenario = ScenarioLoader.LoadFromJson(ValidJson);
        Assert.Equal(0, scenario.GameState.GameTimeMs);
        Assert.Equal("Speed1", scenario.GameState.CurrentSpeed);
    }

    [Fact]
    public void LoadFromJson_throws_on_invalid_json()
    {
        Assert.Throws<ScenarioException>(() => ScenarioLoader.LoadFromJson("{invalid}"));
    }

    [Fact]
    public void LoadFromJson_throws_on_missing_gameState()
    {
        var json = """{ "scenarioMetadata": { "scenarioId": "x", "name": "x" } }""";
        Assert.Throws<ScenarioException>(() => ScenarioLoader.LoadFromJson(json));
    }

    [Fact]
    public void LoadFromJson_throws_on_missing_playerShipObjectId()
    {
        var json = """
        {
          "scenarioMetadata": { "scenarioId": "x", "name": "x" },
          "gameState": { "gameTimeMs": 0, "currentSpeed": "Speed1", "spaceObjects": [] }
        }
        """;
        Assert.Throws<ScenarioException>(() => ScenarioLoader.LoadFromJson(json));
    }

    [Fact]
    public void LoadFromJson_throws_on_player_ship_not_found()
    {
        var json = """
        {
          "scenarioMetadata": { "scenarioId": "x", "name": "x" },
          "gameState": {
            "gameTimeMs": 0, "currentSpeed": "Speed1",
            "playerShipObjectId": "MISSING",
            "spaceObjects": [
              { "objectId": "X-1", "objectType": "Station", "persistenceType": "Permanent",
                "positionX": 0, "positionY": 0, "speedMps": 0, "directionDegrees": 0,
                "movementType": "Stationary" }
            ]
          }
        }
        """;
        Assert.Throws<ScenarioException>(() => ScenarioLoader.LoadFromJson(json));
    }

    [Fact]
    public void LoadFromJson_throws_on_duplicate_objectIds()
    {
        var json = """
        {
          "scenarioMetadata": { "scenarioId": "x", "name": "x" },
          "gameState": {
            "gameTimeMs": 0, "currentSpeed": "Speed1",
            "playerShipObjectId": "DUP",
            "spaceObjects": [
              { "objectId": "DUP", "objectType": "PlayerShip", "persistenceType": "Permanent",
                "positionX": 0, "positionY": 0, "speedMps": 0, "directionDegrees": 0,
                "movementType": "Stationary" },
              { "objectId": "DUP", "objectType": "Station", "persistenceType": "Permanent",
                "positionX": 0, "positionY": 0, "speedMps": 0, "directionDegrees": 0,
                "movementType": "Stationary" }
            ]
          }
        }
        """;
        Assert.Throws<ScenarioException>(() => ScenarioLoader.LoadFromJson(json));
    }

    [Fact]
    public void LoadFromJson_throws_on_player_ship_wrong_type()
    {
        var json = """
        {
          "scenarioMetadata": { "scenarioId": "x", "name": "x" },
          "gameState": {
            "gameTimeMs": 0, "currentSpeed": "Speed1",
            "playerShipObjectId": "SHIP",
            "spaceObjects": [
              { "objectId": "SHIP", "objectType": "Station", "persistenceType": "Permanent",
                "positionX": 0, "positionY": 0, "speedMps": 0, "directionDegrees": 0,
                "movementType": "Stationary" }
            ]
          }
        }
        """;
        Assert.Throws<ScenarioException>(() => ScenarioLoader.LoadFromJson(json));
    }

    [Fact]
    public void LoadFromJson_throws_on_unknown_objectType()
    {
        var json = """
        {
          "scenarioMetadata": { "scenarioId": "x", "name": "x" },
          "gameState": {
            "gameTimeMs": 0, "currentSpeed": "Speed1",
            "playerShipObjectId": "SHIP",
            "spaceObjects": [
              { "objectId": "SHIP", "objectType": "PlayerShip", "persistenceType": "Permanent",
                "positionX": 0, "positionY": 0, "speedMps": 0, "directionDegrees": 0,
                "movementType": "Stationary" },
              { "objectId": "X-1", "objectType": "Alien", "persistenceType": "Permanent",
                "positionX": 0, "positionY": 0, "speedMps": 0, "directionDegrees": 0,
                "movementType": "Stationary" }
            ]
          }
        }
        """;
        Assert.Throws<ScenarioException>(() => ScenarioLoader.LoadFromJson(json));
    }

    [Fact]
    public void LoadFromJson_throws_on_direction_out_of_range()
    {
        var json = """
        {
          "scenarioMetadata": { "scenarioId": "x", "name": "x" },
          "gameState": {
            "gameTimeMs": 0, "currentSpeed": "Speed1",
            "playerShipObjectId": "SHIP",
            "spaceObjects": [
              { "objectId": "SHIP", "objectType": "PlayerShip", "persistenceType": "Permanent",
                "positionX": 0, "positionY": 0, "speedMps": 0, "directionDegrees": 360,
                "movementType": "Stationary" }
            ]
          }
        }
        """;
        Assert.Throws<ScenarioException>(() => ScenarioLoader.LoadFromJson(json));
    }

    [Fact]
    public void LoadFromJson_throws_on_non_finite_coordinates()
    {
        var json = """
        {
          "scenarioMetadata": { "scenarioId": "x", "name": "x" },
          "gameState": {
            "gameTimeMs": 0, "currentSpeed": "Speed1",
            "playerShipObjectId": "SHIP",
            "spaceObjects": [
              { "objectId": "SHIP", "objectType": "PlayerShip", "persistenceType": "Permanent",
                "positionX": null, "positionY": 0, "speedMps": 0, "directionDegrees": 0,
                "movementType": "Stationary" }
            ]
          }
        }
        """;
        Assert.Throws<ScenarioException>(() => ScenarioLoader.LoadFromJson(json));
    }

    // Valid default scenario with 4 objects including 2 asteroids
    private const string ValidDefaultScenario = """
    {
      "scenarioMetadata": { "scenarioId": "default", "name": "Default Scenario" },
      "gameState": {
        "gameTimeMs": 0, "currentSpeed": "Speed0",
        "playerShipObjectId": "SPC-0001",
        "focus": { "mode": "Attached", "objectId": "SPC-0001" },
        "spaceObjects": [
          { "objectId": "SPC-0001", "objectType": "PlayerShip", "persistenceType": "Permanent",
            "positionX": 10000, "positionY": 10000, "speedMps": 0, "directionDegrees": 0,
            "movementType": "Stationary" },
          { "objectId": "SPC-0002", "objectType": "Station", "persistenceType": "Permanent",
            "positionX": 11000, "positionY": 11000, "speedMps": 0, "directionDegrees": 0,
            "movementType": "Stationary" },
          { "objectId": "SPC-0003", "objectType": "Asteroid", "persistenceType": "Temporary",
            "positionX": 10400, "positionY": 10000, "speedMps": 100, "directionDegrees": 270,
            "movementType": "Linear", "massKg": 1000000, "compositionType": "Silicate" },
          { "objectId": "SPC-0004", "objectType": "Asteroid", "persistenceType": "Temporary",
            "positionX": 10000, "positionY": 10450, "speedMps": 200, "directionDegrees": 0,
            "movementType": "Linear", "massKg": 1000000, "compositionType": "Ice" }
        ]
      }
    }
    """;

    [Fact]
    public void Default_scenario_loads_with_4_objects()
    {
        var scenario = ScenarioLoader.LoadFromJson(ValidDefaultScenario);
        Assert.Equal(4, scenario.GameState.SpaceObjects.Count);
    }

    [Fact]
    public void Default_scenario_player_ship_is_SPC_0001()
    {
        var scenario = ScenarioLoader.LoadFromJson(ValidDefaultScenario);
        Assert.Equal("SPC-0001", scenario.GameState.PlayerShipObjectId);
    }

    [Fact]
    public void Default_scenario_has_correct_speed()
    {
        var scenario = ScenarioLoader.LoadFromJson(ValidDefaultScenario);
        Assert.Equal("Speed0", scenario.GameState.CurrentSpeed);
    }

    [Fact]
    public void Asteroid_speed_out_of_range_throws()
    {
        var json = """
        {
          "scenarioMetadata": { "scenarioId": "x", "name": "x" },
          "gameState": {
            "gameTimeMs": 0, "currentSpeed": "Speed1",
            "playerShipObjectId": "SHIP",
            "spaceObjects": [
              { "objectId": "SHIP", "objectType": "PlayerShip", "persistenceType": "Permanent",
                "positionX": 0, "positionY": 0, "speedMps": 0, "directionDegrees": 0,
                "movementType": "Stationary" },
              { "objectId": "AST", "objectType": "Asteroid", "persistenceType": "Temporary",
                "positionX": 0, "positionY": 0, "speedMps": 50, "directionDegrees": 0,
                "movementType": "Linear", "massKg": 5000000 }
            ]
          }
        }
        """;
        Assert.Throws<ScenarioException>(() => ScenarioLoader.LoadFromJson(json));
    }

    [Fact]
    public void Null_element_in_spaceObjects_throws()
    {
        var json = """
        {
          "scenarioMetadata": { "scenarioId": "x", "name": "x" },
          "gameState": {
            "gameTimeMs": 0, "currentSpeed": "Speed1",
            "playerShipObjectId": "SHIP",
            "spaceObjects": [
              { "objectId": "SHIP", "objectType": "PlayerShip", "persistenceType": "Permanent",
                "positionX": 0, "positionY": 0, "speedMps": 0, "directionDegrees": 0,
                "movementType": "Stationary" },
              null
            ]
          }
        }
        """;
        Assert.Throws<ScenarioException>(() => ScenarioLoader.LoadFromJson(json));
    }

    [Fact]
    public void Module_instance_rejects_embedded_type_definition_data()
    {
        var json = """
        {
          "scenarioMetadata": { "scenarioId": "x", "name": "x" },
          "gameState": {
            "gameTimeMs": 0, "currentSpeed": "Speed1",
            "playerShipObjectId": "SHIP",
            "spaceObjects": [
              {
                "objectId": "SHIP", "objectType": "PlayerShip", "persistenceType": "Permanent",
                "positionX": 0, "positionY": 0, "speedMps": 0, "directionDegrees": 0,
                "movementType": "Stationary",
                "modules": [
                  {
                    "moduleId": "MOD-1",
                    "moduleType": "Container",
                    "slotSize": 4,
                    "moduleTypeId": "module.container.basic",
                    "platformIndex": 0,
                    "occupiedCells": [1],
                    "massKg": 20000,
                    "structurePoints": 400,
                    "structurePointsMax": 400,
                    "powerState": "On",
                    "operationalState": "Ready",
                    "cargo": [
                      {
                        "itemTypeId": "item.energy-cells",
                        "resourceType": "Energy Cells",
                        "quantity": 1,
                        "unitMassKg": 10
                      }
                    ]
                  }
                ]
              }
            ]
          }
        }
        """;

        Assert.Throws<ScenarioException>(() => ScenarioLoader.LoadFromJson(json));
    }

    [Fact]
    public void Asteroid_mass_out_of_range_throws()
    {
        var json = """
        {
          "scenarioMetadata": { "scenarioId": "x", "name": "x" },
          "gameState": {
            "gameTimeMs": 0, "currentSpeed": "Speed1",
            "playerShipObjectId": "SHIP",
            "spaceObjects": [
              { "objectId": "SHIP", "objectType": "PlayerShip", "persistenceType": "Permanent",
                "positionX": 0, "positionY": 0, "speedMps": 0, "directionDegrees": 0,
                "movementType": "Stationary" },
              { "objectId": "AST", "objectType": "Asteroid", "persistenceType": "Temporary",
                "positionX": 0, "positionY": 0, "speedMps": 500, "directionDegrees": 0,
                "movementType": "Linear", "massKg": 500 }
            ]
          }
        }
        """;
        Assert.Throws<ScenarioException>(() => ScenarioLoader.LoadFromJson(json));
    }
}
