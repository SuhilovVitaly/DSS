using System.Reflection;
using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Engine;
using DeepSpaceSaga.Engine.LocalClient;
using DeepSpaceSaga.Engine.Scenario;

namespace DeepSpaceSaga.Client.Tests;

public class SaveReliabilityTests
{
    [Fact]
    public async Task Save_waits_for_gate_before_capturing_world()
    {
        string directory = Path.Combine(Path.GetTempPath(), "dss-save-test-" + Guid.NewGuid().ToString("N"));
        var engine = new SimulationEngine();
        engine.SetSpeed(SimulationSpeed.Speed0);
        await using var connection = new LocalGameSessionConnection(engine, directory);
        // Hold the real serialization gate to deterministically model another save.
        var gate = (SemaphoreSlim)typeof(LocalGameSessionConnection)
            .GetField("_saveGate", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(connection)!;
        await gate.WaitAsync();
        Task save;
        try
        {
            save = connection.SaveAsync("slot").AsTask();
            Assert.False(save.IsCompleted);
            engine.SetSpeed(SimulationSpeed.Speed2);
        }
        finally { gate.Release(); }
        try
        {
            await save;
            using var json = System.Text.Json.JsonDocument.Parse(await File.ReadAllTextAsync(Path.Combine(directory, "slot.json")));
            Assert.Equal("Speed2", json.RootElement.GetProperty("gameState").GetProperty("currentSpeed").GetString());
            Assert.Empty(Directory.GetFiles(directory, "*.tmp"));
        }
        finally { if (Directory.Exists(directory)) Directory.Delete(directory, true); }
    }

    [Fact]
    public async Task Failed_rename_cleans_temp_file_and_releases_gate()
    {
        string directory = Path.Combine(Path.GetTempPath(), "dss-save-test-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Path.Combine(directory, "blocked.json"));
        await using var connection = new LocalGameSessionConnection(new SimulationEngine(), directory);
        try
        {
            var error = await Record.ExceptionAsync(() => connection.SaveAsync("blocked").AsTask());
            Assert.True(error is IOException or UnauthorizedAccessException, $"Unexpected failure: {error}");
            Assert.Empty(Directory.GetFiles(directory, "*.tmp"));
            await connection.SaveAsync("working");
            Assert.True(File.Exists(Path.Combine(directory, "working.json")));
        }
        finally { Directory.Delete(directory, true); }
    }
}
