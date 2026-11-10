using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Engine;
using DeepSpaceSaga.Engine.Content;
using DeepSpaceSaga.Engine.LocalClient;
using DeepSpaceSaga.Engine.Scenario;

namespace DeepSpaceSaga.Client.Tests;

public sealed class AllScenarioSystemContentTests
{
    [Fact]
    public void AllEnabledStartsPublishV2Geography()
    {
        var config = EngineContentLoader.LoadSolarSystemGenerationConfig(DefaultSystemContentTests.Settings)!;
        Assert.Equal(6, config.EnabledScenarios.Count);
        foreach (string name in config.EnabledScenarios)
        {
            using var engine = SimulationEngine.CreateFromScenarioFile(DefaultSystemContentTests.Settings,
                Path.Combine(AppContext.BaseDirectory, "Scenarios", name, "scenario.json"));
            AssertGeography(engine.CaptureSnapshot());
        }
    }

    internal static void AssertGeography(AuthoritativeSnapshot snapshot, double? legacyFactoryRadiusKm = null)
    {
        Assert.Equal(2, snapshot.SolarSystemMap!.GeneratorVersion);
        var map = snapshot.ClusterMap!;
        Assert.Equal(2, map.RulesVersion);
        double scale = double.Hypot(100, 55) / 3.5;
        Assert.Equal(scale, map.Geography!.MinimumStationDays, 9);
        Assert.InRange(map.Clusters.Length, 3, 5);
        double units = map.Geography.ReferenceSpeedKmS * 86400 / 300 * 10;
        var objects = snapshot.Objects.ToDictionary(o => o.ObjectId);
        foreach (var a in map.Stations)
        {
            var others = map.Stations.Where(b => b.ObjectId != a.ObjectId).Select(b =>
                (Station: b, Days: double.Hypot(objects[a.ObjectId].X - objects[b.ObjectId].X,
                    objects[a.ObjectId].Y - objects[b.ObjectId].Y) / units)).ToArray();
            Assert.All(others, b => Assert.True(b.Days >= scale - 1e-8));
            double nearest = others.Where(b => b.Station.ClusterId == a.ClusterId).Min(b => b.Days);
            Assert.InRange(nearest, scale - 1e-8, 3 * scale + 1e-8);
            var station = objects[a.ObjectId];
            var field = Assert.Single(map.ResourceBindings, b => b.AnchorStationId == a.ObjectId);
            AssertCompanion(objects[field.FieldId], station, double.Hypot(100, 55));
            string prefix = $"FACTORY-{a.ObjectId}-";
            var factories = snapshot.Objects.Where(o => o.ObjectId.StartsWith(prefix, StringComparison.Ordinal)).ToArray();
            Assert.Equal(5, factories.Length);
            Assert.All(factories, factory =>
            {
                Assert.Equal(SpaceObjectType.Factory, factory.RenderObjectType);
                Assert.Equal(station.SpeedKmS, factory.SpeedKmS);
                Assert.Equal(station.Direction, factory.Direction);
            });
            var radii = factories.Select(factory => double.Hypot(factory.X - station.X, factory.Y - station.Y) / 10).ToArray();
            if (legacyFactoryRadiusKm is { } wideRadius)
                Assert.All(radii, radius => Assert.Equal(wideRadius, radius, 6));
            else
            {
                Assert.All(radii, radius => Assert.InRange(radius, 2.5, 4.5));
                Assert.Equal(5, radii.Select(radius => Math.Round(radius, 3)).Distinct().Count());
                var angles = factories.Select(factory => Math.Atan2(factory.Y - station.Y, factory.X - station.X)).Order().ToArray();
                var gaps = angles.Zip(angles.Skip(1), (first, next) => next - first).Append(angles[0] + Math.Tau - angles[^1]);
                Assert.Equal(5, gaps.Select(gap => Math.Round(gap, 3)).Distinct().Count());
            }
        }
    }

    private static void AssertCompanion(ObjectMotionSnapshot companion, ObjectMotionSnapshot station, double expectedKm)
    {
        double distanceKm = double.Hypot(companion.X - station.X, companion.Y - station.Y) / 10;
        Assert.Equal(expectedKm, distanceKm, 6);
        Assert.Equal(station.SpeedKmS, companion.SpeedKmS);
        Assert.Equal(station.Direction, companion.Direction);
    }

    [Fact]
    public void StationFormationsRoundTripThroughSave20()
    {
        using var engine = SimulationEngine.CreateFromSettingsFile(DefaultSystemContentTests.Settings);
        AssertGeography(Reload(engine.CaptureSaveState()));
    }

    [Fact]
    public void EarlyFormationSavesKeepTheirOriginalFactoryRadius()
    {
        using var engine = SimulationEngine.CreateFromSettingsFile(DefaultSystemContentTests.Settings);
        var save = engine.CaptureSaveState();
        var state = save.GameState;
        var fields = state.StationResourceFields!;
        double wideRadius = 2 * double.Hypot(100, 55);
        var objects = state.SpaceObjects.Select(o =>
        {
            if (o.ObjectType != SpaceObjectType.Factory) return o;
            double ratio = wideRadius * 10 / double.Hypot(o.WorldOffsetX, o.WorldOffsetY);
            return o with
            {
                PositionX = o.PositionX + o.WorldOffsetX * (ratio - 1),
                PositionY = o.PositionY + o.WorldOffsetY * (ratio - 1),
                WorldOffsetX = o.WorldOffsetX * ratio,
                WorldOffsetY = o.WorldOffsetY * ratio
            };
        }).ToArray();
        var earlySave = save with
        {
            GameState = state with
            {
                SpaceObjects = objects,
                StationResourceFields = fields with { Rules = fields.Rules with { FactoryRadiusKm = null } }
            }
        };
        AssertGeography(Reload(earlySave), wideRadius);
    }

    private static AuthoritativeSnapshot Reload(ScenarioFile source)
    {
        var save = ScenarioLoader.LoadFromJson(ScenarioLoader.Serialize(source), allowNonZeroGameTime: true, isSave: true);
        var registry = EngineContentLoader.LoadRegistryFromSettingsFile(DefaultSystemContentTests.Settings, out _, out _);
        using var restored = new SimulationEngine(registry);
        restored.LoadScenario(save, isSave: true);
        return restored.CaptureSnapshot();
    }

    [Fact]
    public async Task EveryListedScenarioStarts()
    {
        var scenarios = ScenarioRepository.ListScenarios(Path.Combine(AppContext.BaseDirectory, "Scenarios"));
        var config = EngineContentLoader.LoadSolarSystemGenerationConfig(DefaultSystemContentTests.Settings)!;
        Assert.All(config.EnabledScenarios, name => Assert.Contains(scenarios,
            s => Path.GetFileName(Path.GetDirectoryName(s.ScenarioPath)) == name));
        foreach (var scenario in scenarios.Where(s => config.EnabledScenarios.Contains(
            Path.GetFileName(Path.GetDirectoryName(s.ScenarioPath)), StringComparer.Ordinal)))
        {
            await using var connection = LocalGameSessionConnection.CreateFromScenarioFile(DefaultSystemContentTests.Settings, scenario.ScenarioPath);
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(10));
            await using var snapshots = connection.ReadSnapshotsAsync(timeout.Token).GetAsyncEnumerator();
            Assert.True(await snapshots.MoveNextAsync());
            var snapshot = snapshots.Current;
            Assert.Single(snapshot.Objects, o => o.ObjectType == SpaceObjectType.Sun);
            var ship = snapshot.Objects.Single(o => o.ObjectId == snapshot.PlayerShipObjectId);
            Assert.InRange(double.Hypot(ship.X, ship.Y) / 10 / ship.MaxSpeedKmS!.Value * 300 / 86400,
                config.StartMinDays * config.DistanceScale, config.StartMaxDays * config.DistanceScale);
            string name = Path.GetFileName(Path.GetDirectoryName(scenario.ScenarioPath))!;
            var sourceAsteroids = ScenarioLoader.LoadFromFile(scenario.ScenarioPath).GameState.SpaceObjects
                .Where(o => o.ObjectType == SpaceObjectType.Asteroid).ToArray();
            if (name == "Default_500") Assert.Equal(500, sourceAsteroids.Length);
            foreach (var asteroid in sourceAsteroids)
                Assert.NotNull(snapshot.Objects.Single(o => o.ObjectId == asteroid.ObjectId).Orbit);
            Assert.InRange(snapshot.ClusterMap!.Clusters.Single(c => c.Id == snapshot.ClusterMap.StartClusterId).StationIds.Length, 10, 12);
            Assert.Equal(name == "Docked", ship.IsDocked);
        }
    }

    [Fact]
    public async Task SavedLegacySessionDoesNotGenerate()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "Scenarios", "Default", "scenario.json");
        var registry = EngineContentLoader.LoadRegistryFromSettingsFile(DefaultSystemContentTests.Settings, out _, out _);
        using var engine = new SimulationEngine(registry);
        engine.LoadScenario(ScenarioLoader.LoadFromFile(path));
        var save = engine.CaptureSaveState();
        Assert.Null(save.GameState.SolarSystem);
        string folder = Path.Combine(Path.GetTempPath(), "dss-legacy-system-" + Guid.NewGuid());
        Directory.CreateDirectory(folder);
        try
        {
            string file = Path.Combine(folder, "save.json");
            File.WriteAllText(file, ScenarioLoader.Serialize(save));
            await using var connection = LocalGameSessionConnection.CreateFromSaveFile(DefaultSystemContentTests.Settings, file);
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(10));
            await using var snapshots = connection.ReadSnapshotsAsync(timeout.Token).GetAsyncEnumerator();
            Assert.True(await snapshots.MoveNextAsync());
            Assert.Null(snapshots.Current.SolarSystemMap);
            Assert.Equal(save.GameState.SpaceObjects.Count, snapshots.Current.Objects.Length);
        }
        finally { Directory.Delete(folder, true); }
    }
}
