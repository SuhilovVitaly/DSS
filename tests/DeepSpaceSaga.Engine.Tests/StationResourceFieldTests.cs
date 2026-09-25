using System.Collections.Immutable;
using System.Text.Json;
using System.Text.Json.Nodes;
using DeepSpaceSaga.Engine.Content;
using DeepSpaceSaga.Engine.Rng;
using DeepSpaceSaga.Engine.Scenario;

namespace DeepSpaceSaga.Engine.Tests;

public sealed class StationResourceFieldTests
{
    [Fact]
    public void Profiles_generate_exact_counts_and_resource_fractions()
    {
        var result = Generate();
        var fields = result.StationResourceFields!;
        Assert.Equal(57, fields.Asteroids.Count);
        Assert.Equal(1057, fields.NextObjectNumber);
        Assert.Empty(fields.Surveys);
        Assert.All(fields.Asteroids, a =>
        {
            Assert.False(a.CompositionKnown);
            Assert.Equal(1000, a.Resources.Sum(r => r.Permille));
            var obj = result.SpaceObjects.Single(o => o.ObjectId == a.ObjectId);
            Assert.Equal("Asteroid", obj.ObjectType);
            Assert.Equal("Permanent", obj.PersistenceType);
            Assert.Equal("Stationary", obj.MovementType);
            Assert.Equal(0, obj.SpeedMps);
            Assert.True(obj.IsKnown);
            Assert.Null(obj.Name);
            Assert.Empty(obj.Modules!);
        });
        foreach (var station in result.TradingMap!.Rules.Stations)
            foreach (var f in Config().Roles.Single(r => r.MarketProfileId == station.MarketProfileId).Fields)
                Assert.Equal(f.AsteroidCount, fields.Asteroids.Count(a => a.StationObjectId == station.ObjectId && a.FieldKindId == f.FieldKindId));
        Assert.Equal(6, fields.Asteroids.Count(a => a.FieldKindId == "carbon"));
        Assert.Equal(2, fields.Asteroids.Where(a => a.FieldKindId == "carbon").Select(a => a.StationObjectId).Distinct().Count());
        Assert.All(fields.Asteroids.Where(a => a.FieldKindId == "mixed"), a =>
            Assert.Equal(100, a.Resources.Single(r => r.ItemTypeId == "item.silicon").Permille));
    }

    [Fact]
    public void Same_seed_and_reordered_inputs_generate_identical_fields()
    {
        var map = Map();
        var config = Config();
        var reordered = config with
        {
            Roles = config.Roles.Reverse().Select(r => r with { Fields = r.Fields.Reverse().ToArray() }).ToArray(),
            FieldKinds = config.FieldKinds.Reverse().Select(k => k with
            {
                Variants = k.Variants.Reverse().Select(v => v with { Resources = v.Resources.Reverse().ToArray() }).ToArray()
            }).ToArray(),
            MassBands = config.MassBands.Reverse().ToArray()
        };
        var reversedMap = map with
        {
            SpaceObjects = map.SpaceObjects.Reverse().ToArray(),
            TradingMap = map.TradingMap! with
            {
                Rules = map.TradingMap.Rules with { Stations = map.TradingMap.Rules.Stations.Reverse().ToArray() },
                Edges = map.TradingMap.Edges.Reverse().ToArray()
            }
        };
        var first = StationResourceFields.Generate(map, 123, config, Registry());
        var second = StationResourceFields.Generate(reversedMap, 123, reordered, Registry());
        Assert.Equal(Json(first.StationResourceFields), Json(second.StationResourceFields));
        Assert.Equal(Json(FieldObjects(first)), Json(FieldObjects(second)));
    }

    [Fact]
    public void Other_rng_streams_do_not_change_fields()
    {
        var first = Generate();
        foreach (var name in new[] { "TradingMap.Geometry", "StationCredits:SPC-0002", "ResourceSurvey:SPC-0001:scanner" })
        {
            var random = new ResourceFieldRandom(new(name, RngStreamSeedDerivation.DeriveStreamSeed(123, name), 10000));
            for (int i = 0; i < 99; i++) random.NextDouble();
        }
        Assert.Equal(Json(first), Json(Generate()));
    }

    [Fact]
    public void Mass_draws_follow_configured_bands_and_saved_counters()
    {
        var result = Generate();
        var rules = result.StationResourceFields!.Rules;
        foreach (var station in result.TradingMap!.Rules.Stations)
        {
            ulong stationSeed = RngStreamSeedDerivation.DeriveStreamSeed(123, "ResourceFields:" + station.ObjectId);
            ulong seed = RngStreamSeedDerivation.DeriveStreamSeed(stationSeed, "Mass");
            var expected = RngStreamNames.CreateDeterministicRandom(seed);
            for (int i = 0; i < 1000; i++) expected.NextDouble();
            var asteroids = result.StationResourceFields.Asteroids.Where(a => a.StationObjectId == station.ObjectId).ToArray();
            foreach (var a in asteroids)
            {
                double weighted = expected.NextDouble() * 100;
                // Canonical IDs sort Large(8), Medium(30), Small(60), VeryLarge(2), Zero(0).
                string bandId = weighted < 8 ? "Large" : weighted < 38 ? "Medium" : weighted < 98 ? "Small" : "VeryLarge";
                var band = rules.MassBands.Single(b => b.BandId == bandId);
                long mass = band.MinKg + (long)Math.Floor(expected.NextDouble() * (band.MaxKg - band.MinKg + 1));
                Assert.Equal(mass, result.SpaceObjects.Single(o => o.ObjectId == a.ObjectId).MassKg);
            }
            var saved = result.StationResourceFields.RngStreams.Single(s => s.Name == $"ResourceFields:{station.ObjectId}:Mass");
            Assert.Equal(seed, saved.Seed);
            Assert.Equal(10000UL + (ulong)asteroids.Length * 20, saved.Counter);
            var resumed = new ResourceFieldRandom(saved);
            Assert.Equal(expected.NextDouble(), resumed.NextDouble());
            Assert.Equal(saved.Counter + 10, resumed.Capture().Counter);
            Assert.Equal(10000UL + (ulong)asteroids.Length * 10,
                result.StationResourceFields.RngStreams.Single(s => s.Name == $"ResourceFields:{station.ObjectId}:Composition").Counter);
        }
    }

    [Fact]
    public void All_objects_respect_annulus_station_clearance_spacing_and_corridors()
    {
        foreach (ulong seed in Enumerable.Range(0, 256).Select(i => (ulong)i).Append(ulong.MaxValue))
        {
            var map = Map(seed);
            var result = StationResourceFields.Generate(map, seed, Config(), Registry());
            var fields = result.StationResourceFields!;
            Assert.Equal(57, fields.Asteroids.Count);
            foreach (var a in fields.Asteroids)
            {
                var o = result.SpaceObjects.Single(o => o.ObjectId == a.ObjectId);
                var owner = result.SpaceObjects.Single(o => o.ObjectId == a.StationObjectId);
                Assert.InRange(Distance(o, owner), 2.5, 4.5);
                foreach (var station in map.SpaceObjects.Where(o => o.ObjectType == "Station"))
                    Assert.True(Distance(o, station) >= 2);
                foreach (var other in result.SpaceObjects.Where(p => p.ObjectType == "Asteroid" && p.ObjectId != o.ObjectId))
                    Assert.True(Distance(o, other) >= 0.1);
                foreach (var edge in map.TradingMap!.Edges)
                {
                    var from = map.SpaceObjects.Single(o => o.ObjectId == edge.FromStationObjectId);
                    var to = map.SpaceObjects.Single(o => o.ObjectId == edge.ToStationObjectId);
                    // Independent distance oracle: endpoint tests followed by triangle area/base.
                    double dx = to.PositionX - from.PositionX, dy = to.PositionY - from.PositionY;
                    double ax = o.PositionX - from.PositionX, ay = o.PositionY - from.PositionY;
                    double projection = ax * dx + ay * dy;
                    double length2 = dx * dx + dy * dy;
                    double distance = projection <= 0 ? Distance(o, from) : projection >= length2 ? Distance(o, to)
                        : Math.Abs(ax * dy - ay * dx) / Math.Sqrt(length2) / 10;
                    Assert.True(distance >= 0.25, $"seed={seed}, asteroid={o.ObjectId}");
                }
            }
            Assert.Equal(Json(fields), Json(StationResourceFields.ValidateSaved(result, Registry()).StationResourceFields));
        }
    }

    [Fact]
    public void Impossible_placement_and_id_collision_leave_loaded_world_unchanged()
    {
        using var engine = Engine();
        var before = Json(Save(engine));
        engine.ConfigureStationResourceFields(Config() with { CorridorHalfWidthKm = 100000, MaxPlacementAttempts = 1 });
        var error = Assert.Throws<ScenarioException>(() => engine.LoadScenario(Scenario()));
        Assert.Contains("station SPC-0002, kind mixed, ordinal 0", error.Message);
        Assert.Equal(before, Json(Save(engine)));
        engine.ConfigureStationResourceFields(Config());
        var map = Map();
        var collision = map.SpaceObjects.First(o => o.ObjectType == "Asteroid") with { ObjectId = "spc-1000" };
        Assert.Throws<ScenarioException>(() => engine.LoadScenario(new(new("bad", "bad"), map with
        { SpaceObjects = map.SpaceObjects.Append(collision).ToArray() })));
        Assert.Equal(before, Json(Save(engine)));
    }

    [Fact]
    public void Save_load_preserves_all_fields_without_rng_or_regeneration()
    {
        using var engine = Engine();
        var save = Save(engine);
        var json = Json(save);
        var restored = ScenarioLoader.LoadFromJson(json, allowNonZeroGameTime: true);
        using var other = new SimulationEngine(Registry());
        other.ConfigureStationResourceFields(Config() with { FirstObjectNumber = 5000, CorridorHalfWidthKm = 100000 });
        other.LoadScenario(restored, isSave: true);
        Assert.Equal(json, Json(Save(other)));
        Assert.Equal(Json(engine.CaptureSnapshotForTests()), Json(other.CaptureSnapshotForTests()));
        var known = restored.GameState.StationResourceFields! with
        { Asteroids = restored.GameState.StationResourceFields!.Asteroids.Select((a, i) => a with { CompositionKnown = i == 0 }).ToArray() };
        other.LoadScenario(restored with { GameState = restored.GameState with { StationResourceFields = known } }, isSave: true);
        var survey = other.CaptureSnapshotForTests().Objects.Single(o => o.ObjectId == known.Asteroids[0].ObjectId).Survey!;
        Assert.True(survey.CompositionKnown);
        Assert.Equal(1000, survey.Resources.Sum(r => r.Permille));
    }

    [Theory]
    [InlineData(4, 2)]
    [InlineData(2, 2.5)]
    [InlineData(2.5, 2.5)]
    public void New_game_save_load_respects_both_map_and_field_clearances(double mapClearanceKm, double fieldClearanceKm)
    {
        var scenario = Scenario(123);
        scenario = scenario with
        {
            GameState = scenario.GameState with
            {
                TradingMapGeneration = scenario.GameState.TradingMapGeneration! with { ClearanceKm = mapClearanceKm }
            }
        };
        using var engine = new SimulationEngine(Registry());
        engine.ConfigureStationResourceFields(Config() with { StationClearanceKm = fieldClearanceKm });
        engine.LoadScenario(scenario);
        var saved = Save(engine);
        Assert.Equal(57, saved.GameState.StationResourceFields!.Asteroids.Count);

        using var restored = new SimulationEngine(Registry());
        restored.LoadScenario(ScenarioLoader.LoadFromJson(Json(saved), allowNonZeroGameTime: true), isSave: true);
        Assert.Equal(Json(saved), Json(Save(restored)));
        Assert.Equal(Json(engine.CaptureSnapshotForTests()), Json(restored.CaptureSnapshotForTests()));
        foreach (var asteroid in FieldObjects(saved.GameState))
            foreach (var station in saved.GameState.SpaceObjects.Where(o => o.ObjectType == "Station"))
                Assert.True(Distance(asteroid, station) >= Math.Max(mapClearanceKm, fieldClearanceKm));
    }

    [Fact]
    public void Map_clearance_outside_field_annulus_rejects_before_replacing_world()
    {
        using var engine = Engine();
        var before = Json(Save(engine));
        var scenario = Scenario(123);
        scenario = scenario with
        {
            GameState = scenario.GameState with
            {
                TradingMapGeneration = scenario.GameState.TradingMapGeneration! with { ClearanceKm = 4.75 }
            }
        };

        var error = Assert.Throws<ScenarioException>(() => engine.LoadScenario(scenario));
        Assert.Contains("Placement exhausted for station SPC-0002, kind mixed, ordinal 0", error.Message);
        Assert.Equal(before, Json(Save(engine)));
    }

    [Fact]
    public void Legacy_asteroid_motion_does_not_invalidate_saved_fields()
    {
        using var engine = Engine();
        var saved = Save(engine);
        var field = FieldObjects(saved.GameState).First();
        var moved = saved.GameState.SpaceObjects.Select(o => o.ObjectId == "SPC-0003"
            ? o with { PositionX = field.PositionX, PositionY = field.PositionY } : o).ToArray();
        engine.LoadScenario(saved with { GameState = saved.GameState with { SpaceObjects = moved } }, isSave: true);
        Assert.Equal(Json(saved.GameState.StationResourceFields), Json(Save(engine).GameState.StationResourceFields));
    }

    [Fact]
    public void Bad_manifest_seed_fraction_or_reference_rejects_atomically()
    {
        using var engine = Engine();
        var saved = Save(engine);
        var fields = saved.GameState.StationResourceFields!;
        var first = fields.Asteroids[0];
        StationResourceFieldsState Asteroid(ResourceFieldAsteroidData a) => fields with { Asteroids = fields.Asteroids.Skip(1).Prepend(a).ToArray() };
        StationResourceFieldsState Rng(ResourceFieldRngData s) => fields with { RngStreams = fields.RngStreams.Skip(1).Prepend(s).ToArray() };
        var invalid = new[]
        {
            fields with { SchemaVersion = 2 }, fields with { Rules = null! }, fields with { Asteroids = null! },
            fields with { Asteroids = fields.Asteroids.Skip(1).ToArray() },
            fields with { Asteroids = fields.Asteroids.Append(first).ToArray() },
            fields with { NextObjectNumber = 1056 }, fields with { RngStreams = [] }, fields with { Surveys = null! },
            Asteroid(first with { ObjectId = "SPC-9999" }), Asteroid(first with { StationObjectId = "missing" }),
            Asteroid(first with { VariantId = "missing" }), Asteroid(first with { FieldKindId = "carbon" }),
            Asteroid(first with { Resources = [new("item.silicon", 1000)] }), Asteroid(first with { Resources = null! }),
            Rng(fields.RngStreams[0] with { Seed = fields.RngStreams[0].Seed + 1 }),
            Rng(fields.RngStreams[0] with { Counter = 10000 }), Rng(fields.RngStreams[0] with { Counter = ulong.MaxValue }),
            Rng(fields.RngStreams[0] with { Name = "ResourceSurvey:unknown:module" }),
            fields with { Surveys = [new("scan", "SPC-0001", "missing", first.ObjectId, 0, 60000, 0)] }
        };
        foreach (var bad in invalid)
        {
            Assert.Throws<ScenarioException>(() => engine.LoadScenario(saved with { GameState = saved.GameState with { StationResourceFields = bad } }, true));
            Assert.Equal(Json(saved), Json(Save(engine)));
        }
        Assert.Throws<ScenarioException>(() => engine.LoadScenario(saved with { GameState = saved.GameState with { MasterSeed = null } }, true));
        var obj = saved.GameState.SpaceObjects.Single(o => o.ObjectId == first.ObjectId);
        foreach (var bad in new[]
        {
            obj with { MassKg = 1 }, obj with { CompositionType = obj.CompositionType == "Ice" ? "Iron" : "Ice" },
            obj with { PositionX = 0 }, obj with { Name = "hidden composition" }, obj with { IsKnown = false },
            obj with { SpeedMps = 1 }, obj with { PersistenceType = "Temporary" }
        })
        {
            Assert.Throws<ScenarioException>(() => engine.LoadScenario(saved with
            {
                GameState = saved.GameState with
                { SpaceObjects = saved.GameState.SpaceObjects.Select(o => o.ObjectId == bad.ObjectId ? bad : o).ToArray() }
            }, true));
            Assert.Equal(Json(saved), Json(Save(engine)));
        }
    }

    [Fact]
    public void No_map_no_config_and_legacy_save_do_not_generate_fields()
    {
        using var engine = new SimulationEngine(Registry());
        engine.LoadScenario(Scenario());
        Assert.Null(Save(engine).GameState.StationResourceFields);
        var saveWithoutFields = Save(engine);
        engine.ConfigureStationResourceFields(Config());
        engine.LoadScenario(saveWithoutFields, true);
        Assert.Null(Save(engine).GameState.StationResourceFields);
        engine.LoadScenario(saveWithoutFields); // SaveFormatVersion alone prevents retrofit.
        Assert.Null(Save(engine).GameState.StationResourceFields);
        var legacy = Scenario();
        engine.LoadScenario(legacy with { GameState = legacy.GameState with { TradingMapGeneration = null } });
        Assert.Null(Save(engine).GameState.StationResourceFields);
        Assert.Equal(4, Save(engine).GameState.SpaceObjects.Count);
    }

    [Fact]
    public void Fields_do_not_mutate_station_inventory_budget_or_player_cargo()
    {
        var before = Map();
        var generated = StationResourceFields.Generate(before, 123, Config(), Registry());
        Assert.Equal(Json(before), Json(generated with { SpaceObjects = generated.SpaceObjects.Take(before.SpaceObjects.Count).ToArray(), StationResourceFields = null }));
        using var plain = new SimulationEngine(Registry());
        plain.LoadScenario(Scenario());
        using var fields = Engine();
        var after = Save(fields);
        Assert.Equal(Json(Save(plain)), Json(after with
        {
            GameState = after.GameState with
            { SpaceObjects = after.GameState.SpaceObjects.Where(o => !after.GameState.StationResourceFields!.Asteroids.Any(a => a.ObjectId == o.ObjectId)).ToArray(), StationResourceFields = null }
        }));
    }

    [Fact]
    public void Unrevealed_field_snapshot_has_neutral_image_and_no_resources()
    {
        using var engine = Engine();
        var save = Save(engine);
        var ids = save.GameState.StationResourceFields!.Asteroids.Select(a => a.ObjectId).ToHashSet();
        var snapshot = engine.CaptureSnapshotForTests();
        foreach (var row in snapshot.Objects.Where(o => ids.Contains(o.ObjectId)))
        {
            Assert.Null(row.DisplayName);
            Assert.NotNull(row.Image);
            Assert.DoesNotContain("ice", row.Image, StringComparison.OrdinalIgnoreCase);
            Assert.False(row.Survey!.CompositionKnown);
            Assert.False(row.Survey.CanStructuralScan);
            Assert.Null(row.Survey.CompositionType);
            Assert.Empty(row.Survey.Resources);
            Assert.Equal(save.GameState.SpaceObjects.Single(o => o.ObjectId == row.ObjectId).MassKg, row.Survey.MassKg);
        }
        Assert.All(snapshot.Objects.Where(o => !ids.Contains(o.ObjectId)), row => Assert.Null(row.Survey));
        Assert.Contains(save.GameState.SpaceObjects, o => ids.Contains(o.ObjectId) && o.CompositionType == "Ice" && o.Image!.Contains("Ice", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Config_rejects_nulls_duplicates_bad_references_geometry_weights_and_overflow()
    {
        var c = Config();
        var invalid = new[]
        {
            c with { SchemaVersion = 0 }, c with { FirstObjectNumber = int.MaxValue }, c with { MaxPlacementAttempts = 0 },
            c with { InnerRadiusKm = double.NaN }, c with { OuterRadiusKm = double.PositiveInfinity },
            c with { InnerRadiusKm = 1 }, c with { AsteroidSpacingKm = 0 }, c with { CorridorHalfWidthKm = -1 },
            c with { Roles = c.Roles.Skip(1).ToArray() }, c with { Roles = c.Roles.Append(c.Roles[0]).ToArray() },
            c with { FieldKinds = null! }, c with { StructuralScan = null! }, c with { StructuralScan = new(120, 0, 85) },
            c with { StructuralScan = new(120, 60000, 101) },
            c with { MassBands = [new("a", 1000000, 2000000, 1), new("b", 2000000, 3000000, 1)] },
            c with { MassBands = [new("a", 1000000, 2000000, 0)] },
            c with { MassBands = [new("a", 1000000, 2000000, -1)] },
            c with { Roles = c.Roles.Select(r => r with { Fields = [new("missing", 1)] }).ToArray() },
            c with { Roles = c.Roles.Select(r => r with { Fields = [new("ice", int.MaxValue)] }).ToArray() }
        };
        foreach (var bad in invalid) Assert.Throws<ContentException>(() => StationResourceFields.ValidateConfig(bad, Registry()));
        foreach (var variant in new[]
        {
            new ResourceVariantData("v", "Carbon", 1, [new("item.ice", 1000)]),
            new("v", "Ice", 1, [new("item.ice", 999)]), new("v", "Ice", 1, [new("item.food", 1000)]),
            new("v", "Ice", 1, [new("unknown", 1000)]), new("v", "Ice", 1, [new("item.ice", 500), new("item.ice", 500)]),
            new("v", "Ice", 0, [new("item.ice", 1000)]), new("v", "Ice", 1, null!)
        })
            Assert.Throws<ContentException>(() => StationResourceFields.ValidateConfig(c with
            { FieldKinds = c.FieldKinds.Select(k => k with { Variants = [variant] }).ToArray() }, Registry()));
    }

    [Fact]
    public void Rng_rounds_counter_up_restores_and_rejects_overflow()
    {
        var random = new ResourceFieldRandom(new("test", 123, 10001));
        Assert.Equal(10010UL, random.Capture().Counter);
        var reference = RngStreamNames.CreateDeterministicRandom(123);
        for (int i = 0; i < 1001; i++) reference.NextDouble();
        Assert.Equal(reference.NextDouble(), random.NextDouble());
        Assert.Equal(random.NextDouble(), new ResourceFieldRandom(new("test", 123, 10020)).NextDouble());
        Assert.Throws<ScenarioException>(() => new ResourceFieldRandom(new("test", 123, ulong.MaxValue)));
        Assert.Throws<ScenarioException>(() => new ResourceFieldRandom(new("test", 123, ulong.MaxValue - 5)).NextDouble());
    }

    [Fact]
    public void Named_station_stream_has_fixed_seed_and_draw_vectors()
    {
        ulong stationSeed = RngStreamSeedDerivation.DeriveStreamSeed(123, "ResourceFields:SPC-0002");
        Assert.Equal(15747011834317213464UL, stationSeed);
        ulong seed = RngStreamSeedDerivation.DeriveStreamSeed(stationSeed, "Mass");
        Assert.Equal(12645317997094230505UL, seed);
        var random = new ResourceFieldRandom(new("ResourceFields:SPC-0002:Mass", seed, 10000));
        Assert.Equal(0.42563693431468536, random.NextDouble());
        Assert.Equal(0.7668210550988191, random.NextDouble());
        var first = FieldObjects(Generate()).First();
        Assert.Equal("SPC-1000", first.ObjectId);
        Assert.Equal(7901389, first.MassKg);
    }

    [Theory]
    [InlineData(1000000)]
    [InlineData(1000000000)]
    public void Inclusive_mass_endpoints_and_immutable_configuration(long mass)
    {
        var roles = Config().Roles.ToArray();
        var config = Config() with { Roles = roles, MassBands = [new("fixed", mass, mass, 1)] };
        using var engine = new SimulationEngine(Registry());
        engine.ConfigureStationResourceFields(config);
        roles[0] = roles[0] with { Fields = [new("mixed", 100000)] };
        engine.LoadScenario(Scenario());
        var saved = Save(engine);
        Assert.Equal(57, saved.GameState.StationResourceFields!.Asteroids.Count);
        Assert.All(FieldObjects(saved.GameState), o => Assert.Equal(mass, o.MassKg));
        engine.LoadScenario(saved, true);
        Assert.Equal(Json(saved), Json(Save(engine)));
    }

    [Fact]
    public void Default_docked_and_undocked_variants_have_identical_fields()
    {
        string expected = Json(Generate().StationResourceFields);
        foreach (var variant in new[] { "Default", "Docked", "Undocked" })
        {
            var scenario = Scenario();
            scenario = scenario with
            {
                GameState = scenario.GameState with
                {
                    SpaceObjects = scenario.GameState.SpaceObjects.Select(o => o.ObjectType == "PlayerShip" ? o with
                    {
                        SpeedMps = variant == "Default" ? 700 : 0,
                        MovementType = variant == "Default" ? "Linear" : "Stationary",
                        IsDocked = variant == "Docked",
                        DockedStationObjectId = variant == "Docked" ? "SPC-0002" : null
                    } : o).ToArray()
                }
            };
            using var engine = new SimulationEngine(Registry());
            engine.ConfigureStationResourceFields(Config());
            engine.LoadScenario(scenario);
            Assert.Equal(expected, Json(Save(engine).GameState.StationResourceFields));
        }
    }

    [Fact]
    public void Saved_survey_jobs_and_independent_streams_roundtrip_and_validate_shape()
    {
        using var engine = Engine();
        var saved = Save(engine);
        var fields = saved.GameState.StationResourceFields!;
        var job = new ResourceSurveyJobData("scan-1", "SPC-0001", "MOD-TEST", fields.Asteroids[0].ObjectId, 0, 60000, 0);
        const string streamName = "ResourceSurvey:SPC-0001:MOD-TEST";
        var stream = new ResourceFieldRngData(streamName, RngStreamSeedDerivation.DeriveStreamSeed(123, streamName), 10011);
        var withJob = saved with
        {
            GameState = saved.GameState with
            {
                StationResourceFields = fields with
                { Surveys = [job], RngStreams = fields.RngStreams.Append(stream).ToArray() }
            }
        };
        engine.LoadScenario(ScenarioLoader.LoadFromJson(Json(withJob), true), true);
        var normalized = Save(engine);
        Assert.Equal(10020UL, normalized.GameState.StationResourceFields!.RngStreams.Single(s => s.Name == streamName).Counter);
        Assert.Equal(job, Assert.Single(normalized.GameState.StationResourceFields.Surveys));
        engine.LoadScenario(normalized, true);
        Assert.Equal(Json(normalized), Json(Save(engine)));
        foreach (var bad in new[]
        {
            job with { CommandId = "" }, job with { ObjectId = "SPC-0002" }, job with { ModuleId = "missing" },
            job with { TargetObjectId = "SPC-0003" }, job with { StartedGameTimeMs = -1 }, job with { DueGameTimeMs = 0 },
            job with { LastValidatedSimulationTimeMs = -1 }
        })
            Assert.Throws<ScenarioException>(() => engine.LoadScenario(withJob with
            {
                GameState = withJob.GameState with
                { StationResourceFields = withJob.GameState.StationResourceFields! with { Surveys = [bad] } }
            }, true));
        Assert.Throws<ScenarioException>(() => engine.LoadScenario(withJob with
        {
            GameState = withJob.GameState with
            { StationResourceFields = withJob.GameState.StationResourceFields! with { Surveys = [job, job] } }
        }, true));
        Assert.Throws<ScenarioException>(() => engine.LoadScenario(withJob with
        {
            GameState = withJob.GameState with
            {
                StationResourceFields = withJob.GameState.StationResourceFields! with
                { Asteroids = fields.Asteroids.Select(a => a with { CompositionKnown = true }).ToArray() }
            }
        }, true));
        Assert.Equal(Json(normalized), Json(Save(engine)));
    }

    [Fact]
    public void All_three_loader_paths_use_optional_strict_relative_resource_config()
    {
        using var content = new ContentFixture();
        using var fromSettings = EngineContentLoader.CreateEngineFromSettingsFile(content.SettingsPath);
        using var fromScenario = EngineContentLoader.CreateEngineFromScenarioFile(content.SettingsPath, content.ScenarioPath);
        Assert.Equal(57, Save(fromSettings).GameState.StationResourceFields!.Asteroids.Count);
        Assert.Equal(Json(Save(fromSettings)), Json(Save(fromScenario)));
        content.Write("save.json", Save(fromSettings));
        content.Write("fields.json", Config() with { FirstObjectNumber = 5000, CorridorHalfWidthKm = 100000 });
        using var fromSave = EngineContentLoader.CreateEngineFromSaveFile(content.SettingsPath, content.PathFor("save.json"));
        Assert.Equal(Json(Save(fromSettings)), Json(Save(fromSave)));
        foreach (var invalid in new JsonNode?[] { null, JsonValue.Create(""), JsonValue.Create(" "), JsonValue.Create("missing.json"), JsonValue.Create(1) })
        {
            content.Settings["typeData"]!["stationResourceFields"] = invalid;
            content.Write("settings.json", content.Settings);
            foreach (var load in content.Loaders()) Assert.Throws<ContentException>(() => { using var engine = load(); });
        }
        content.Settings["typeData"]!["stationResourceFields"] = "fields.json";
        content.Write("settings.json", content.Settings);
        var unknown = JsonSerializer.SerializeToNode(Config())!;
        unknown["unknownField"] = true;
        foreach (object invalid in new object[] { unknown, Config() with { Roles = [] }, Config() with { FieldKinds = null! } })
        {
            content.Write("fields.json", invalid);
            foreach (var load in content.Loaders()) Assert.Throws<ContentException>(() => { using var engine = load(); });
        }
        content.Settings["typeData"]!.AsObject().Remove("stationResourceFields");
        content.Write("settings.json", content.Settings);
        using var disabled = EngineContentLoader.CreateEngineFromSettingsFile(content.SettingsPath);
        Assert.Null(Save(disabled).GameState.StationResourceFields);
        using var savedEnabled = EngineContentLoader.CreateEngineFromSaveFile(content.SettingsPath, content.PathFor("save.json"));
        Assert.Equal(Json(Save(fromSettings)), Json(Save(savedEnabled)));
    }

    private sealed class ContentFixture : IDisposable
    {
        private readonly string _directory = Path.Combine(Path.GetTempPath(), "dss-resource-fields-" + Guid.NewGuid());
        internal string SettingsPath => PathFor("settings.json");
        internal string ScenarioPath => PathFor("scenario.json");
        internal JsonNode Settings { get; }
        internal string PathFor(string file) => Path.Combine(_directory, file);
        internal void Write(string file, object value) => File.WriteAllText(PathFor(file), Json(value));
        internal IEnumerable<Func<SimulationEngine>> Loaders() =>
        [() => EngineContentLoader.CreateEngineFromSettingsFile(SettingsPath),
            () => EngineContentLoader.CreateEngineFromScenarioFile(SettingsPath, ScenarioPath),
            () => EngineContentLoader.CreateEngineFromSaveFile(SettingsPath, PathFor("save.json"))];

        internal ContentFixture()
        {
            Directory.CreateDirectory(_directory);
            Write("scenario.json", Scenario());
            Write("fields.json", Config());
            Write("categories.json", new { moduleTypes = new[] { new { typeId = "module.test", displayName = "Test", slotSize = 1, commandTypeIds = Array.Empty<string>() } } });
            Write("modules.json", new { moduleImplementations = new[] { new { typeId = "module.test", type = "module.test", displayName = "Test", massKg = 10, structurePointsMax = 100, powerConsumptionW = 0, cargoCapacityKg = 100 } } });
            Write("commands.json", new { commandDefinitions = Array.Empty<object>() });
            var registry = Registry();
            Write("items.json", new
            {
                itemTypes = Enumerable.Range(0, registry.ItemTypes.Count).Select(registry.ItemTypes.GetDefinition).Select(i => new
                {
                    typeId = i.TypeId,
                    displayName = i.DisplayName,
                    unitMassKg = i.UnitMassKg,
                    basePriceCredits = i.BasePriceCredits,
                    tradeCategory = i.Category.ToString(),
                    tradeUnit = i.TradeUnit.ToString(),
                    storageKind = i.StorageKind.ToString()
                })
            });
            Write("profiles.json", new
            {
                schemaVersion = 1,
                sizeFactors = new { Outpost = 500, Medium = 1000, Large = 1500, Huge = 2000 },
                profiles = Profiles().Select(p => new
                {
                    typeId = p.TypeId,
                    displayName = p.DisplayName,
                    supplyItemTypeIds = p.SupplyItemTypeIds,
                    demandItemTypeIds = p.DemandItemTypeIds,
                    initialCredits = p.InitialCredits,
                    refuelStockKg = p.RefuelStockKg,
                    initialInventory = p.InitialInventory.Select(i => new { itemTypeId = i.ItemTypeId, quantity = i.Quantity })
                })
            });
            Settings = JsonSerializer.SerializeToNode(new
            {
                typeData = new
                {
                    moduleTypes = "categories.json",
                    moduleImplementations = "modules.json",
                    itemTypes = "items.json",
                    commandDefinitions = "commands.json",
                    stationMarketProfiles = "profiles.json",
                    stationResourceFields = "fields.json"
                },
                defaultScenario = "scenario.json"
            })!;
            Write("settings.json", Settings);
        }

        public void Dispose() => Directory.Delete(_directory, recursive: true);
    }

    private static SimulationEngine Engine()
    {
        var engine = new SimulationEngine(Registry());
        engine.ConfigureStationResourceFields(Config());
        engine.LoadScenario(Scenario());
        return engine;
    }

    private static ScenarioFile Save(SimulationEngine engine) => engine.CaptureSaveStateForTests(0, DeepSpaceSaga.Contracts.SimulationSpeed.Speed0);
    private static string Json<T>(T value) => JsonSerializer.Serialize(value);
    private static GameStateData Generate() => StationResourceFields.Generate(Map(), 123, Config(), Registry());
    private static IEnumerable<SpaceObjectData> FieldObjects(GameStateData state) =>
        state.StationResourceFields!.Asteroids.Select(a => state.SpaceObjects.Single(o => o.ObjectId == a.ObjectId));
    private static double Distance(SpaceObjectData a, SpaceObjectData b) =>
        Math.Sqrt(Math.Pow(a.PositionX - b.PositionX, 2) + Math.Pow(a.PositionY - b.PositionY, 2)) / 10;

    private static GameStateData Map(ulong seed = 123)
    {
        using var engine = new SimulationEngine(Registry());
        engine.LoadScenario(Scenario(seed));
        return Save(engine).GameState;
    }

    private static ScenarioFile Scenario(ulong seed = 123) => new(new("fields", "Fields"), new(
        0, "Speed0", "SPC-0001", null,
        [
            new("SPC-0001", "PlayerShip", "Permanent", "Player", 10000, 10000, 700, 0, "Linear", null, null,
                [new("MOD-TEST", "module.test", [new(1, 1)], 100, "On", "Ready", null, [new("item.ore", 3)])],
                IsKnown: true, HullLayout: new(3, 3, [new(1, 1)])),
            new("SPC-0002", "Station", "Permanent", "Transit", 10000, 10000, 0, 0, "Stationary", null, null, null,
                IsKnown: true, MarketProfileId: "market.transit", StationSize: "Large"),
            new("SPC-0003", "Asteroid", "Temporary", null, 10400, 10000, 600, 256, "Linear", 1000000, "Silicate", null, IsKnown: true),
            new("SPC-0004", "Asteroid", "Temporary", null, 10000, 10450, 1200, 22, "Linear", 1000000, "Ice", null)
        ], MasterSeed: seed, PlayerTokens: 2000, TradingMapGeneration: Rules()));

    private static StationResourceFieldConfig Config() => new(1, 1000, 64, 2.5, 4.5, 2, 0.1, 0.25,
        [
            new("market.transit", [new("mixed", 6)]), new("market.mining", [new("metal", 12), new("ice", 6), new("carbon", 3)]),
            new("market.industrial", [new("mixed", 9), new("carbon", 3)]), new("market.hydroponic", [new("ice", 12)]),
            new("market.scientific-military", [new("mixed", 6)])
        ],
        [
            new("metal", [new("metal-rich", "Iron", 3, [new("item.ore", 900), new("item.magnesium-ore", 100)]),
                new("metal-poor", "Silicate", 1, [new("item.ore", 600), new("item.magnesium-ore", 400)]),
                new("zero", "Ice", 0, [new("item.ice", 1000)])]),
            new("ice", [new("ice-pure", "Ice", 1, [new("item.ice", 1000)])]),
            new("mixed", [new("mixed-rock", "Silicate", 1, [new("item.ore", 900), new("item.silicon", 100)])]),
            new("carbon", [new("carbon-rich", "Silicate", 1, [new("item.carbon-ore", 800), new("item.ore", 200)])])
        ],
        [new("Small", 1000000, 9999999, 60), new("Medium", 10000000, 99999999, 30),
            new("Large", 100000000, 499999999, 8), new("VeryLarge", 500000000, 999999999, 2), new("Zero", 1000000000, 1000000000, 0)],
        new(120, 60000, 85));
    private static TradingMapGenerationData Rules() => new(
        1, "SPC-0002", 700, 21_600_000, 64_800_000, 129_600_000, 5, 2,
        [
            new("SPC-0002", "market.transit", "Transit", "Large"),
            new("SPC-0005", "market.mining", "Mining", "Medium"),
            new("SPC-0006", "market.industrial", "Industrial", "Medium"),
            new("SPC-0007", "market.hydroponic", "Hydroponic", "Medium"),
            new("SPC-0008", "market.scientific-military", "Scientific/Military", "Medium"),
        ],
        [new("seeded",
        [
            new("SPC-0002", "SPC-0005", "risk.safe"),
            new("SPC-0005", "SPC-0006", "risk.normal"),
            new("SPC-0006", "SPC-0007", "risk.normal"),
            new("SPC-0006", "SPC-0008", "risk.elevated"),
            new("SPC-0007", "SPC-0008", "risk.normal"),
            new("SPC-0008", "SPC-0002", "risk.elevated"),
            new("SPC-0005", "SPC-0008", "risk.normal"),
        ],
        [
            new("SPC-0002", 0, 0),
            new("SPC-0005", 100, 0),
            new("SPC-0006", 0, 1000),
            new("SPC-0007", -1000, 0),
            new("SPC-0008", 0, 1500),
        ])],
        [new("risk.elevated", 1250), new("risk.normal", 1100), new("risk.safe", 1000)]);

    private static GameDataRegistry Registry() => GameDataRegistry.Create(
        [new ModuleCategoryDefinition("module.test", "Test", 1, [])],
        [new ModuleTypeDefinition("module.test", "Test", 1, 10, 100, 0, [], CargoCapacityKg: 100)],
        [new("item.ore", "Ore", 1, 10, Category: TradeCategory.Resource), new("item.steel", "Steel", 1, 10),
            new("item.fuel", "Fuel", 0, 10, TradeUnit: TradeUnit.Kilogram, StorageKind: ItemStorageKind.FuelTank),
            new("item.food", "Food", 1, 10), new("item.science", "Science", 1, 10),
            new("item.ice", "Ice", 1, 10, Category: TradeCategory.Resource),
            new("item.magnesium-ore", "Magnesium", 1, 10, Category: TradeCategory.Resource),
            new("item.silicon", "Silicon", 1, 10, Category: TradeCategory.Resource),
            new("item.carbon-ore", "Carbon", 1, 10, Category: TradeCategory.Resource)], [], stationMarketProfiles: Profiles());
    private static StationMarketProfileDefinition[] Profiles()
    {
        var factors = new Dictionary<StationSize, int>
        {
            [StationSize.Outpost] = 500,
            [StationSize.Medium] = 1000,
            [StationSize.Large] = 1500,
            [StationSize.Huge] = 2000,
        }.ToImmutableDictionary();

        return
        [
            Profile("market.transit", [], ["item.ore"], factors),
            Profile("market.mining", ["item.ore"], ["item.steel"], factors),
            Profile("market.industrial", ["item.steel"], ["item.ore", "item.food", "item.science"], factors),
            Profile("market.hydroponic", ["item.food"], ["item.steel"], factors),
            Profile("market.scientific-military", ["item.science"], ["item.food"], factors),
        ];
    }

    private static StationMarketProfileDefinition Profile(
        string id,
        IReadOnlyList<string> supply,
        IReadOnlyList<string> demand,
        ImmutableDictionary<StationSize, int> factors) =>
        new(id, id, supply.ToImmutableArray(), demand.ToImmutableArray(),
            supply.Concat(demand).Distinct(StringComparer.Ordinal)
                .Select(item => new StationMarketStockDefinition(item, 1)).ToImmutableArray(),
            1000, 100, factors);
}
