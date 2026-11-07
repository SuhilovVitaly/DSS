using System.Text.Json;
using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Engine.LocalClient;
using DeepSpaceSaga.Engine.Scenario;

namespace DeepSpaceSaga.Engine.Tests;

public sealed class ClusterLocalResumeTests
{
    private static string Settings => Path.Combine(SeededWorldBootstrapTests.ClientRoot, "Settings.json");

    [Theory]
    [InlineData(true, 1)]
    [InlineData(false, 1)]
    [InlineData(true, 2)]
    [InlineData(false, 2)]
    public async Task V1AndV2ResumeSameGeometryAndVoyage(bool inFlight, int version)
    {
        string directory = LocalWorldSaveRoundtripTests.RunDirectory("voyage");
        var config = version == 2 ? GenerationInputSchemaTests.V2Config() : GenerationInputSchemaTests.Config() with
        { Clusters = new ClusterGenerationConfig(3, 5, 10, 12, 0.5, 2, 3, 7, 15, 35) };
        using var continuous = new ClusterVoyageFixture(42, true, 1000000, config);
        ClusterVoyageFixture? captured = null;
        try
        {
            continuous.Trade(TradeCommandTypes.Buy, continuous.OutboundItem);
            continuous.FlyTo(continuous.Destination, midpoint => { if (inFlight) captured = midpoint.Reload(); });
            captured ??= continuous.Reload();
            await using (var connection = new LocalGameSessionConnection(captured.Engine, directory))
                await connection.SaveAsync("voyage");
            string path = Path.Combine(directory, "voyage.json"); string bytes = File.ReadAllText(path);
            var saved = ScenarioLoader.LoadFromFile(path, true);
            Assert.NotNull(saved.GameState.ClusterMap); Assert.Equal(inFlight ? VoyagePhases.InTransit : VoyagePhases.Docked, saved.GameState.VoyageState!.Phase);
            await using (var factory = LocalGameSessionConnection.CreateFromSaveFile(Settings, path, directory))
            {
                using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(10));
                await foreach (var snapshot in factory.ReadSnapshotsAsync(timeout.Token))
                {
                    Assert.Equal(saved.GameState.GameTimeMs, snapshot.GameTimeMs);
                    Assert.Equal(JsonSerializer.Serialize(saved.GameState.ClusterMap), JsonSerializer.Serialize(snapshot.ClusterMap));
                    Assert.Equal(saved.GameState.VoyageState.DestinationStationObjectId, snapshot.Voyage!.DestinationStationObjectId);
                    break;
                }
            }
            Assert.Equal(bytes, File.ReadAllText(path));
            using var resumed = captured.Reload(saved, SimulationEngine.CreateFromSaveFile(Settings, path));
            if (inFlight) resumed.FinishFlightTo(resumed.Destination);
            ClusterSaveStateTests.SameFacts(continuous, resumed);
            continuous.Trade(TradeCommandTypes.Sell, continuous.OutboundItem); resumed.Trade(TradeCommandTypes.Sell, resumed.OutboundItem);
            continuous.Trade(TradeCommandTypes.Buy, continuous.ReturnItem); resumed.Trade(TradeCommandTypes.Buy, resumed.ReturnItem);
            continuous.FlyTo(continuous.Origin); resumed.FlyTo(resumed.Origin);
            continuous.Trade(TradeCommandTypes.Sell, continuous.ReturnItem); resumed.Trade(TradeCommandTypes.Sell, resumed.ReturnItem);
            ClusterSaveStateTests.SameFacts(continuous, resumed);
        }
        finally { captured?.Dispose(); if (Directory.Exists(directory)) Directory.Delete(directory, true); }
    }

    [Fact]
    public async Task RetriedTradeAfterLocalLoadIsIdempotent()
    {
        string directory = LocalWorldSaveRoundtripTests.RunDirectory("retry");
        using var source = new ClusterVoyageFixture(1, false);
        try
        {
            var quote = source.Engine.GetTradeQuote(new("persistent-buy", ClusterVoyageFixture.Ship, ClusterVoyageFixture.Cargo, TradeCommandTypes.Buy, source.OutboundItem, 1));
            var command = new PlayerCommand("persistent-command", 999, ClusterVoyageFixture.Ship, ClusterVoyageFixture.Cargo, TradeCommandTypes.Buy,
                ItemTypeId: source.OutboundItem, Quantity: 1, QuoteId: quote.QuoteId, MarketRevision: quote.MarketRevision);
            await using (var connection = new LocalGameSessionConnection(source.Engine, directory))
            {
                await connection.SendCommandAsync(command); source.Capture(); await connection.SaveAsync("trade");
            }
            string path = Path.Combine(directory, "trade.json");
            var saved = ScenarioLoader.LoadFromFile(path, true);
            using var engine = SimulationEngine.CreateFromSaveFile(Settings, path);
            string before = ScenarioLoader.Serialize(engine.CaptureSaveState());
            await using var loaded = new LocalGameSessionConnection(engine, directory);
            await loaded.SendCommandAsync(command);
            var replay = engine.CaptureSnapshotForTests(saved.GameState.GameTimeMs, SimulationSpeed.Speed0, saved.GameState.MotionTimeMs);
            Assert.Single(replay.CommandResults, r => r.CommandId == command.CommandId);
            Assert.Equal(JsonSerializer.Serialize(saved.GameState.CommandReceipts!.Single(r => r.CommandId == command.CommandId).TradeReceipt),
                JsonSerializer.Serialize(replay.CommandResults.Single(r => r.CommandId == command.CommandId).TradeReceipt));
            Assert.Equal(before, ScenarioLoader.Serialize(engine.CaptureSaveState()));
            await loaded.SaveAsync("replayed");
            Assert.Equal(JsonSerializer.Serialize(saved.GameState.SpaceObjects), JsonSerializer.Serialize(ScenarioLoader.LoadFromFile(Path.Combine(directory, "replayed.json"), true).GameState.SpaceObjects));
        }
        finally { if (Directory.Exists(directory)) Directory.Delete(directory, true); }
    }
}
