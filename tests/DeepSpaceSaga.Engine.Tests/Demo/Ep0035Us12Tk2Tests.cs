using DeepSpaceSaga.Engine.Scenario;

namespace DeepSpaceSaga.Engine.Tests.Demo;

public sealed class Ep0035Us12Tk2Tests
{
    [Fact]
    public void Squadron_UsesMinimumMemberSpeedAndMaximumWorkingSensor()
    {
        using var engine = DemoEp0033Us0001Tk0003Tests.Demo();
        var world = engine.CaptureSaveState().GameState.DemoCampaign!.PirateWorld!;
        var group = world.Groups[0];
        var first = group.Members[0] with { SignatureTypeId = "lair" };
        var second = first with { CarrierId = first.CarrierId + "-second" };
        group = group with { Members = [first, second] };
        Assert.Equal((world.CapturedPlayerMaxSpeedKmS * 1.1, 2000d), engine.PirateGroupCapabilities(group, world));
        var sensorOff = first with { Sensor = first.Sensor with { PowerState = "Off" } };
        Assert.Equal(2000, engine.PirateGroupCapabilities(group with { Members = [sensorOff, second] }, world).SensorRadius);
        Assert.Equal(0, engine.PirateGroupCapabilities(group with { Members = [sensorOff, second with { IsDestroyed = true }] }, world).SensorRadius);
        var engineOff = first with { Engine = first.Engine with { StructurePoints = 0 } };
        Assert.Equal(0, engine.PirateGroupCapabilities(group with { Members = [engineOff, second] }, world).MaxSpeedKmS);
        Assert.Equal(world.CapturedPlayerMaxSpeedKmS * 0.8,
            engine.PirateGroupCapabilities(group with { Members = [first with { SignatureTypeId = "tancred" }] }, world).MaxSpeedKmS);
    }

    [Fact]
    public void MixedOrThirdSquadMember_IsRejectedBeforeLoad()
    {
        using var engine = DemoEp0033Us0001Tk0003Tests.Demo();
        var save = engine.CaptureSaveState();
        var campaign = save.GameState.DemoCampaign!;
        var world = campaign.PirateWorld!;
        var group = world.Groups[0];
        var member = group.Members[0];
        var invalid = world with { Groups = world.Groups.SetItem(0, group with { Members = [member, member, member] }) };
        Assert.Throws<ScenarioException>(() => engine.LoadScenario(save with
        { GameState = save.GameState with { DemoCampaign = campaign with { PirateWorld = invalid } } }, true));
        Assert.Equal(4, engine.CaptureSaveState().GameState.DemoCampaign!.PirateWorld!.Groups.Length);
    }
}
