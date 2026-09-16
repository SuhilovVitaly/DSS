using DeepSpaceSaga.Contracts;

namespace DeepSpaceSaga.Engine.Tests;

public class StationTravelTests
{
    internal static SimulationEngine DockedEngine(long time = 0)
    {
        var engine = DockCommandTests.CreateEngine();
        var save = engine.CaptureSaveState();
        engine.LoadScenario(save with { GameState = save.GameState with {
            GameTimeMs = time,
            SpaceObjects = save.GameState.SpaceObjects.Select(o => o.ObjectId == "SPC-0001"
                ? o with { IsDocked = true, DockedStationObjectId = "STATION-01" } : o).ToArray()
        }});
        return engine;
    }

    [Fact]
    public void Travel_and_return_each_cost_one_hour_but_retries_and_current_district_are_free()
    {
        using var engine = DockedEngine();
        var command = new StationTravelCommand("one", StationDistrict.Market);
        var first = engine.TravelStation(command);
        Assert.True(first.Accepted);
        Assert.Equal(GameCalendar.HourMs, first.Snapshot.GameTimeMs);
        Assert.Equal(StationDistrict.Market, first.Snapshot.CurrentStationDistrict);
        Assert.Equal(GameCalendar.HourMs, engine.TravelStation(command).Snapshot.GameTimeMs);
        Assert.False(engine.TravelStation(new("two", StationDistrict.Market)).Accepted);
        var back = engine.TravelStation(new("back", StationDistrict.Dock));
        Assert.Equal(2 * GameCalendar.HourMs, back.Snapshot.GameTimeMs);
        Assert.Equal(SimulationSpeed.Speed0, back.Snapshot.CurrentSpeed);
        Assert.Equal(back.Snapshot.GameTimeMs, engine.CaptureSaveState().GameState.GameTimeMs);
    }

    [Fact]
    public void Travel_is_rejected_outside_docked_paused_session()
    {
        using var engine = DockCommandTests.CreateEngine();
        Assert.False(engine.TravelStation(new("no", StationDistrict.Market)).Accepted);
        Assert.Equal(0, engine.CaptureSnapshot().GameTimeMs);
    }
}
