using System.Collections.Immutable;
using System.Text.Json;
using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Engine.Content;
using DeepSpaceSaga.Engine.Rng;
using DeepSpaceSaga.Engine.Scenario;
using DeepSpaceSaga.Engine.Trading;

namespace DeepSpaceSaga.Engine.Tests;

public sealed class RouteRiskIntegrationTests
{
    private const long Hour = GameCalendar.HourMs;
    private const string Ship = "SPC-0001";

    private static StationMarketEventDefinition Definition(string id = "event.pirate-blockade", string availability = "Restricted",
        int chance = 1000, int priority = 900)
    {
        var source = QuotedTradeExecutionTests.RealRegistry();
        return source.StationMarketEvents.GetDefinition(source.StationMarketEvents.GetIndex(id)) with
        {
            ChancePermillePerHour = chance,
            Priority = priority,
            MinDurationHours = 1,
            MaxDurationHours = 1,
            EligibleMarketProfileIds = ["market.transit"],
            ItemEffects = [new("item.food-rations", 1000, 1000, 1100, -1)],
            RouteEffect = new(availability, 1, 1250, 1500),
        };
    }

    private static GameDataRegistry Registry(params StationMarketEventDefinition[] definitions)
    {
        var r = QuotedTradeExecutionTests.RealRegistry();
        return GameDataRegistry.Create(
            Enumerable.Range(0, r.ModuleCategories.Count).Select(r.ModuleCategories.GetDefinition),
            Enumerable.Range(0, r.ModuleTypes.Count).Select(r.ModuleTypes.GetDefinition),
            Enumerable.Range(0, r.ItemTypes.Count).Select(r.ItemTypes.GetDefinition),
            Enumerable.Range(0, r.CommandDefinitions.Count).Select(r.CommandDefinitions.GetDefinition),
            Enumerable.Range(0, r.FactoryTypes.Count).Select(r.FactoryTypes.GetDefinition),
            Enumerable.Range(0, r.Recipes.Count).Select(r.Recipes.GetDefinition),
            Enumerable.Range(0, r.Dialogues.Count).Select(r.Dialogues.GetDefinition),
            Enumerable.Range(0, r.Quests.Count).Select(r.Quests.GetDefinition),
            legacyCatalogFingerprint: r.LegacyCatalogFingerprint,
            stationMarketProfiles: Enumerable.Range(0, r.StationMarketProfiles.Count).Select(r.StationMarketProfiles.GetDefinition),
            shipClasses: Enumerable.Range(0, r.ShipClasses.Count).Select(r.ShipClasses.GetDefinition), stationMarketEvents: definitions);
    }

    private static SimulationEngine Create(GameDataRegistry registry, ulong seed = 1)
    {
        var engine = new SimulationEngine(registry, [], new SimulationClock(SimulationSpeed.Speed0, () => 0));
        var s = VoyageLifecycleTests.DockedScenario();
        engine.LoadScenario(s with
        {
            GameState = s.GameState with
            {
                MasterSeed = seed,
                SpaceObjects = s.GameState.SpaceObjects.Select(o => o.ObjectId == Ship ? o with { Crew = [], Passengers = [], Modules = o.Modules?.Select(m => m with { OperatorCrewId = null }).ToArray() } : o).ToArray()
            }
        });
        return engine;
    }

    private static AuthoritativeSnapshot Snapshot(SimulationEngine e, long time = Hour) => e.CaptureSnapshotForTests(time, simulationTimeMs: 0);
    private static ScenarioFile Save(SimulationEngine e, long time = Hour) => e.CaptureSaveStateForTests(time, SimulationSpeed.Speed0, 0);
    private static string Routes(AuthoritativeSnapshot s) => JsonSerializer.Serialize(s.TradingRoutes);

    [Fact]
    public void Docked_snapshot_publishes_only_sorted_outgoing_effective_routes()
    {
        using var engine = Create(Registry(Definition()));
        var before = Snapshot(engine, 0);
        var active = Snapshot(engine);
        var affected = Assert.Single(active.TradingRoutes.Where(r => !r.ActiveEventIds.IsDefaultOrEmpty));
        Assert.Equal(TradingRouteAvailability.Restricted, affected.Availability);
        Assert.Equal(TradingRouteRisk.Elevated, affected.Risk);
        Assert.Equal((long)decimal.Round(affected.BaseTravelEstimateGameTimeMs * 1.25m, 0, MidpointRounding.AwayFromZero), affected.EffectiveTravelEstimateGameTimeMs);
        Assert.Equal((int)decimal.Round(affected.BaseFuelMultiplierPermille * 1.5m, 0, MidpointRounding.AwayFromZero), affected.EffectiveFuelMultiplierPermille);
        string origin = active.Objects.Single(o => o.ObjectId == Ship).DockedStationObjectId!;
        Assert.All(active.TradingRoutes, r => Assert.Equal(origin, r.OriginStationObjectId));
        Assert.Equal(before.TradingRoutes.Select(r => r.DestinationStationObjectId), active.TradingRoutes.Select(r => r.DestinationStationObjectId));
        Assert.Equal(active.TradingRoutes.Select(r => r.DestinationStationObjectId).Order(StringComparer.Ordinal), active.TradingRoutes.Select(r => r.DestinationStationObjectId));
        Assert.Equal(Routes(active), Routes(Snapshot(engine)));
        var option = active.Voyage!.RouteOptions.Single(o => o.DestinationStationObjectId == affected.DestinationStationObjectId);
        Assert.Equal(affected.EffectiveTravelEstimateGameTimeMs, option.TravelEstimateGameTimeMs);
        Assert.True(option.IsAvailable);
    }

    [Fact]
    public void Blockade_activates_first_candidate_that_preserves_connected_alternative_without_extra_rng()
    {
        var first = Definition(availability: "Unavailable");
        var second = Definition("event.quarantine", "Unavailable", priority: 800);
        var registry = Registry(first, second);
        bool foundFallback = false;
        for (ulong seed = 1; seed <= 64 && !foundFallback; seed++)
        {
            using var engine = Create(registry, seed);
            var before = Save(engine, 0);
            var origin = before.GameState.SpaceObjects.Single(o => o.ObjectId == Ship).DockedStationObjectId!;
            var map = before.GameState.TradingMap!;
            var incident = TradingRouteEvaluator.Evaluate(map, []).Select(e => e.BaseEdge)
                .Where(e => e.FromStationObjectId == origin || e.ToStationObjectId == origin)
                .OrderBy(e => e.FromStationObjectId, StringComparer.Ordinal).ThenBy(e => e.ToStationObjectId, StringComparer.Ordinal).ToArray();
            if (incident.Length != 2) continue;
            int Roll(string id) => (int)(RngStreamSeedDerivation.DeriveStreamSeed(seed, $"market-event:route:{origin}:{id}:1") % 2);
            if (Roll(first.TypeId) == Roll(second.TypeId)) continue;
            var snapshot = Snapshot(engine);
            var saved = Save(engine);
            var events = saved.GameState.SpaceObjects.Single(o => o.ObjectId == origin).Events!;
            Assert.Equal(2, events.Count);
            var blocked = Assert.Single(snapshot.TradingRoutes.Where(r => r.Availability == TradingRouteAvailability.Unavailable));
            Assert.Equal(2, blocked.ActiveEventIds.Length);
            var selected = events.Single(e => e.DefinitionId == second.TypeId).RouteEffect!;
            Assert.Equal(events.Single(e => e.DefinitionId == first.TypeId).RouteEffect!.FromStationObjectId, selected.FromStationObjectId);
            Assert.Equal(events.Single(e => e.DefinitionId == first.TypeId).RouteEffect!.ToStationObjectId, selected.ToStationObjectId);
            Assert.NotEqual(incident[Roll(second.TypeId)], map.Edges.Single(e =>
                e.FromStationObjectId == selected.FromStationObjectId && e.ToStationObjectId == selected.ToStationObjectId));
            Assert.Equal(before.GameState.TradingMap!.RngStreams.ToArray(), saved.GameState.TradingMap!.RngStreams.ToArray());
            using var loaded = new SimulationEngine(registry);
            loaded.LoadScenario(ScenarioLoader.LoadFromJson(ScenarioLoader.Serialize(saved), true));
            Assert.Equal(Routes(snapshot), Routes(Snapshot(loaded)));
            foundFallback = true;
        }
        Assert.True(foundFallback, "The seed corpus must exercise rejection of an isolating first candidate and acceptance of the next.");
    }

    [Theory]
    [InlineData("Unavailable")]
    [InlineData("Restricted")]
    public void Unavailable_destination_is_rejected_and_restricted_destination_uses_effective_duration(string availability)
    {
        using var engine = Create(Registry(Definition(availability: availability)));
        var snapshot = Snapshot(engine);
        var route = Assert.Single(snapshot.TradingRoutes.Where(r => r.ActiveEventIds.Length > 0));
        var ship = snapshot.Objects.Single(o => o.ObjectId == Ship);
        engine.ReceiveCommand(VoyageLifecycleTests.Undock("depart", route.DestinationStationObjectId));
        var departed = Snapshot(engine);
        var result = Assert.Single(departed.CommandResults);
        if (availability == "Unavailable")
        {
            Assert.Equal(CommandResultStatus.Rejected, result.Status);
            Assert.Equal(CommandReasonCodes.RouteUnavailable, result.ReasonCode);
            Assert.True(departed.Objects.Single(o => o.ObjectId == Ship).IsDocked);
            Assert.Equal(VoyagePhases.Docked, Save(engine).GameState.VoyageState!.Phase);
            Assert.Equal(ship.SpeedKmS, departed.Objects.Single(o => o.ObjectId == Ship).SpeedKmS);
            return;
        }
        Assert.Equal(CommandResultStatus.Executed, result.Status);
        var saved = Save(engine).GameState.VoyageState!;
        Assert.Equal(route.EffectiveTravelEstimateGameTimeMs, saved.TravelEstimateGameTimeMs);
        Assert.Equal(route.EffectiveFuelMultiplierPermille, saved.FuelMultiplierPermille);
        Assert.Equal(route.RiskProfileId, saved.RiskProfileId);
        Assert.Equal(route.ActiveEventIds.ToArray(), saved.ActiveEventIds!.ToArray());
        Assert.Equal(Hour + route.EffectiveTravelEstimateGameTimeMs, saved.ArrivalGameTimeMs);
        Assert.Equal(Hour, saved.StartedGameTimeMs);
        Assert.Empty(departed.TradingRoutes);
        Assert.Equal(ship.SpeedKmS, departed.Objects.Single(o => o.ObjectId == Ship).SpeedKmS);
        Assert.Equal(snapshot.SimulationTimeMs, departed.SimulationTimeMs);
        Assert.Equal(ship.ApproachRoute, departed.Objects.Single(o => o.ObjectId == Ship).ApproachRoute);
    }

    [Fact]
    public void Started_voyage_keeps_captured_terms_when_event_expires_and_across_save_load()
    {
        var registry = Registry(Definition());
        using var engine = Create(registry);
        var route = Assert.Single(Snapshot(engine).TradingRoutes.Where(r => r.ActiveEventIds.Length > 0));
        engine.ReceiveCommand(VoyageLifecycleTests.Undock("frozen", route.DestinationStationObjectId));
        Snapshot(engine);
        var original = Save(engine).GameState.VoyageState!;
        var activeSave = Save(engine);
        using var loaded = new SimulationEngine(registry);
        loaded.LoadScenario(ScenarioLoader.LoadFromJson(ScenarioLoader.Serialize(activeSave), true));
        Assert.Equal(JsonSerializer.Serialize(original), JsonSerializer.Serialize(Save(loaded).GameState.VoyageState));
        Snapshot(engine, 2 * Hour);
        Snapshot(loaded, 2 * Hour);
        Assert.Equal(JsonSerializer.Serialize(original), JsonSerializer.Serialize(Save(engine, 2 * Hour).GameState.VoyageState));
        Assert.Equal(JsonSerializer.Serialize(original), JsonSerializer.Serialize(Save(loaded, 2 * Hour).GameState.VoyageState));
        Assert.DoesNotContain(Save(engine, 2 * Hour).GameState.SpaceObjects.SelectMany(o => o.Events ?? []), e => original.ActiveEventIds!.Contains(e.EventId));
    }

    [Fact]
    public void Expiry_restores_base_projection_once_without_replaying_event()
    {
        // Select a seed with an activation at hour 1 and no activation at hour 2.
        const string origin = "SPC-0002";
        ulong seed = Enumerable.Range(1, 20000).Select(i => (ulong)i).First(s =>
            RngStreamSeedDerivation.DeriveStreamSeed(s, $"market-event:roll:{origin}:event.pirate-blockade:1") % 1000 < 10 &&
            RngStreamSeedDerivation.DeriveStreamSeed(s, $"market-event:roll:{origin}:event.pirate-blockade:2") % 1000 >= 10);
        using var engine = Create(Registry(Definition(chance: 10)), seed);
        Assert.Single(Snapshot(engine).TradingRoutes.Where(r => r.ActiveEventIds.Length > 0));
        var expired = Snapshot(engine, 2 * Hour);
        Assert.All(expired.TradingRoutes, r =>
        {
            Assert.Empty(r.ActiveEventIds);
            Assert.Equal(TradingRouteAvailability.Available, r.Availability);
            Assert.Equal(r.BaseTravelEstimateGameTimeMs, r.EffectiveTravelEstimateGameTimeMs);
            Assert.Equal(r.BaseFuelMultiplierPermille, r.EffectiveFuelMultiplierPermille);
        });
        Assert.Equal(Routes(expired), Routes(Snapshot(engine, 2 * Hour)));
        Assert.Empty(expired.DockedStationTrade!.ActiveEvents);
    }

    [Theory]
    [InlineData("missing-endpoint")]
    [InlineData("unknown-edge")]
    [InlineData("nonincident-edge")]
    [InlineData("partial-voyage-terms")]
    [InlineData("bad-arrival")]
    public void Invalid_saved_route_or_captured_terms_reject_atomically(string corruption)
    {
        var registry = Registry(Definition());
        using var engine = Create(registry);
        var route = Assert.Single(Snapshot(engine).TradingRoutes.Where(r => r.ActiveEventIds.Length > 0));
        engine.ReceiveCommand(VoyageLifecycleTests.Undock("saved", route.DestinationStationObjectId));
        Snapshot(engine);
        var valid = Save(engine);
        string before = ScenarioLoader.Serialize(valid);
        var bad = valid;
        if (corruption is "partial-voyage-terms" or "bad-arrival")
            bad = valid with
            {
                GameState = valid.GameState with
                {
                    VoyageState = corruption == "partial-voyage-terms"
                ? valid.GameState.VoyageState! with { FuelMultiplierPermille = null }
                : valid.GameState.VoyageState! with { ArrivalGameTimeMs = 1 }
                }
            };
        else
        {
            var unrelated = valid.GameState.TradingMap!.Edges.First(e => e.FromStationObjectId != route.OriginStationObjectId && e.ToStationObjectId != route.OriginStationObjectId);
            bad = valid with
            {
                GameState = valid.GameState with
                {
                    SpaceObjects = valid.GameState.SpaceObjects.Select(o => o.ObjectId != route.OriginStationObjectId ? o : o with
                    {
                        Events = o.Events!.Select(e => e with
                        {
                            RouteEffect = corruption switch
                            {
                                "missing-endpoint" => e.RouteEffect! with { ToStationObjectId = null },
                                "unknown-edge" => e.RouteEffect! with { ToStationObjectId = "missing" },
                                _ => e.RouteEffect! with { FromStationObjectId = unrelated.FromStationObjectId, ToStationObjectId = unrelated.ToStationObjectId },
                            }
                        }).ToArray()
                    }).ToArray()
                }
            };
        }
        Assert.Throws<ScenarioException>(() => engine.LoadScenario(bad, isSave: true));
        Assert.Equal(before, ScenarioLoader.Serialize(Save(engine)));
    }

    [Fact]
    public void Legacy_unbound_route_event_resolves_once_and_persists_its_selected_edge()
    {
        var registry = Registry(Definition());
        using var engine = Create(registry);
        var expected = Routes(Snapshot(engine));
        var save = Save(engine);
        var old = save with
        {
            GameState = save.GameState with
            {
                SpaceObjects = save.GameState.SpaceObjects.Select(o => o with
                {
                    Events = o.Events?.Select(e => e.RouteEffect is null ? e : e with
                    {
                        RouteEffect = e.RouteEffect with
                        { FromStationObjectId = null, ToStationObjectId = null }
                    }).ToArray()
                }).ToArray()
            }
        };
        using var loaded = new SimulationEngine(registry);
        loaded.LoadScenario(ScenarioLoader.LoadFromJson(ScenarioLoader.Serialize(old), true));
        Assert.Equal(expected, Routes(Snapshot(loaded)));
        Assert.All(Save(loaded).GameState.SpaceObjects.SelectMany(o => o.Events ?? []).Where(e => e.RouteEffect is not null),
            e => Assert.NotNull(e.RouteEffect!.FromStationObjectId));
    }


    [Fact]
    public void Route_restriction_does_not_change_physical_acceleration_or_approach()
    {
        var restrictedDefinition = Definition();
        using var restricted = Create(Registry(restrictedDefinition));
        using var baseline = Create(Registry(restrictedDefinition with { RouteEffect = null }));
        var route = Assert.Single(Snapshot(restricted).TradingRoutes.Where(r => r.ActiveEventIds.Length > 0));
        foreach (var engine in new[] { restricted, baseline })
        {
            Snapshot(engine);
            engine.ReceiveCommand(VoyageLifecycleTests.Undock("motion-depart", route.DestinationStationObjectId));
            Snapshot(engine);
            engine.ReceiveCommand(new("accelerate", 2, Ship, "MOD-PLAYER-ENGINE-01", ShipEngineCommandTypes.Accelerate));
            engine.CaptureSnapshotForTests(Hour + 1000, simulationTimeMs: 1000);
            engine.CaptureSnapshotForTests(Hour + 60000, simulationTimeMs: 60000);
            engine.ReceiveCommand(new("approach", 3, Ship, "MOD-PLAYER-ENGINE-01", NavigationComputerCommandTypes.Approach,
                TargetObjectId: route.DestinationStationObjectId));
        }
        var actual = restricted.CaptureSnapshotForTests(Hour + 61000, simulationTimeMs: 61000);
        var expected = baseline.CaptureSnapshotForTests(Hour + 61000, simulationTimeMs: 61000);
        var ship = actual.Objects.Single(o => o.ObjectId == Ship);
        Assert.True(ship.SpeedKmS > 0);
        Assert.NotNull(ship.ApproachRoute);
        Assert.Equal(JsonSerializer.Serialize(expected.Objects.Single(o => o.ObjectId == Ship)), JsonSerializer.Serialize(ship));
        Assert.Equal(expected.SimulationTimeMs, actual.SimulationTimeMs);
        Assert.Equal(VoyagePhases.InTransit, actual.Voyage!.Phase);
    }

    [Fact]
    public void Departure_revalidates_after_calendar_event_activates()
    {
        using var engine = Create(Registry(Definition(availability: "Unavailable")));
        var before = Snapshot(engine, 0);
        // A selection made from the previously available snapshot is only an intent.
        string destination = before.TradingRoutes.First().DestinationStationObjectId;
        var active = Snapshot(engine);
        destination = active.TradingRoutes.Single(r => r.Availability == TradingRouteAvailability.Unavailable).DestinationStationObjectId;
        Assert.Equal(TradingRouteAvailability.Available, before.TradingRoutes.Single(r => r.DestinationStationObjectId == destination).Availability);
        engine.ReceiveCommand(VoyageLifecycleTests.Undock("old-selection", destination));
        var result = Assert.Single(Snapshot(engine).CommandResults);
        Assert.Equal(CommandReasonCodes.RouteUnavailable, result.ReasonCode);
        Assert.Equal(CommandResultStatus.Rejected, result.Status);
    }


    [Fact]
    public void Loaded_captured_event_ids_are_isolated_from_the_callers_mutable_save_array()
    {
        var registry = Registry(Definition());
        using var source = Create(registry);
        var route = Assert.Single(Snapshot(source).TradingRoutes.Where(r => r.ActiveEventIds.Length > 0));
        source.ReceiveCommand(VoyageLifecycleTests.Undock("isolation", route.DestinationStationObjectId));
        Snapshot(source);
        var save = Save(source);
        var ids = save.GameState.VoyageState!.ActiveEventIds!.ToArray();
        save = save with { GameState = save.GameState with { VoyageState = save.GameState.VoyageState with { ActiveEventIds = ids } } };
        using var loaded = new SimulationEngine(registry);
        loaded.LoadScenario(save, isSave: true);
        ids[0] = "changed-after-load";
        Assert.DoesNotContain("changed-after-load", Save(loaded).GameState.VoyageState!.ActiveEventIds!);
    }

    [Fact]
    public void Legacy_route_binding_migration_is_independent_of_saved_event_array_order()
    {
        var a = Definition(availability: "Unavailable");
        var b = Definition("event.quarantine", "Unavailable", priority: 800);
        var registry = Registry(a, b);
        bool exercised = false;
        for (ulong seed = 1; seed <= 64 && !exercised; seed++)
        {
            using var source = Create(registry, seed);
            var initial = Save(source, 0);
            string origin = initial.GameState.SpaceObjects.Single(o => o.ObjectId == Ship).DockedStationObjectId!;
            var map = initial.GameState.TradingMap!;
            if (map.Edges.Count(e => e.FromStationObjectId == origin || e.ToStationObjectId == origin) != 2) continue;
            ulong Roll(string id) => RngStreamSeedDerivation.DeriveStreamSeed(seed, $"market-event:route:{origin}:{id}:1") % 2;
            if (Roll(a.TypeId) == Roll(b.TypeId)) continue;
            Snapshot(source);
            var save = Save(source);
            ScenarioFile Legacy(bool reverse) => save with
            {
                GameState = save.GameState with
                {
                    SpaceObjects = save.GameState.SpaceObjects.Select(o => o.Events is not { Count: > 0 } ? o : o with
                    {
                        Events = (reverse ? o.Events.AsEnumerable().Reverse() : o.Events).Select(e => e with
                        { RouteEffect = e.RouteEffect! with { FromStationObjectId = null, ToStationObjectId = null } }).ToArray()
                    }).ToArray()
                }
            };
            using var first = new SimulationEngine(registry);
            using var second = new SimulationEngine(registry);
            first.LoadScenario(Legacy(false), isSave: true);
            second.LoadScenario(Legacy(true), isSave: true);
            Assert.Equal(Routes(Snapshot(first)), Routes(Snapshot(second)));
            exercised = true;
        }
        Assert.True(exercised);
    }


    [Fact]
    public void Saved_incident_closures_that_isolate_a_station_reject_before_world_replacement()
    {
        var registry = Registry(Definition(availability: "Unavailable"), Definition("event.quarantine", "Unavailable", priority: 800));
        using var engine = Create(registry);
        var snapshot = Snapshot(engine);
        string origin = snapshot.Objects.Single(o => o.ObjectId == Ship).DockedStationObjectId!;
        var saved = Save(engine);
        var incident = TradingRouteEvaluator.Evaluate(saved.GameState.TradingMap!, []).Select(e => e.BaseEdge)
            .Where(e => e.FromStationObjectId == origin || e.ToStationObjectId == origin).ToArray();
        Assert.Equal(2, incident.Length);
        var bad = saved with
        {
            GameState = saved.GameState with
            {
                SpaceObjects = saved.GameState.SpaceObjects.Select(o => o.ObjectId != origin ? o : o with
                {
                    Events = o.Events!.Select((e, i) => e with
                    {
                        RouteEffect = e.RouteEffect! with
                        { FromStationObjectId = incident[i].FromStationObjectId, ToStationObjectId = incident[i].ToStationObjectId }
                    }).ToArray()
                }).ToArray()
            }
        };
        string before = ScenarioLoader.Serialize(saved);
        Assert.Throws<ScenarioException>(() => engine.LoadScenario(bad, isSave: true));
        Assert.Equal(before, ScenarioLoader.Serialize(Save(engine)));
    }

    [Fact]
    public void Legacy_no_map_snapshot_has_no_routes()
    {
        using var engine = new SimulationEngine(Registry());
        var scenario = VoyageLifecycleTests.DockedScenario();
        engine.LoadScenario(scenario with { GameState = scenario.GameState with { TradingMapGeneration = null, TradingMap = null } });
        Assert.Empty(Snapshot(engine, 0).TradingRoutes);
    }
}
