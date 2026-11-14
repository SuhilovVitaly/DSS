using System.Text.Json.Nodes;
using DeepSpaceSaga.Engine.Scenario;

namespace DeepSpaceSaga.Engine.Tests;

/// <summary>EP-0027-US-0001-TK-0001: optional scenarioMetadata.hidden flag.</summary>
public sealed class ScenarioMetadataHiddenTests
{
    private static string Json(string metadata) => $$"""
    {
      "scenarioMetadata": {{metadata}},
      "gameState": {
        "gameTimeMs": 0, "currentSpeed": "Speed0", "playerShipObjectId": "SPC-0001",
        "spaceObjects": [
          { "objectId": "SPC-0001", "objectType": "PlayerShip", "persistenceType": "Permanent",
            "positionX": 0, "positionY": 0, "speedMps": 0, "directionDegrees": 0, "movementType": "Stationary" }
        ]
      }
    }
    """;

    [Fact]
    public void Missing_hidden_loads_as_null()
    {
        var scenario = ScenarioLoader.LoadFromJson(Json("""{ "scenarioId": "default", "name": "Default" }"""));

        Assert.Null(scenario.Metadata.Hidden);
    }

    [Fact]
    public void Hidden_true_is_read_from_scenario_file()
    {
        var scenario = ScenarioLoader.LoadFromJson(Json("""{ "scenarioId": "tutorial", "name": "Tutorial", "hidden": true }"""));

        Assert.True(scenario.Metadata.Hidden);
    }

    [Fact]
    public void Null_hidden_is_omitted_when_serialized()
    {
        var scenario = ScenarioLoader.LoadFromJson(Json("""{ "scenarioId": "default", "name": "Default", "description": "d" }"""));

        var metadata = JsonNode.Parse(ScenarioLoader.Serialize(scenario))!["scenarioMetadata"]!.AsObject();

        Assert.False(metadata.ContainsKey("hidden"));
        Assert.Equal(["scenarioId", "name", "description"], metadata.Select(p => p.Key).ToArray());
    }

    [Fact]
    public void Hidden_true_round_trips_through_serialize()
    {
        var scenario = ScenarioLoader.LoadFromJson(Json("""{ "scenarioId": "tutorial", "name": "Tutorial", "hidden": true }"""));

        string json = ScenarioLoader.Serialize(scenario);

        Assert.True(JsonNode.Parse(json)!["scenarioMetadata"]!["hidden"]!.GetValue<bool>());
        Assert.True(ScenarioLoader.LoadFromJson(json).Metadata.Hidden);
    }
}
