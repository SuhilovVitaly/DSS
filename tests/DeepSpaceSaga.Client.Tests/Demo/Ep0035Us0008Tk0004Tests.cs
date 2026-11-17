using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Client.UI.DebugConsole;

namespace DeepSpaceSaga.Client.Tests.Demo;

public sealed class Ep0035Us0008Tk0004Tests
{
    [Fact]
    public async Task MenuAppliesImmediately_ActiveRejectionDoesNotChangeProcessOrAcknowledgeSuccess()
    {
        var state = new DemoDebugSettings();
        DemoDebugSettings Configure(bool? value) => state = value is { } v ? state with { FogOfWar = v } : state;
        Assert.False((await AppDebugOverrides.FogAsync(false, Configure, null, null, default)).IsError);
        Assert.False(state.FogOfWar);
        var rejected = await AppDebugOverrides.FogAsync(true, Configure, 4, (command, _) =>
        {
            Assert.Equal(4, command.ExpectedRevision);
            Assert.True(Assert.IsType<SetFogOfWarRequest>(command).Enabled);
            return Task.FromResult(new DemoCampaignResponse(command.CommandId, false, DemoCampaignReasons.StaleRevision, 5));
        }, default);
        Assert.True(rejected.IsError);
        Assert.False(state.FogOfWar);
        Assert.Contains("false", (await AppDebugOverrides.FogAsync(null, Configure, 5, null, default)).Lines[0]);
    }
}
