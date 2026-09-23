using DeepSpaceSaga.Engine.Scenario;

namespace DeepSpaceSaga.Engine.Tests;

public sealed class TradingMapSchemaTests
{
    [Fact]
    public void Legacy_scenario_without_map_fields_roundtrips()
    {
        var scenario = ScenarioLoader.LoadFromJson(BaseScenarioJson);
        var roundTrip = ScenarioLoader.LoadFromJson(ScenarioLoader.Serialize(scenario));

        Assert.Null(roundTrip.GameState.TradingMapGeneration);
        Assert.Null(roundTrip.GameState.TradingMap);
        Assert.Equal(scenario.GameState.SpaceObjects, roundTrip.GameState.SpaceObjects);
    }

    [Fact]
    public void Generation_and_materialized_map_each_roundtrip_all_fields()
    {
        var request = ScenarioLoader.LoadFromJson(ScenarioLoader.Serialize(Scenario(generation: Rules())));
        Assert.NotNull(request.GameState.TradingMapGeneration);
        Assert.Equal("ST-TRANSIT", request.GameState.TradingMapGeneration!.StartStationObjectId);

        var materialized = ScenarioLoader.LoadFromJson(ScenarioLoader.Serialize(Scenario(map: MaterializedMap())));
        var map = materialized.GameState.TradingMap!;
        Assert.Equal(1, map.SchemaVersion);
        Assert.Equal("ring", map.TemplateId);
        Assert.Equal(2, map.Edges.Count);
        Assert.Equal(2, map.CargoFlows.Count);
        Assert.Equal(["TradingMap.Geometry", "TradingMap.Topology"], map.RngStreams.Select(s => s.Name));
        Assert.Equal(123UL, materialized.GameState.MasterSeed);
    }

    [Fact]
    public void Request_and_result_are_mutually_exclusive()
    {
        var scenario = Scenario(generation: Rules(), map: MaterializedMap());

        var error = Assert.Throws<ScenarioException>(() => ScenarioLoader.LoadFromJson(ScenarioLoader.Serialize(scenario)));
        Assert.Contains("tradingMapGeneration", error.Message, StringComparison.Ordinal);
        Assert.Contains("tradingMap", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Generation_request_is_rejected_in_versioned_or_nonzero_time_save()
    {
        Assert.Throws<ScenarioException>(() => ScenarioLoader.LoadFromJson(
            ScenarioLoader.Serialize(Scenario(generation: Rules(), saveFormatVersion: 1))));
        Assert.Throws<ScenarioException>(() => ScenarioLoader.LoadFromJson(
            ScenarioLoader.Serialize(Scenario(generation: Rules(), gameTimeMs: 1))));
    }

    [Fact]
    public void Result_requires_materialized_stations_seed_and_two_rng_streams()
    {
        Assert.Throws<ScenarioException>(() => ScenarioLoader.LoadFromJson(
            ScenarioLoader.Serialize(Scenario(map: MaterializedMap(), masterSeed: null))));
        Assert.Throws<ScenarioException>(() => ScenarioLoader.LoadFromJson(
            ScenarioLoader.Serialize(Scenario(map: MaterializedMap() with
            {
                RngStreams = [new("TradingMap.Topology", 1, 10000)]
            }))));
        Assert.Throws<ScenarioException>(() => ScenarioLoader.LoadFromJson(
            ScenarioLoader.Serialize(Scenario(map: MaterializedMap() with
            {
                Rules = Rules() with
                {
                    Stations = Rules().Stations.Skip(1).ToArray()
                }
            }))));
    }

    [Fact]
    public void Counter_rejects_non_multiple_of_ten_and_overflow_values()
    {
        var map = MaterializedMap() with
        {
            RngStreams =
            [
                new("TradingMap.Topology", 1, 10001),
                new("TradingMap.Geometry", 2, 10000),
            ]
        };

        Assert.Throws<ScenarioException>(() =>
            ScenarioLoader.LoadFromJson(ScenarioLoader.Serialize(Scenario(map: map))));

        var overflow = map with
        {
            RngStreams =
            [
                new("TradingMap.Topology", 1, ulong.MaxValue),
                new("TradingMap.Geometry", 2, 10000),
            ]
        };
        Assert.Throws<ScenarioException>(() => ScenarioLoader.LoadFromJson(ScenarioLoader.Serialize(Scenario(map: overflow))));
    }

    [Fact]
    public void Input_order_does_not_change_normalized_map_json()
    {
        var first = ScenarioLoader.LoadFromJson(ScenarioLoader.Serialize(Scenario(map: MaterializedMap())));
        var second = ScenarioLoader.LoadFromJson(ScenarioLoader.Serialize(Scenario(map: ShuffledMaterializedMap())));

        Assert.Equal(ScenarioLoader.Serialize(first), ScenarioLoader.Serialize(second));
    }

    [Fact]
    public void Map_schema_rejects_bad_versions_ids_profiles_numbers_and_links()
    {
        Assert.Throws<ScenarioException>(() => ScenarioLoader.LoadFromJson(
            ScenarioLoader.Serialize(Scenario(generation: Rules() with { SchemaVersion = 2 }))));
        Assert.Throws<ScenarioException>(() => ScenarioLoader.ValidateAndNormalize(
            Scenario(generation: Rules() with { ReferenceSpeedMps = double.NaN }), false));
        Assert.Throws<ScenarioException>(() => ScenarioLoader.LoadFromJson(
            ScenarioLoader.Serialize(Scenario(generation: Rules() with
            {
                Stations = Rules().Stations.Take(4).Append(Rules().Stations[0]).ToArray()
            }))));
        Assert.Throws<ScenarioException>(() => ScenarioLoader.LoadFromJson(
            ScenarioLoader.Serialize(Scenario(generation: Rules() with
            {
                Templates = [Rules().Templates[0] with
                {
                    Links = [new("ST-TRANSIT", "ST-TRANSIT", "risk.normal")]
                }]
            }))));
        Assert.Throws<ScenarioException>(() => ScenarioLoader.LoadFromJson(
            ScenarioLoader.Serialize(Scenario(map: MaterializedMap() with
            {
                Edges = [MaterializedMap().Edges[0] with { DistanceClass = "short" }, MaterializedMap().Edges[1]]
            }))));

        var rules = Rules();
        Assert.Throws<ScenarioException>(() => ScenarioLoader.LoadFromJson(
            ScenarioLoader.Serialize(Scenario(generation: rules with
            {
                Stations = rules.Stations.Select((station, index) => index == 0
                    ? station with { ObjectId = "st-mine" }
                    : station).ToArray()
            }))));
        Assert.Throws<ScenarioException>(() => ScenarioLoader.LoadFromJson(
            ScenarioLoader.Serialize(Scenario(generation: rules with
            {
                Templates = [rules.Templates[0] with
                {
                    Links = [rules.Templates[0].Links[0] with { ToStationObjectId = "ST-UNKNOWN" }]
                }]
            }))));
        Assert.Throws<ScenarioException>(() => ScenarioLoader.LoadFromJson(
            ScenarioLoader.Serialize(Scenario(generation: rules with
            {
                Templates = [rules.Templates[0] with
                {
                    Links = [rules.Templates[0].Links[0] with { RiskProfileId = "risk.unknown" }]
                }]
            }))));
        Assert.Throws<ScenarioException>(() => ScenarioLoader.LoadFromJson(
            ScenarioLoader.Serialize(Scenario(generation: rules with
            {
                Templates = [rules.Templates[0] with
                {
                    Links =
                    [
                        rules.Templates[0].Links[0],
                        rules.Templates[0].Links[0] with
                        {
                            FromStationObjectId = rules.Templates[0].Links[0].ToStationObjectId,
                            ToStationObjectId = rules.Templates[0].Links[0].FromStationObjectId
                        }
                    ]
                }]
            }))));
        Assert.Throws<ScenarioException>(() => ScenarioLoader.LoadFromJson(
            ScenarioLoader.Serialize(Scenario(generation: rules with
            {
                Stations = rules.Stations.Select((station, index) => index == 0 ? null! : station).ToArray()
            }))));
        Assert.Throws<ScenarioException>(() => ScenarioLoader.LoadFromJson(
            ScenarioLoader.Serialize(Scenario(generation: rules with { StartStationObjectId = "" }))));
        Assert.Throws<ScenarioException>(() => ScenarioLoader.LoadFromJson(
            ScenarioLoader.Serialize(Scenario(generation: rules with { RiskProfiles = null! }))));
    }

    [Fact]
    public void Edge_fuel_multiplier_must_match_declared_risk_profile()
    {
        var map = MaterializedMap() with
        {
            Edges =
            [
                MaterializedMap().Edges[0] with { FuelMultiplierPermille = 2500 },
                MaterializedMap().Edges[1]
            ]
        };

        var error = Assert.Throws<ScenarioException>(() => ScenarioLoader.LoadFromJson(
            ScenarioLoader.Serialize(Scenario(map: map))));
        Assert.Contains("must equal riskProfile", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Existing_station_id_case_must_match_exactly()
    {
        var scenario = Scenario(map: MaterializedMap());
        var mismatched = scenario with
        {
            GameState = scenario.GameState with
            {
                SpaceObjects = scenario.GameState.SpaceObjects
                    .Select(obj => obj.ObjectId == "ST-TRANSIT" ? obj with { ObjectId = "st-transit" } : obj)
                    .ToArray()
            }
        };

        var error = Assert.Throws<ScenarioException>(() => ScenarioLoader.LoadFromJson(
            ScenarioLoader.Serialize(mismatched)));
        Assert.Contains("including case", error.Message, StringComparison.Ordinal);
    }

    private const string BaseScenarioJson = """
    {
      "scenarioMetadata": { "scenarioId": "legacy", "name": "Legacy" },
      "gameState": {
        "gameTimeMs": 0, "currentSpeed": "Speed0", "playerShipObjectId": "SHIP",
        "spaceObjects": [
          { "objectId": "SHIP", "objectType": "PlayerShip", "persistenceType": "Permanent",
            "positionX": 0, "positionY": 0, "speedMps": 0, "directionDegrees": 0, "movementType": "Stationary" }
        ]
      }
    }
    """;

    private static ScenarioFile Scenario(
        TradingMapGenerationData? generation = null,
        TradingMapStateData? map = null,
        ulong? masterSeed = 123,
        int saveFormatVersion = 0,
        long gameTimeMs = 0)
    {
        var objects = new List<SpaceObjectData>
        {
            new("SHIP", "PlayerShip", "Permanent", "Ship", 0, 0, 0, 0, "Stationary", null, null, null),
            Station("ST-TRANSIT", "market.transit", "Large"),
        };
        if (map is not null)
            objects.AddRange(Rules().Stations.Where(s => s.ObjectId != "ST-TRANSIT")
                .Select(s => Station(s.ObjectId, s.MarketProfileId, s.StationSize)));

        return new(
            new("test", "Trading map"),
            new(gameTimeMs, "Speed0", "SHIP", null, objects, MasterSeed: masterSeed,
                TradingMapGeneration: generation, TradingMap: map),
            saveFormatVersion);
    }

    private static SpaceObjectData Station(string id, string profile, string size) =>
        new(id, "Station", "Permanent", id, 0, 0, 0, 0, "Stationary", null, null, null,
            MarketProfileId: profile, StationSize: size);

    private static TradingMapGenerationData Rules()
    {
        var stations = new[]
        {
            new TradingMapStationData("ST-SCI", "market.scientific-military", "Scientific", "Medium"),
            new TradingMapStationData("ST-TRANSIT", "market.transit", "Transit", "Large"),
            new TradingMapStationData("ST-MINE", "market.mining", "Mining", "Medium"),
            new TradingMapStationData("ST-IND", "market.industrial", "Industrial", "Medium"),
            new TradingMapStationData("ST-HYDROPONIC", "market.hydroponic", "Hydroponic", "Medium"),
        };
        var links = new[]
        {
            new TradingMapLinkData("ST-TRANSIT", "ST-MINE", "risk.normal"),
            new TradingMapLinkData("ST-TRANSIT", "ST-IND", "risk.normal"),
        };
        var offsets = stations.Select((station, index) =>
            new TradingMapOffsetData(station.ObjectId, index == 1 ? 0 : index * 10, index == 1 ? 0 : index * -10)).ToArray();
        return new(
            1, "ST-TRANSIT", 10, 100, 200, 300, 20, 5,
            stations,
            [new("ring", links, offsets)],
            [new("risk.normal", 1000)]);
    }

    private static TradingMapStateData MaterializedMap() => new(
        1, "ring", 0, Rules(),
        [
            new("ST-MINE", "ST-TRANSIT", 10, 100, "Short", 1000, "risk.normal"),
            new("ST-IND", "ST-TRANSIT", 12, 120, "Medium", 1000, "risk.normal"),
        ],
        [
            new("ST-MINE", "ST-TRANSIT", ["item.iron-ore", "item.water"]),
            new("ST-IND", "ST-TRANSIT", ["item.steel"]),
        ],
        [new("TradingMap.Geometry", 2, 10000), new("TradingMap.Topology", 1, 10000)]);

    private static TradingMapStateData ShuffledMaterializedMap()
    {
        var rules = Rules();
        return MaterializedMap() with
        {
            Rules = rules with
            {
                Stations = rules.Stations.Reverse().ToArray(),
                Templates = [rules.Templates[0] with
                {
                    Links = rules.Templates[0].Links.Reverse().Select(l => l with
                    {
                        FromStationObjectId = l.ToStationObjectId,
                        ToStationObjectId = l.FromStationObjectId
                    }).ToArray(),
                    Offsets = rules.Templates[0].Offsets.Reverse().ToArray()
                }],
                RiskProfiles = rules.RiskProfiles.Reverse().ToArray()
            },
            Edges = MaterializedMap().Edges.Reverse().Select(e => e with
            {
                FromStationObjectId = e.ToStationObjectId,
                ToStationObjectId = e.FromStationObjectId
            }).ToArray(),
            CargoFlows = MaterializedMap().CargoFlows.Reverse().ToArray()
        };
    }
}
