using System.Text;
using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Engine.LocalClient;
using DeepSpaceSaga.Engine.Scenario;

namespace DeepSpaceSaga.Engine.Tests;

public sealed class LocalWorldSaveRoundtripTests
{
    private static string Settings => Path.Combine(SeededWorldBootstrapTests.ClientRoot, "Settings.json");
    internal static string RunDirectory(string name) => Path.Combine("D:/DeepSpaceSaga/artifacts/validation/ep-0018-map-geography-and-scale",
        "local-save-" + name + "-" + Guid.NewGuid().ToString("N"));

    [Theory]
    [InlineData("Default")]
    [InlineData("Default_500")]
    [InlineData("Docked")]
    [InlineData("Undocked")]
    [InlineData("MarketProfiles")]
    [InlineData("PlayerShipOnly")]
    public async Task V2AllScenariosIgnoreChangedGenerationContentOnLoad(string name)
    {
        string folder = RunDirectory(name);
        Directory.CreateDirectory(folder);
        try
        {
            using var bootstrap = StationGeographyBootstrapTests.Create(name, GenerationInputSchemaTests.V2Config());
            long realMs = 0;
            using var original = new SimulationEngine(SeededWorldBootstrapTests.Registry(),
                clock: new SimulationClock(SimulationSpeed.Speed0, () => Interlocked.Read(ref realMs)));
            original.LoadScenario(bootstrap.CaptureSaveState(), true);
            original.SetSpeed(SimulationSpeed.Speed1);
            Interlocked.Exchange(ref realMs, 288000);
            original.SetSpeed(SimulationSpeed.Speed0);
            var expected = original.CaptureSnapshot();
            await using var connection = new LocalGameSessionConnection(original, folder);
            await connection.SaveAsync("world");
            string file = Path.Combine(folder, "world.json");
            byte[] bytes = File.ReadAllBytes(file);
            await using (var factory = LocalGameSessionConnection.CreateFromSaveFile(Settings, file, folder))
            {
                using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(10));
                await foreach (var snapshot in factory.ReadSnapshotsAsync(timeout.Token))
                {
                    GeneratedWorldPersistenceTests.EqualWorld(expected, snapshot);
                    break;
                }
            }
            var registry = SeededWorldBootstrapTests.Registry();
            registry = registry.WithModuleTypes([.. Enumerable.Range(0, registry.ModuleTypes.Count).Select(i =>
            {
                var m = registry.ModuleTypes.GetDefinition(i);
                return m.MaxSpeedMps is > 0 ? m with { MaxSpeedMps = m.MaxSpeedMps * 2 } : m;
            })]);
            using var resumed = new SimulationEngine(registry);
            resumed.LoadScenario(ScenarioLoader.LoadFromFile(file, true, true), true,
                generation: GenerationInputSchemaTests.V2Config() with { GeneratorVersion = 999 });
            Assert.Equal(expected.ClusterMap!.Geography, resumed.CaptureSnapshot().ClusterMap!.Geography);
            original.SetSpeed(SimulationSpeed.Speed1);
            Interlocked.Add(ref realMs, 1000);
            original.SetSpeed(SimulationSpeed.Speed0);
            var next = original.CaptureSnapshot();
            GeneratedWorldPersistenceTests.EqualWorld(next,
                resumed.CaptureSnapshotForTests(next.GameTimeMs, SimulationSpeed.Speed0, next.SimulationTimeMs));
            Assert.Equal(bytes, File.ReadAllBytes(file));
            var save = ScenarioLoader.LoadFromFile(file, true, true);
            foreach (string failure in new[] { "unknown", "mixed" })
            {
                var invalid = save with
                {
                    GameState = save.GameState with
                    {
                        SolarSystem = save.GameState.SolarSystem! with
                        { GeneratorVersion = failure == "unknown" ? 999 : 1 }
                    }
                };
                string badFile = Path.Combine(folder, failure + ".json");
                byte[] badBytes = Encoding.UTF8.GetBytes(ScenarioLoader.Serialize(invalid));
                File.WriteAllBytes(badFile, badBytes);
                string before = ScenarioLoader.Serialize(resumed.CaptureSaveState());
                Assert.Throws<ScenarioException>(() => resumed.LoadScenario(ScenarioLoader.LoadFromFile(badFile, true, true), true));
                Assert.Equal(before, ScenarioLoader.Serialize(resumed.CaptureSaveState()));
                Assert.Equal(badBytes, File.ReadAllBytes(badFile));
                Assert.Throws<ScenarioException>(() => LocalGameSessionConnection.CreateFromSaveFile(Settings, badFile, folder));
            }
        }
        finally { Directory.Delete(folder, true); }
    }

    [Theory]
    [InlineData("Default")]
    [InlineData("Default_500")]
    [InlineData("Docked")]
    [InlineData("Undocked")]
    [InlineData("MarketProfiles")]
    [InlineData("PlayerShipOnly")]
    public async Task LocalSaveRestoresSameSystem(string name)
    {
        string folder = RunDirectory("system");
        Directory.CreateDirectory(folder);
        try
        {
            using var bootstrap = SimulationEngine.CreateFromScenarioFile(Settings, Path.Combine(SeededWorldBootstrapTests.ClientRoot, "Scenarios", name, "scenario.json"));
            bootstrap.SetSpeed(SimulationSpeed.Speed0);
            long realMs = 0;
            var clock = new SimulationClock(SimulationSpeed.Speed0, () => Interlocked.Read(ref realMs));
            var original = new SimulationEngine(SeededWorldBootstrapTests.Registry(), clock: clock);
            original.LoadScenario(bootstrap.CaptureSaveState(), isSave: true);
            await using var connection = new LocalGameSessionConnection(original, folder);
            original.SetSpeed(SimulationSpeed.Speed1);
            Interlocked.Exchange(ref realMs, 34567);
            original.SetSpeed(SimulationSpeed.Speed0);
            var expected = original.CaptureSnapshot();
            await connection.SaveAsync("system");
            string file = Path.Combine(folder, "system.json");
            byte[] bytes = File.ReadAllBytes(file);
            await using (var restored = LocalGameSessionConnection.CreateFromSaveFile(Settings, file))
            {
                using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(10));
                await using var snapshots = restored.ReadSnapshotsAsync(timeout.Token).GetAsyncEnumerator();
                Assert.True(await snapshots.MoveNextAsync());
                GeneratedWorldPersistenceTests.EqualWorld(expected, snapshots.Current);
                Assert.Equal(SimulationSpeed.Speed0, snapshots.Current.CurrentSpeed);
                Assert.Equal(bytes, File.ReadAllBytes(file));
            }
            using var continued = SimulationEngine.CreateFromSaveFile(Settings, file);
            original.SetSpeed(SimulationSpeed.Speed1);
            Interlocked.Add(ref realMs, 1000);
            original.SetSpeed(SimulationSpeed.Speed0);
            var next = original.CaptureSnapshot();
            GeneratedWorldPersistenceTests.EqualWorld(next,
                continued.CaptureSnapshotForTests(next.GameTimeMs, SimulationSpeed.Speed0, next.SimulationTimeMs));
            Assert.Equal(bytes, File.ReadAllBytes(file));
        }
        finally { Directory.Delete(folder, true); }
    }

    [Theory]
    [InlineData("version")]
    [InlineData("parent")]
    [InlineData("json")]
    public void FailedLoadLeavesSaveUntouched(string failure)
    {
        using var engine = SimulationEngine.CreateFromScenarioFile(Settings,
            Path.Combine(SeededWorldBootstrapTests.ClientRoot, "Scenarios", "Docked", "scenario.json"));
        engine.SetSpeed(SimulationSpeed.Speed0);
        var save = engine.CaptureSaveState();
        if (failure == "version") save = save with { SaveFormatVersion = SaveFormat.CurrentSaveFormatVersion + 1 };
        if (failure == "parent") save = save with
        {
            GameState = save.GameState with
            {
                SpaceObjects = save.GameState.SpaceObjects.Select(o =>
            o.IsDocked ? o with { DockedStationObjectId = "missing" } : o).ToArray()
            }
        };
        string folder = RunDirectory("invalid");
        Directory.CreateDirectory(folder);
        try
        {
            string file = Path.Combine(folder, "system.json");
            byte[] bytes = Encoding.UTF8.GetBytes(failure == "json" ? "{broken" : ScenarioLoader.Serialize(save));
            File.WriteAllBytes(file, bytes);
            Assert.Throws<ScenarioException>(() => LocalGameSessionConnection.CreateFromSaveFile(Settings, file));
            Assert.Equal(bytes, File.ReadAllBytes(file));
            Assert.Equal(new[] { file }, Directory.GetFiles(folder));
        }
        finally { Directory.Delete(folder, true); }
    }
}
