using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Engine.Scenario;
using DeepSpaceSaga.Motion;

namespace DeepSpaceSaga.Engine.Tests;

public sealed class AuthoritativeOrbitRuntimeTests
{
    internal static ScenarioFile Scene(bool docked = false)
    {
        var orbit = new OrbitalElements(720000, 720000, 1200000000, 17, 0.000001, 0, "clockwise");
        return new(new("orbits", "Orbits"), new(0, "Speed0", "ship", null,
            [new("sun", "Sun", "Permanent", "Sun", 0, 0, 0, 0, "Stationary", null, null, null, IsKnown: true),
             new("station", "Station", "Permanent", "Station", 0, -720000, 0, 0, "Orbital", null, null, null, IsKnown: true, Orbit: orbit),
             new("ship", "PlayerShip", "Permanent", "Ship", 10, 20, 4000, 90, "Linear", null, null, null,
                IsDocked: docked, DockedStationObjectId: docked ? "station" : null)], MasterSeed: 1, SimulationTimeMs: 0));
    }

    [Theory]
    [InlineData(SimulationSpeed.Speed0)]
    [InlineData(SimulationSpeed.Speed1)]
    [InlineData(SimulationSpeed.Speed2)]
    [InlineData(SimulationSpeed.Speed3)]
    [InlineData(SimulationSpeed.Speed4)]
    public void AllSpeedsMatchMotion(SimulationSpeed speed)
    {
        using var engine = new SimulationEngine();
        engine.LoadScenario(Scene());
        long time = (long)speed * 1000;
        var snapshot = engine.CaptureSnapshotForTests(time * 300, speed, time);
        var actual = snapshot.Objects.Single(o => o.ObjectId == "station");
        var expected = OrbitalMotionMath.At(actual, actual.Orbit!, time);
        Assert.Equal(expected.X, actual.X); Assert.Equal(expected.Y, actual.Y);
        Assert.Equal(expected.SpeedKmS, actual.SpeedKmS);
        Assert.Equal(expected.Direction, actual.Direction);
        Assert.Equal(time, actual.OrbitSampleSimulationTimeMs);
        var afterCalendarHour = engine.CaptureSnapshotForTests(time * 300 + GameCalendar.HourMs, speed, time);
        Assert.Equal(actual, afterCalendarHour.Objects.Single(o => o.ObjectId == "station"));
        var sun = snapshot.Objects.Single(o => o.ObjectId == "sun");
        Assert.Equal(0, sun.X); Assert.Equal(0, sun.Y); Assert.Equal(0, sun.SpeedKmS);
    }

    [Fact]
    public void DockedOffsetAndFreeFlight()
    {
        using var docked = new SimulationEngine();
        docked.LoadScenario(Scene(true));
        foreach (long time in new long[] { 0, 1, 1000, 12000, 1000000 })
        {
            var snapshot = docked.CaptureSnapshotForTests(time * 300, SimulationSpeed.Speed4, time);
            var station = snapshot.Objects.Single(o => o.ObjectId == "station");
            var ship = snapshot.Objects.Single(o => o.ObjectId == "ship");
            Assert.Equal(station.X + 1, ship.X); Assert.Equal(station.Y + 1, ship.Y);
            Assert.Equal(station.SpeedKmS, ship.SpeedKmS); Assert.Equal(station.Direction, ship.Direction);
            Assert.Equal(station.Orbit, ship.Orbit);
        }
        using var free = new SimulationEngine();
        free.LoadScenario(Scene());
        var moved = free.CaptureSnapshotForTests(300000, SimulationSpeed.Speed1, 1000).Objects.Single(o => o.ObjectId == "ship");
        Assert.Equal(50, moved.X, 8); Assert.Equal(20, moved.Y, 8);
        Assert.Null(moved.Orbit);
    }

    [Fact]
    public void SplitAdvanceEqualsDirect()
    {
        using var direct = new SimulationEngine();
        using var split = new SimulationEngine();
        direct.LoadScenario(Scene(true)); split.LoadScenario(Scene(true));
        foreach (long time in new long[] { 1, 3, 10, 13, 99, 11111 })
            split.CaptureSnapshotForTests(time * 300, SimulationSpeed.Speed1, time);
        var a = direct.CaptureSnapshotForTests(3333300, SimulationSpeed.Speed1, 11111);
        var b = split.CaptureSnapshotForTests(3333300, SimulationSpeed.Speed1, 11111);
        Assert.Equal(a.Objects.ToArray(), b.Objects.ToArray());
        var save = split.CaptureSaveStateForTests(3333300, SimulationSpeed.Speed1, 11111);
        using var restored = new SimulationEngine();
        restored.LoadScenario(ScenarioLoader.LoadFromJson(ScenarioLoader.Serialize(save), true), isSave: true);
        var resumed = restored.CaptureSnapshotForTests(3600000, SimulationSpeed.Speed1, 12000);
        Assert.Equal(direct.CaptureSnapshotForTests(3600000, SimulationSpeed.Speed1, 12000).Objects.ToArray(), resumed.Objects.ToArray());
    }
}
