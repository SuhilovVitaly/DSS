using DeepSpaceSaga.Engine.LocalClient;
using DeepSpaceSaga.Contracts;

namespace DeepSpaceSaga.Engine.Tests.Demo;

[Collection("Demo process debug")]
public sealed class Ep0035Us0008Tk0005Tests
{
    [Fact]
    public async Task ProcessOptionsApplyBeforeFirstConnectionSnapshot()
    {
        var original = LocalGameSessionConnection.ConfigureFogOfWar();
        try
        {
            LocalGameSessionConnection.ConfigureFogOfWar(false);
            LocalGameSessionConnection.ConfigurePirateSquadChance(100);
            using var engine = DemoEp0033Us0001Tk0003Tests.Demo();
            engine.SetSpeed(SimulationSpeed.Speed0);
            await using var connection = new LocalGameSessionConnection(engine);
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(10));
            await foreach (var snapshot in connection.ReadSnapshotsAsync(timeout.Token))
            {
                Assert.False(snapshot.DemoCampaign!.FogOfWar!.Enabled);
                Assert.Equal(4, snapshot.DemoCampaign.Encounters!.Groups.Length);
                Assert.All(snapshot.DemoCampaign.Encounters.Groups, g => Assert.True(g.IsDebugVisible));
                Assert.All(engine.CaptureSaveState().GameState.DemoCampaign!.PirateWorld!.Groups,
                    g => Assert.Equal(2, g.Members.Length));
                break;
            }
        }
        finally
        {
            LocalGameSessionConnection.ConfigureFogOfWar(original.FogOfWar);
            LocalGameSessionConnection.ConfigurePirateSquadChance(original.PirateSquadChancePercent);
        }
    }
}
