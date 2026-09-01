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
    public void LoadFromJson_missing_description_defaults_to_null()
    {
        // Backward compatibility: scenario/save files predating the description field
        // (and every save file, which never sets it) must still load.
        var scenario = ScenarioLoader.LoadFromJson(ValidJson);
        Assert.Null(scenario.Metadata.Description);
    }

    [Fact]
    public void LoadFromJson_reads_description_when_present()
    {
        const string json = """
        {
          "scenarioMetadata": { "scenarioId": "default", "name": "Default", "description": "A starter scenario." },
          "gameState": {
            "gameTimeMs": 0, "currentSpeed": "Speed1", "playerShipObjectId": "SPC-0001",
            "spaceObjects": [
              { "objectId": "SPC-0001", "objectType": "PlayerShip", "persistenceType": "Permanent",
                "positionX": 0, "positionY": 0, "speedMps": 0, "directionDegrees": 0, "movementType": "Stationary" }
            ]
          }
        }
        """;

        var scenario = ScenarioLoader.LoadFromJson(json);
        Assert.Equal("A starter scenario.", scenario.Metadata.Description);
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
                    "occupiedCells": [ { "x": 1, "y": 0 } ],
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

    // 4.1 backward compatibility: saveFormatVersion / isKnown are new, optional fields.
    // Existing scenario files that don't mention them must keep loading unchanged.
    [Fact]
    public void LoadFromJson_defaults_saveFormatVersion_and_isKnown_when_absent()
    {
        var scenario = ScenarioLoader.LoadFromJson(ValidJson);

        Assert.Equal(0, scenario.SaveFormatVersion);
        Assert.All(scenario.GameState.SpaceObjects, o => Assert.False(o.IsKnown));
    }

    [Fact]
    public void LoadFromJson_reads_saveFormatVersion_and_isKnown_when_present()
    {
        var json = """
        {
          "scenarioMetadata": { "scenarioId": "x", "name": "x" },
          "saveFormatVersion": 1,
          "gameState": {
            "gameTimeMs": 0, "currentSpeed": "Speed1",
            "playerShipObjectId": "SHIP",
            "spaceObjects": [
              { "objectId": "SHIP", "objectType": "PlayerShip", "persistenceType": "Permanent",
                "positionX": 0, "positionY": 0, "speedMps": 0, "directionDegrees": 0,
                "movementType": "Stationary", "isKnown": true }
            ]
          }
        }
        """;

        var scenario = ScenarioLoader.LoadFromJson(json);

        Assert.Equal(1, scenario.SaveFormatVersion);
        Assert.True(scenario.GameState.SpaceObjects.Single().IsKnown);
    }

    // gameTimeMs > 0 — New Game rejects it by default; the explicit save-load
    // mode (allowNonZeroGameTime: true) accepts it. Regression coverage for 4.2:
    // the default parameter value must keep every existing (New Game) call site
    // behaving exactly as before.
    private const string NonZeroGameTimeJson = """
    {
      "scenarioMetadata": { "scenarioId": "x", "name": "x" },
      "gameState": {
        "gameTimeMs": 42000, "currentSpeed": "Speed1",
        "playerShipObjectId": "SHIP",
        "spaceObjects": [
          { "objectId": "SHIP", "objectType": "PlayerShip", "persistenceType": "Permanent",
            "positionX": 0, "positionY": 0, "speedMps": 0, "directionDegrees": 0,
            "movementType": "Stationary" }
        ]
      }
    }
    """;

    [Fact]
    public void LoadFromJson_throws_on_nonzero_gameTime_by_default()
    {
        Assert.Throws<ScenarioException>(() => ScenarioLoader.LoadFromJson(NonZeroGameTimeJson));
    }

    [Fact]
    public void LoadFromJson_throws_on_nonzero_gameTime_when_explicitly_disallowed()
    {
        Assert.Throws<ScenarioException>(
            () => ScenarioLoader.LoadFromJson(NonZeroGameTimeJson, allowNonZeroGameTime: false));
    }

    [Fact]
    public void LoadFromJson_allows_nonzero_gameTime_when_explicitly_permitted()
    {
        var scenario = ScenarioLoader.LoadFromJson(NonZeroGameTimeJson, allowNonZeroGameTime: true);
        Assert.Equal(42000, scenario.GameState.GameTimeMs);
    }

    [Fact]
    public void LoadFromFile_allows_nonzero_gameTime_when_explicitly_permitted()
    {
        string path = Path.Combine(Path.GetTempPath(), $"dss-save-{Guid.NewGuid():N}.json");
        File.WriteAllText(path, NonZeroGameTimeJson);
        try
        {
            var scenario = ScenarioLoader.LoadFromFile(path, allowNonZeroGameTime: true);
            Assert.Equal(42000, scenario.GameState.GameTimeMs);
        }
        finally
        {
            File.Delete(path);
        }
    }

    // requirements §57 form-check: a negative hull-cell coordinate is rejected at the
    // ScenarioLoader.ValidateModule level (before any hull-layout/domain validation runs).
    [Fact]
    public void LoadFromJson_throws_on_negative_module_cell_coordinate()
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
                "hullLayout": { "width": 1, "height": 1, "cells": [ { "x": 0, "y": 0 } ] },
                "modules": [
                  {
                    "moduleId": "MOD-1",
                    "moduleTypeId": "module.container.basic",
                    "occupiedCells": [ { "x": -1, "y": 0 } ],
                    "structurePoints": 400,
                    "powerState": "On",
                    "operationalState": "Ready",
                    "cargo": []
                  }
                ]
              }
            ]
          }
        }
        """;

        var ex = Assert.Throws<ScenarioException>(() => ScenarioLoader.LoadFromJson(json));
        Assert.Contains("negative coordinate", ex.Message, StringComparison.Ordinal);
    }

    // story-20260901-112254 (Batch A, U1): the scenario "crew" field is a plain nullable
    // list, no domain validation at the ScenarioLoader level (see SimulationEngine.
    // ResolveShipCrew for the engine-level id checks) — this only covers the schema
    // deserializes and defaults correctly.
    [Fact]
    public void LoadFromJson_reads_crew_when_present()
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
                "movementType": "Stationary",
                "crew": [ { "crewId": "CHR-0001", "displayName": "Dunkan Su" } ] }
            ]
          }
        }
        """;

        var scenario = ScenarioLoader.LoadFromJson(json);
        var ship = scenario.GameState.SpaceObjects.Single(o => o.ObjectId == "SHIP");

        var crewMember = Assert.Single(ship.Crew!);
        Assert.Equal("CHR-0001", crewMember.CrewId);
        Assert.Equal("Dunkan Su", crewMember.DisplayName);
    }

    [Fact]
    public void LoadFromJson_defaults_crew_to_null_when_absent()
    {
        var scenario = ScenarioLoader.LoadFromJson(ValidJson);
        var ship = scenario.GameState.SpaceObjects
            .Single(o => o.ObjectId == scenario.GameState.PlayerShipObjectId);

        Assert.Null(ship.Crew);
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
