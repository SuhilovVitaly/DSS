using System.Globalization;
using DeepSpaceSaga.Client.UI.Demo;
using DeepSpaceSaga.Contracts;

namespace DeepSpaceSaga.Client.Tests.Demo;

public sealed class DemoEp0036Us0006Tk0004Tests
{
    [Theory]
    [InlineData("ru-RU", "23,75")]
    [InlineData("en-US", "23.75")]
    public void ChanceHasExactLocalizedPrecision(string culture, string expected) =>
        Assert.Equal(expected, ModuleSalvagePresentation.ChanceText(2375, CultureInfo.GetCultureInfo(culture)));

    [Fact]
    public async Task InstanceIdentityAndTerminalStateAreAuthoritative()
    {
        DemoCampaignCommand? sent = null;
        var pending = new TaskCompletionSource<DemoCampaignResponse>();
        var presentation = new BattleLootPresentation(c => { sent = c; return pending.Task; });
        var module = new ModuleLootSnapshot("engine/0", "engine", "drive.standard", "item.trophy.engine.drive.standard", "Drive", "intact", 1000, 2625,
            LootAttemptStates.NotAttempted, true, null);
        presentation.Observe(new("battle", LootPhases.Open,
            [new("drone", "DisabledDrone", [], null, [module, module with { InstanceId = "engine/1", State = LootAttemptStates.Failed, CanAttempt = false,
                Reason = BattleLootReasons.AlreadyAttempted }], null)], true), 4);
        presentation.Select("drone");
        var panel = new ModuleSalvagePresentation(presentation);
        Assert.True(panel.Rows(CultureInfo.InvariantCulture)[0].Enabled);
        Assert.False(panel.Rows(CultureInfo.InvariantCulture)[1].Enabled);
        var task = panel.AttemptAsync("engine/0");
        Assert.All(panel.Rows(CultureInfo.InvariantCulture), row => Assert.False(row.Enabled));
        var command = Assert.IsType<AttemptModuleSalvageCommand>(sent);
        Assert.Equal("engine/0", command.InstanceId); Assert.Equal("drone", command.ObjectId); Assert.Equal(4, command.ExpectedRevision);
        pending.SetResult(new(command.CommandId, false, BattleLootReasons.CargoFull, 4));
        Assert.False(await task);
    }
}
