using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Engine.Scenario;

namespace DeepSpaceSaga.Engine.Tests.Demo;

public sealed class DemoEp0036Us0011Tk0001Tests
{
    [Fact]
    public void TrackedOpenAreaCoversPatrol_AndLoadKeepsGeometry()
    {
        using var engine = DemoEp0033Us0001Tk0003Tests.Demo();
        var order = DemoEp0036Us0001Tk0002Tests.Order(engine);
        Assert.Null(order.SearchArea);
        Assert.True(engine.ExecuteDemoCampaign(new AcceptBountyCommand("track", engine.CaptureSnapshot().DemoCampaign!.Revision, order.BountyId)).Applied);
        var area = DemoEp0036Us0001Tk0002Tests.Order(engine).SearchArea!;
        Assert.NotNull(area);
        var save = engine.CaptureSaveState();
        foreach (var point in save.GameState.DemoCampaign!.PirateWorld!.Groups.Single(g => g.GroupId == order.TargetGroupId).PatrolPoints)
            Assert.True(Math.Sqrt(Math.Pow(point.X - area.X, 2) + Math.Pow(point.Y - area.Y, 2)) <= area.Radius);
        engine.LoadScenario(ScenarioLoader.LoadFromJson(ScenarioLoader.Serialize(save), true, true), true);
        Assert.Equal(area, DemoEp0036Us0001Tk0002Tests.Order(engine).SearchArea);
        Assert.True(engine.ExecuteDemoCampaign(new CancelBountyCommand("cancel", engine.CaptureSnapshot().DemoCampaign!.Revision, order.BountyId)).Applied);
        Assert.Null(DemoEp0036Us0001Tk0002Tests.Order(engine).SearchArea);
    }
    [Fact]
    public void VictoryHasNoSearchArea()
    {
        using var engine = Ep0035Us15Tk3Tests.Begin();
        DemoEp0036Us0001Tk0002Tests.Finish(engine, true);
        Assert.Null(DemoEp0036Us0001Tk0002Tests.Order(engine).SearchArea);
    }
}
