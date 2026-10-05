using System.Text;
using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Engine.LocalClient;
using DeepSpaceSaga.Engine.Scenario;

namespace DeepSpaceSaga.Engine.Tests;

public sealed class LocalWorldSaveRoundtripTests
{
    private static string Settings => Path.Combine(SeededWorldBootstrapTests.ClientRoot, "Settings.json");

    [Theory]
    [InlineData("Default")]
    [InlineData("Default_500")]
    [InlineData("Docked")]
    [InlineData("Undocked")]
    [InlineData("MarketProfiles")]
    [InlineData("PlayerShipOnly")]
    public async Task LocalSaveRestoresSameSystem(string name)
    {
        string folder = Path.Combine(Path.GetTempPath(), "dss-system-save-" + Guid.NewGuid());
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
        string folder = Path.Combine(Path.GetTempPath(), "dss-bad-system-save-" + Guid.NewGuid());
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
