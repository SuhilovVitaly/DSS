using System.Linq;
using DeepSpaceSaga.Engine.Scenario;

namespace DeepSpaceSaga.Engine.Tests;

/// <summary>
/// LoadScenario assigns a graphical representation (SpaceObjectData.Image) when a scenario
/// does not already specify one: the player ship always gets the Tetrarch sprite, and each
/// asteroid gets a deterministic pick from the Ice or regular sprite pool depending on its
/// compositionType.
/// </summary>
public class ObjectImageResolutionTests
{
    private const string ShipObjectId = "SPC-0001";
    private const string SilicateAsteroidId = "SPC-0003";
    private const string IceAsteroidId = "SPC-0004";

    private static string ScenarioJson(ulong masterSeed, string? explicitAsteroidImage = null)
    {
        string explicitImageField = explicitAsteroidImage is not null
            ? $"\"image\": \"{explicitAsteroidImage}\","
            : "";

        return $$"""
        {
          "scenarioMetadata": { "scenarioId": "test", "name": "Test" },
          "gameState": {
            "masterSeed": {{masterSeed}},
            "gameTimeMs": 0, "currentSpeed": "Speed0",
            "playerShipObjectId": "{{ShipObjectId}}",
            "spaceObjects": [
              { "objectId": "{{ShipObjectId}}", "objectType": "PlayerShip", "persistenceType": "Permanent",
                "positionX": 10000, "positionY": 10000, "speedMps": 0, "directionDegrees": 0,
                "movementType": "Stationary" },
              { "objectId": "{{SilicateAsteroidId}}", "objectType": "Asteroid", "persistenceType": "Temporary",
                {{explicitImageField}}
                "positionX": 10400, "positionY": 10000, "speedMps": 100, "directionDegrees": 270,
                "movementType": "Linear", "massKg": 1000000, "compositionType": "Silicate" },
              { "objectId": "{{IceAsteroidId}}", "objectType": "Asteroid", "persistenceType": "Temporary",
                "positionX": 10000, "positionY": 10450, "speedMps": 200, "directionDegrees": 0,
                "movementType": "Linear", "massKg": 1000000, "compositionType": "Ice" }
            ]
          }
        }
        """;
    }

    private static SimulationEngine LoadEngine(ulong masterSeed, string? explicitAsteroidImage = null)
    {
        var engine = new SimulationEngine();
        engine.LoadScenario(ScenarioLoader.LoadFromJson(ScenarioJson(masterSeed, explicitAsteroidImage)));
        return engine;
    }

    [Fact]
    public void Player_ship_gets_the_Tetrarch_sprite()
    {
        var engine = LoadEngine(masterSeed: 1UL);
        var ship = engine.RuntimeObjects.Single(o => o.InitialMotion.ObjectId == ShipObjectId);

        Assert.Equal("Images/CelestialObjects/Spacecraft/ship-tetrarch-class.png", ship.Image);
    }

    [Fact]
    public void Silicate_asteroid_gets_a_regular_asteroid_sprite()
    {
        var engine = LoadEngine(masterSeed: 1UL);
        var asteroid = engine.RuntimeObjects.Single(o => o.InitialMotion.ObjectId == SilicateAsteroidId);

        Assert.NotNull(asteroid.Image);
        Assert.Matches(@"^Images/CelestialObjects/Asteroid/asteroid-\d+\.png$", asteroid.Image!);
    }

    [Fact]
    public void Ice_asteroid_gets_an_ice_asteroid_sprite()
    {
        var engine = LoadEngine(masterSeed: 1UL);
        var asteroid = engine.RuntimeObjects.Single(o => o.InitialMotion.ObjectId == IceAsteroidId);

        Assert.NotNull(asteroid.Image);
        Assert.Matches(@"^Images/CelestialObjects/Asteroid/ice-asteroid-\d+\.png$", asteroid.Image!);
    }

    [Fact]
    public void Asteroid_image_is_deterministic_for_the_same_masterSeed()
    {
        var first = LoadEngine(masterSeed: 12345UL);
        var second = LoadEngine(masterSeed: 12345UL);

        var firstImage = first.RuntimeObjects.Single(o => o.InitialMotion.ObjectId == SilicateAsteroidId).Image;
        var secondImage = second.RuntimeObjects.Single(o => o.InitialMotion.ObjectId == SilicateAsteroidId).Image;

        Assert.Equal(firstImage, secondImage);
    }

    [Fact]
    public void Explicit_scenario_image_is_kept_as_is()
    {
        var engine = LoadEngine(masterSeed: 1UL, explicitAsteroidImage: "Images/Custom/my-rock.png");
        var asteroid = engine.RuntimeObjects.Single(o => o.InitialMotion.ObjectId == SilicateAsteroidId);

        Assert.Equal("Images/Custom/my-rock.png", asteroid.Image);
    }

    [Fact]
    public void Resolved_asteroid_image_round_trips_through_save()
    {
        var engine = LoadEngine(masterSeed: 1UL);
        var loaded = engine.RuntimeObjects.Single(o => o.InitialMotion.ObjectId == SilicateAsteroidId).Image;

        var saved = engine.CaptureSaveState();
        var savedAsteroid = saved.GameState.SpaceObjects.Single(o => o.ObjectId == SilicateAsteroidId);

        Assert.Equal(loaded, savedAsteroid.Image);
    }
}
