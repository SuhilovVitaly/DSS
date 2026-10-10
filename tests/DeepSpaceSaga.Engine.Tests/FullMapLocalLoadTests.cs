using System.Text.Json;
using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Engine.LocalClient;
using DeepSpaceSaga.Engine.Scenario;

namespace DeepSpaceSaga.Engine.Tests;

public sealed class FullMapLocalLoadTests
{
    private static string Settings => Path.Combine(SeededWorldBootstrapTests.ClientRoot, "Settings.json");

    [Theory]
    [InlineData("in-flight")]
    [InlineData("docked")]
    [InlineData("paused")]
    public async Task LocalFullMapResumeMatrix(string state)
    {
        string directory = Path.Combine(Path.GetTempPath(), "dss-full-map-" + Guid.NewGuid().ToString("N"));
        using var continuous = new ClusterVoyageFixture(42, true, 1000000);
        ClusterVoyageFixture? captured = null;
        try
        {
            continuous.Trade(TradeCommandTypes.Buy, continuous.OutboundItem);
            if (state != "paused") continuous.FlyTo(continuous.Destination, midpoint => { if (state == "in-flight") captured = midpoint.Reload(); });
            else continuous.Advance(12000);
            captured ??= continuous.Reload();
            await using (var transport = new LocalGameSessionConnection(captured.Engine, directory)) await transport.SaveAsync("world");
            string path = Path.Combine(directory, "world.json"); byte[] before = File.ReadAllBytes(path);
            var saved = ScenarioLoader.LoadFromFile(path, true); var map = saved.GameState.AiMap!;
            Assert.NotEmpty(map.Bases); Assert.NotEmpty(map.Territories); Assert.NotEmpty(map.Fields); Assert.NotEmpty(map.PointsOfInterest);
            await using (var factory = LocalGameSessionConnection.CreateFromSaveFile(Settings, path, directory))
            {
                using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(10)); bool received = false;
                await foreach (var snapshot in factory.ReadSnapshotsAsync(timeout.Token))
                {
                    received = true;
                    Assert.Equal(SimulationSpeed.Speed0, snapshot.CurrentSpeed);
                    Assert.Equal(saved.GameState.GameTimeMs, snapshot.GameTimeMs); Assert.Equal(saved.GameState.MotionTimeMs, snapshot.MotionTimeMs);
                    Assert.Equal(JsonSerializer.Serialize(map), JsonSerializer.Serialize(snapshot.AiMap));
                    Assert.Equal(state == "in-flight" ? VoyagePhases.InTransit : VoyagePhases.Docked, snapshot.Voyage!.Phase);
                    foreach (var territory in map.Territories) Assert.Contains(snapshot.Objects, o => o.ObjectId == territory.BaseObjectId);
                    foreach (var field in map.Fields.Where(f => f.ParentObjectId is not null)) Assert.Contains(snapshot.Objects, o => o.ObjectId == field.ParentObjectId);
                    foreach (var poi in map.PointsOfInterest) Assert.DoesNotContain(snapshot.Objects, o => o.ObjectId == poi.ObjectId);
                    break;
                }
                Assert.True(received);
            }
            Assert.Equal(before, File.ReadAllBytes(path));
            using var resumed = captured.Reload(saved, SimulationEngine.CreateFromSaveFile(Settings, path));
            MapEnvironmentSaveTests.SameMap(captured.Save(), resumed.Save()); ClusterSaveStateTests.SameFacts(captured, resumed);
            if (state == "in-flight") resumed.FinishFlightTo(resumed.Destination);
            ClusterSaveStateTests.SameFacts(continuous, resumed);
            continuous.Advance(12000); resumed.Advance(12000);
            MapEnvironmentSaveTests.SameMap(continuous.Save(), resumed.Save()); ClusterSaveStateTests.SameFacts(continuous, resumed);
            if (state != "paused")
            {
                continuous.Trade(TradeCommandTypes.Sell, continuous.OutboundItem); resumed.Trade(TradeCommandTypes.Sell, resumed.OutboundItem);
                ClusterSaveStateTests.SameFacts(continuous, resumed);
            }
        }
        finally { captured?.Dispose(); if (Directory.Exists(directory)) Directory.Delete(directory, true); }
    }

    [Fact]
    public async Task BrokenAiParentPreventsSessionPublication()
    {
        string directory = Path.Combine(Path.GetTempPath(), "dss-broken-map-" + Guid.NewGuid().ToString("N"));
        try
        {
            using var source = SeededAbandonedObjectsTests.Create();
            await using (var transport = new LocalGameSessionConnection(source, directory)) await transport.SaveAsync("broken");
            string path = Path.Combine(directory, "broken.json"); var saved = ScenarioLoader.LoadFromFile(path, true); var map = saved.GameState.AiMap!;
            var broken = saved with
            {
                GameState = saved.GameState with
                {
                    AiMap = map with
                    { Bases = map.Bases.SetItem(0, map.Bases[0] with { BaseType = "Planetary", ParentObjectId = "missing-planet", Orbit = null }) }
                }
            };
            File.WriteAllText(path, ScenarioLoader.Serialize(broken)); byte[] before = File.ReadAllBytes(path);
            Assert.Throws<ScenarioException>(() => LocalGameSessionConnection.CreateFromSaveFile(Settings, path, directory));
            Assert.Equal(before, File.ReadAllBytes(path));
        }
        finally { if (Directory.Exists(directory)) Directory.Delete(directory, true); }
    }
}
