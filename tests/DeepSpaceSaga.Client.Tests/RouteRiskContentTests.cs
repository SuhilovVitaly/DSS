using System.Collections.Immutable;
using System.Text.Json;
using DeepSpaceSaga.Client.UI;
using DeepSpaceSaga.Client.UI.Screens.Station;
using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Engine;
using DeepSpaceSaga.Engine.Content;
using DeepSpaceSaga.Engine.LocalClient;
using DeepSpaceSaga.Engine.Scenario;
using DeepSpaceSaga.Engine.Trading;
using SkiaSharp;

namespace DeepSpaceSaga.Client.Tests;

public sealed class RouteRiskContentTests
{
    private const long Hour = GameCalendar.HourMs;
    private static readonly string Root = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory,
        "..", "..", "..", "..", "..", "src", "DeepSpaceSaga.Client"));
    private static readonly GameDataRegistry Registry = EngineContentLoader.LoadRegistryFromSettingsFile(Path.Combine(Root, "Settings.json"), out _, out _);
    private static ScenarioFile Source(string name = "Docked") => ScenarioLoader.LoadFromFile(Path.Combine(Root, "Scenarios", name, "scenario.json"));

    private static SimulationEngine Create(ulong seed, string? template = null, string name = "Docked")
    {
        var s = Source(name);
        var request = s.GameState.TradingMapGeneration;
        if (template is not null) request = request! with { Templates = request.Templates.Where(t => t.TemplateId == template).ToArray() };
        var engine = new SimulationEngine(Registry, [], new SimulationClock(SimulationSpeed.Speed0, () => 0));
        engine.LoadScenario(s with { GameState = s.GameState with { MasterSeed = seed, CurrentSpeed = "Speed0", TradingMapGeneration = request } });
        return engine;
    }

    private static AuthoritativeSnapshot Snapshot(SimulationEngine e, long time) => e.CaptureSnapshotForTests(time, simulationTimeMs: 0);
    private static ScenarioFile Save(SimulationEngine e, long time) => e.CaptureSaveStateForTests(time, SimulationSpeed.Speed0, 0);
    private static string Routes(AuthoritativeSnapshot s) => JsonSerializer.Serialize(s.TradingRoutes);
    private static IEnumerable<StationEventData> Events(ScenarioFile s) => s.GameState.SpaceObjects.SelectMany(o => o.Events ?? []);

    private static ImmutableArray<TradingRouteModifier> Modifiers(ScenarioFile save) => Events(save).Where(e => e.RouteEffect?.FromStationObjectId is not null)
        .Select(e =>
        {
            var r = e.RouteEffect!;
            var d = Registry.StationMarketEvents.GetDefinition(Registry.StationMarketEvents.GetIndex(e.DefinitionId!));
            return new TradingRouteModifier(e.EventId, e.DefinitionId!, d.Priority, e.StartedGameTimeMs,
                r.FromStationObjectId!, r.ToStationObjectId!, Enum.Parse<TradingRouteAvailability>(r.Availability),
                r.TravelTimeMultiplierPermille, r.FuelMultiplierPermille, d.EffectSummaryKey);
        }).ToImmutableArray();

    [Fact]
    public void Every_template_has_short_medium_long_safe_and_elevated_routes()
    {
        foreach (string scenario in new[] { "Default", "Docked", "Undocked" })
            foreach (var template in Source(scenario).GameState.TradingMapGeneration!.Templates)
            {
                using var engine = Create(1, template.TemplateId, scenario);
                var map = Save(engine, 0).GameState.TradingMap!;
                Assert.Equal(new[] { "Long", "Medium", "Short" }, map.Edges.Select(e => e.DistanceClass).Distinct().Order(StringComparer.Ordinal));
                string start = map.Rules.StartStationObjectId;
                var safe = Assert.Single(map.Edges, e => e.RiskProfileId == "risk.safe");
                Assert.Equal("Short", safe.DistanceClass);
                Assert.True(safe.FromStationObjectId == start || safe.ToStationObjectId == start);
                Assert.Equal(1000, safe.FuelMultiplierPermille);
                var risky = map.Edges.Where(e => e.RiskProfileId == "risk.elevated").ToArray();
                Assert.NotEmpty(risky);
                Assert.All(risky, e => Assert.Equal(1300, e.FuelMultiplierPermille));
                Assert.Contains(risky, e => e.DistanceClass is "Medium" or "Long");
                Assert.All(map.Rules.Stations, station => Assert.True(map.Edges.Count(e =>
                    e.FromStationObjectId == station.ObjectId || e.ToStationObjectId == station.ObjectId) >= 2));
                Assert.All(risky, e => Assert.True(e.TravelEstimateGameTimeMs > safe.TravelEstimateGameTimeMs && e.FuelMultiplierPermille > safe.FuelMultiplierPermille));
            }
    }

    [Fact]
    public void Blockade_and_quarantine_candidates_resolve_and_preserve_an_alternative()
    {
        foreach (var template in Source().GameState.TradingMapGeneration!.Templates)
        {
            using var engine = Create(1, template.TemplateId);
            var map = Save(engine, 0).GameState.TradingMap!;
            // The canonical dependency candidate pairs are the template's links, not
            // a second endpoint catalog that could disagree with materialized edges.
            Assert.True(map.Edges.Count >= 3);
            foreach (var id in new[] { "event.pirate-blockade", "event.quarantine" })
            {
                var definition = Registry.StationMarketEvents.GetDefinition(Registry.StationMarketEvents.GetIndex(id));
                var effect = definition.RouteEffect!;
                Assert.Equal(id == "event.pirate-blockade" ? ("Restricted", 1500, 1250) : ("Unavailable", 1000, 1000),
                    (effect.Availability, effect.TravelTimeMultiplierPermille, effect.FuelMultiplierPermille));
                Assert.All(map.Edges, edge => Assert.True(TradingRouteEvaluator.CanApply(map, [],
                    [new(id, id, definition.Priority, Hour, edge.FromStationObjectId, edge.ToStationObjectId,
                        Enum.Parse<TradingRouteAvailability>(effect.Availability), effect.TravelTimeMultiplierPermille,
                        effect.FuelMultiplierPermille, definition.EffectSummaryKey)])));
            }
        }
    }

    [Fact]
    public void Same_seed_replays_candidate_effective_values_and_expiry_for_all_templates_and_seed_corpus()
    {
        int affected = 0;
        var kinds = new HashSet<string>(StringComparer.Ordinal);
        foreach (var template in Source().GameState.TradingMapGeneration!.Templates)
            foreach (ulong seed in Enumerable.Range(0, 256).Select(i => (ulong)i).Append(ulong.MaxValue))
            {
                using var first = Create(seed, template.TemplateId);
                using var replay = Create(seed, template.TemplateId);
                for (int hour = 0; hour <= 48; hour++)
                {
                    long time = hour * Hour;
                    var a = Snapshot(first, time);
                    var b = Snapshot(replay, time);
                    Assert.Equal(Routes(a), Routes(b));
                    var rows = StationRoutePresentation.Build(a.TradingRoutes);
                    Assert.True(rows.Any(r => r.IsEnabled), $"{template.TemplateId}, seed {seed}, hour {hour}: no outgoing alternative.");
                    if (a.TradingRoutes.Any(r => !r.ActiveEventIds.IsDefaultOrEmpty)) affected++;
                    var save = Save(first, time);
                    Assert.True(TradingRouteEvaluator.CanApply(save.GameState.TradingMap!, Modifiers(save), []));
                    foreach (var e in Events(save).Where(e => e.RouteEffect is not null))
                    {
                        kinds.Add(e.DefinitionId!);
                        Assert.True(e.StartedGameTimeMs <= time && time - e.StartedGameTimeMs < e.DurationMs);
                        Assert.NotNull(e.RouteEffect!.FromStationObjectId);
                    }
                }
            }
        Assert.True(affected > 0, "The corpus must observe real shipping events, not just empty schedules.");
        Assert.Equal(new[] { "event.pirate-blockade", "event.quarantine" }, kinds.Order(StringComparer.Ordinal));
    }

    private static (ScenarioFile ActiveSave, TradingRouteSnapshot Route, long End) ActiveExpiryFixture()
    {
        // Keep the real probabilities, durations and full catalog. Find a reproducible
        // isolated local effect whose natural expiry is not obscured by another event.
        for (ulong seed = 0; seed < 256; seed++)
        {
            using var engine = Create(seed);
            for (int hour = 1; hour <= 48; hour++)
            {
                var active = Snapshot(engine, hour * Hour);
                var routes = active.TradingRoutes.Where(r => r.ActiveEventIds.Length == 1).ToArray();
                if (routes.Length == 0) continue;
                var route = routes[0];
                var save = Save(engine, hour * Hour);
                var e = Events(save).Single(e => e.EventId == route.ActiveEventIds[0]);
                long end = e.StartedGameTimeMs + e.DurationMs!.Value;
                var before = Snapshot(engine, end - 1);
                var after = Snapshot(engine, end);
                var beforeRoute = before.TradingRoutes.Single(r => r.DestinationStationObjectId == route.DestinationStationObjectId);
                var afterRoute = after.TradingRoutes.Single(r => r.DestinationStationObjectId == route.DestinationStationObjectId);
                if (beforeRoute.ActiveEventIds.Length == 1 && afterRoute.ActiveEventIds.IsEmpty)
                    return (save, route, end);
                // The engine has already advanced to end; restart for the next seed.
                break;
            }
        }
        throw new Xunit.Sdk.XunitException("No shipping event with an isolated exact expiry found in seed corpus.");
    }

    [Fact]
    public void Expiry_boundary_restores_exact_base_values_and_same_saved_seed_replays()
    {
        var fixture = ActiveExpiryFixture();
        using var first = new SimulationEngine(Registry);
        using var replay = new SimulationEngine(Registry);
        foreach (var engine in new[] { first, replay }) engine.LoadScenario(ScenarioLoader.LoadFromJson(ScenarioLoader.Serialize(fixture.ActiveSave), true));
        var active = Snapshot(first, fixture.End - 1).TradingRoutes.Single(r => r.DestinationStationObjectId == fixture.Route.DestinationStationObjectId);
        Assert.Contains(fixture.Route.ActiveEventIds[0], active.ActiveEventIds);
        var expired = Snapshot(first, fixture.End);
        Assert.Equal(Routes(expired), Routes(Snapshot(replay, fixture.End)));
        var route = expired.TradingRoutes.Single(r => r.DestinationStationObjectId == fixture.Route.DestinationStationObjectId);
        Assert.Empty(route.ActiveEventIds);
        Assert.Equal(route.BaseTravelEstimateGameTimeMs, route.EffectiveTravelEstimateGameTimeMs);
        Assert.Equal(route.BaseFuelMultiplierPermille, route.EffectiveFuelMultiplierPermille);
        Assert.Equal(TradingRouteAvailability.Available, route.Availability);
        Assert.Equal(Routes(expired), Routes(Snapshot(first, fixture.End)));
    }

    [Fact]
    public async Task Public_local_session_publishes_packaged_reason_in_station_rows_and_exact_expiry()
    {
        var fixture = ActiveExpiryFixture();
        string temp = Path.Combine(Path.GetTempPath(), "dss-route-content-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(temp);
        try
        {
            string file = Path.Combine(temp, "active.json");
            File.WriteAllText(file, ScenarioLoader.Serialize(fixture.ActiveSave));
            await using var connection = LocalGameSessionConnection.CreateFromSaveFile(Path.Combine(Root, "Settings.json"), file);
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(10));
            await using var stream = connection.ReadSnapshotsAsync(timeout.Token).GetAsyncEnumerator(timeout.Token);
            Assert.True(await stream.MoveNextAsync());
            var snapshot = stream.Current;
            var row = StationRoutePresentation.Build(snapshot.TradingRoutes).Single(r => r.DestinationStationObjectId == fixture.Route.DestinationStationObjectId);
            Assert.False(string.IsNullOrWhiteSpace(row.ReasonText));
            foreach (string language in new[] { "English", "Russian" })
            {
                var locale = JsonSerializer.Deserialize<Dictionary<string, string>>(File.ReadAllText(Path.Combine(Root, "Data", "Locale", language + ".json")))!;
                Assert.All(row.ReasonText!.Split("; "), key => Assert.True(locale.ContainsKey(key)));
            }
            var buffer = new SnapshotBuffer();
            buffer.Update(snapshot);
            var station = new StationScreen(buffer);
            using var bitmap = new SKBitmap(1920, 1080);
            using var canvas = new SKCanvas(bitmap);
            station.Render(canvas, 1920, 1080);
            Assert.Equal(row.ReasonText, station.RouteRows.Single(r => r.DestinationStationObjectId == row.DestinationStationObjectId).ReasonText);
            var current = snapshot;
            int hop = 0;
            while (current.GameTimeMs < fixture.End)
            {
                var currentRoute = current.TradingRoutes.Single(r => r.DestinationStationObjectId == fixture.Route.DestinationStationObjectId);
                Assert.Contains(fixture.Route.ActiveEventIds[0], currentRoute.ActiveEventIds);
                var travel = await connection.TravelStationAsync(new("route-expiry-hop-" + ++hop,
                    current.CurrentStationDistrict == StationDistrict.Dock ? StationDistrict.Market : StationDistrict.Dock), timeout.Token);
                Assert.True(travel.Accepted, travel.Error);
                Assert.Equal(current.GameTimeMs + Hour, travel.Snapshot.GameTimeMs);
                current = travel.Snapshot;
            }
            Assert.Equal(fixture.End, current.GameTimeMs);
            var expiredRoute = current.TradingRoutes.Single(r => r.DestinationStationObjectId == fixture.Route.DestinationStationObjectId);
            Assert.Empty(expiredRoute.ActiveEventIds);
            Assert.Equal(expiredRoute.BaseTravelEstimateGameTimeMs, expiredRoute.EffectiveTravelEstimateGameTimeMs);
            Assert.Equal(expiredRoute.BaseFuelMultiplierPermille, expiredRoute.EffectiveFuelMultiplierPermille);
            if (Environment.GetEnvironmentVariable("DSS_TACTICAL_RENDER_DIR") is { Length: > 0 } directory)
            {
                Directory.CreateDirectory(directory);
                using var image = SKImage.FromBitmap(bitmap);
                using var data = image.Encode(SKEncodedImageFormat.Png, 100);
                using var output = File.Create(Path.Combine(directory, "station-route-risk.png"));
                data.SaveTo(output);
            }
        }
        finally { Directory.Delete(temp, recursive: true); }
    }

    [Fact]
    public void Default_500_and_legacy_scenario_do_not_gain_routes_or_events()
    {
        using var defaults = Create(1, name: "Default_500");
        var snapshot = Snapshot(defaults, 0);
        Assert.Empty(snapshot.TradingRoutes);
        Assert.Empty(Events(Save(defaults, 0)));
        var legacy = Source() with { GameState = Source().GameState with { TradingMapGeneration = null, TradingMap = null } };
        using var engine = new SimulationEngine(Registry);
        engine.LoadScenario(legacy);
        Assert.Empty(Snapshot(engine, 0).TradingRoutes);
        Assert.Empty(Events(Save(engine, 0)));
    }
}
