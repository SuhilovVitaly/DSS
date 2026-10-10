using System.Text.Json;
using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Engine.Scenario;
using static DeepSpaceSaga.Engine.Scenario.AiTradePlacementValidator;

namespace DeepSpaceSaga.Engine.Tests;

public sealed class TemporalPlacementValidationTests
{
    private static ScenarioFile World(OrbitalElements baseOrbit, OrbitalElements? stationOrbit, double x = 0, double y = -1000)
    {
        SpaceObjectData Obj(string id, string type, OrbitalElements? orbit, double px, double py) =>
            new(id, type, "Permanent", id, px, py, 0, 0, orbit is null ? "Stationary" : "Orbital", null, null, null, Orbit: orbit);
        return new(new("test", "test"), new(0, "Speed0", "ship", null,
            [Obj("ship", "PlayerShip", null, 3000, 3000), Obj("base", "Station", baseOrbit, 0, 0), Obj("station", "Station", stationOrbit, x, y)],
            SolarSystem: new(1, 1, 10000, [], [], []),
            ClusterMap: new(1, "home", [new("home", "Home", "belt", ["station"], "Default")], [], []),
            AiMap: new(1, [new("base", "Orbital", "Ai", null, baseOrbit, 0, 0)], [new("t", "base", 1, 2)])));
    }

    [Fact]
    public void CriticalApproachBetweenSamplesIsDetected()
    {
        var orbit = new OrbitalElements(1000, 1000, 8 * Day, 180, 0, 0, "clockwise");
        var world = World(orbit, orbit with { OrbitalPeriodMs = 8000 * Day, InitialPhase = 0 });
        var result = Validate(world, world.GameState.ClusterMap!, world.GameState.AiMap!, DefaultHorizon);
        Assert.False(result.IsValid);
        var failure = Assert.Single(result.Violations);
        Assert.InRange(failure.EpochGameTimeMs, Day + 1, 7 * Day - 1);
        Assert.Equal("base", failure.BaseId); Assert.Equal("local_route_overlap", failure.Reason);
        Assert.Contains(failure.EpochGameTimeMs, result.CriticalEpochs);

        // Static anchor has no relative-orbit critical epochs: interval proof must detect the quarter-day crossing.
        var interval = World(orbit with { OrbitalPeriodMs = Day, InitialPhase = 0 }, null, 1000, 0);
        var intervalResult = Validate(interval, interval.GameState.ClusterMap!, interval.GameState.AiMap!, Day);
        Assert.False(intervalResult.IsValid);
        Assert.Equal("interval_overlap", Assert.Single(intervalResult.Violations).Reason);
    }

    [Fact]
    public void DetourGraphConnectsClusters()
    {
        Point[] access = [new(-800, 0), new(800, 0)];
        Assert.True(DetourConnected(access, [new(new(-100, 0), 250), new(new(100, 0), 250)], 1000, 10));
        Assert.False(DetourConnected(access, [new(new(0, -400), 600), new(new(0, 400), 600)], 1000, 10));
        Assert.False(DetourConnected([new(-250, 0), new(800, 0)], [new(new(0, 0), 250)], 1000, 10));
        Assert.False(DetourConnected([new(0, 10), new(800, 0)], [], 1000, 10));
        Assert.False(DetourConnected([new(1000, 0), new(800, 0)], [], 1000, 10));
        Assert.True(DetourConnected(access, [], 1000, 400));
    }

    [Fact]
    public void RetryIsBoundedAndDoesNotMoveHumans()
    {
        using var engine = SeededAiBasesTests.Create();
        string before = ScenarioLoader.Serialize(engine.CaptureSaveState());
        var diagnostic = engine.CaptureAiPlacementValidation();
        Assert.NotNull(diagnostic); Assert.True(diagnostic.IsValid);
        Assert.Equal(DefaultHorizon, diagnostic.HorizonGameTimeMs);
        Assert.All(diagnostic.Connectivity, c => Assert.Equal(1, c.Components));
        foreach (long day in new long[] { 0, 1, 7, 30, 100, 365 })
            Assert.Contains(diagnostic.Checks, c => c.EpochGameTimeMs == day * Day);
        using var repeat = SeededAiBasesTests.Create();
        Assert.Equal(JsonSerializer.Serialize(diagnostic), JsonSerializer.Serialize(repeat.CaptureAiPlacementValidation()));
        var error = Assert.Throws<ScenarioException>(() => engine.LoadScenario(SeededWorldBootstrapTests.Scenario("MarketProfiles"),
            generation: SeededAiBasesTests.Config() with { Ai = new(2, 2, 1e12, 1e12, 3) }));
        Assert.Contains("attempts=3", error.Message);
        Assert.Equal(before, ScenarioLoader.Serialize(engine.CaptureSaveState()));
        Assert.Same(diagnostic, engine.CaptureAiPlacementValidation());
        using var humanOnly = SeededAiBasesTests.Create(config: SeededAiBasesTests.Config() with { Ai = null });
        foreach (var human in humanOnly.CaptureSaveState().GameState.SpaceObjects)
            Assert.Equal(JsonSerializer.Serialize(human), JsonSerializer.Serialize(engine.CaptureSaveState().GameState.SpaceObjects.Single(o => o.ObjectId == human.ObjectId)));
        engine.LoadScenario(engine.CaptureSaveState(), isSave: true);
        Assert.Null(engine.CaptureAiPlacementValidation());
    }
}
