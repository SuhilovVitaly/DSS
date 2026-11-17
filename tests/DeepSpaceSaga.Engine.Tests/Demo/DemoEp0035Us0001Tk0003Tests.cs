using DeepSpaceSaga.Engine.Scenario;

namespace DeepSpaceSaga.Engine.Tests.Demo;

public sealed class DemoEp0035Us0001Tk0003Tests
{
    [Fact]
    public void ExtremeFiniteCorridor_DoesNotBypassSafeCircle()
    {
        Assert.Equal(0, SimulationEngine.SegmentDistance(new(-1e308, 0), new(1e308, 0), new(0, 0)));
        Assert.Equal(1e308, SimulationEngine.SegmentDistance(new(-1e308, 1e308), new(1e308, 1e308), new(0, 0)));
    }

    [Fact]
    public void NewGame_CreatesFourStableGroupsOutsideStationAndPatrolExclusions()
    {
        using var engine = DemoEp0033Us0001Tk0003Tests.Demo();
        var save = engine.CaptureSaveState();
        var world = save.GameState.DemoCampaign!.PirateWorld!;
        Assert.Equal(4, world.Groups.Length);
        Assert.Equal(4, world.Groups.Select(g => g.GroupId).Distinct().Count());
        var stations = save.GameState.SpaceObjects.Where(o => o.ObjectType == "Station").ToArray();
        var start = stations.Single(s => s.ObjectId == "SPC-0002");
        foreach (var group in world.Groups)
        {
            Assert.InRange(group.Members.Length, 1, 2);
            if (group.Members.Length == 2) Assert.All(group.Members, m => Assert.Equal("lair", m.SignatureTypeId));
            foreach (var station in stations)
                Assert.True(Math.Sqrt(Math.Pow(group.X - station.PositionX, 2) + Math.Pow(group.Y - station.PositionY, 2)) >= 4000);
            for (int i = 0; i < 4; i++)
                Assert.True(SimulationEngine.SegmentDistance(group.PatrolPoints[i], group.PatrolPoints[(i + 1) % 4], new(start.PositionX, start.PositionY)) > 8000);
        }
        Assert.Empty(save.GameState.DemoCampaign.PirateCompositions);
        string expected = System.Text.Json.JsonSerializer.Serialize(world);
        engine.LoadScenario(ScenarioLoader.LoadFromJson(ScenarioLoader.Serialize(save), true, true), true);
        Assert.Equal(expected, System.Text.Json.JsonSerializer.Serialize(engine.CaptureSaveState().GameState.DemoCampaign!.PirateWorld));
    }

    [Fact]
    public void CorruptGroup_RejectsLoadAtomically_AndLegacyDoesNotRespawn()
    {
        using var engine = DemoEp0033Us0001Tk0003Tests.Demo();
        var save = engine.CaptureSaveState();
        var campaign = save.GameState.DemoCampaign!;
        var world = campaign.PirateWorld!;
        var corrupt = world with { Groups = world.Groups.SetItem(1, world.Groups[0]) };
        string before = ScenarioLoader.Serialize(save);
        Assert.Throws<ScenarioException>(() => engine.LoadScenario(save with
        { GameState = save.GameState with { DemoCampaign = campaign with { PirateWorld = corrupt } } }, true));
        Assert.Equal(before, ScenarioLoader.Serialize(engine.CaptureSaveState()));
        engine.LoadScenario(save with { GameState = save.GameState with { DemoCampaign = campaign with { PirateWorld = null } } }, true);
        Assert.Null(engine.CaptureSaveState().GameState.DemoCampaign!.PirateWorld);
        Assert.Null(engine.CaptureSnapshot().DemoCampaign!.Encounters);
    }

    [Fact]
    public void MalformedNestedSensor_RejectsLoadWithoutChangingWorld()
    {
        using var engine = DemoEp0033Us0001Tk0003Tests.Demo();
        var save = engine.CaptureSaveState();
        var campaign = save.GameState.DemoCampaign!;
        var world = campaign.PirateWorld!;
        var group = world.Groups[0];
        var member = group.Members[0];
        var malformed = member with { Sensor = member.Sensor with { PowerState = "invalid" } };
        var corrupt = world with { Groups = world.Groups.SetItem(0, group with { Members = group.Members.SetItem(0, malformed) }) };
        string before = ScenarioLoader.Serialize(save);
        Assert.Throws<ScenarioException>(() => engine.LoadScenario(save with
        { GameState = save.GameState with { DemoCampaign = campaign with { PirateWorld = corrupt } } }, true));
        Assert.Equal(before, ScenarioLoader.Serialize(engine.CaptureSaveState()));
    }

    [Fact]
    public void PublicObservation_RequiresActualPlayerCoverage()
    {
        using var engine = DemoEp0033Us0001Tk0003Tests.Demo();
        var save = engine.CaptureSaveState();
        var group = save.GameState.DemoCampaign!.PirateWorld!.Groups[0];
        engine.LoadScenario(save with
        {
            GameState = save.GameState with
            {
                SpaceObjects = save.GameState.SpaceObjects.Select(s => s.ObjectId != save.GameState.PlayerShipObjectId ? s : s with
                { PositionX = group.X, PositionY = group.Y + 2200, SpeedMps = 0, Orbit = null, IsDocked = false, DockedStationObjectId = null }).ToArray()
            }
        }, true);
        var observation = engine.CaptureSnapshot().DemoCampaign!.Encounters!.Groups.Single(g => g.Id == group.GroupId);
        Assert.True(observation.IsDetected);
        Assert.Equal(group.X, observation.X);
        Assert.Equal(group.Members.Select(m => m.CarrierId), observation.Members.Select(m => m.CarrierId));
    }
}
