using System.Collections.Immutable;
using DeepSpaceSaga.Contracts;
using static DeepSpaceSaga.Engine.Tests.OrbitalSynchronizationTests;
using static DeepSpaceSaga.Engine.Tests.OrbitalDockingDepartureTests;

namespace DeepSpaceSaga.Engine.Tests;

public sealed class EpicSolarIntegrationTests
{
    [Theory]
    [InlineData(5000, false)]
    [InlineData(5000, true)]
    [InlineData(500000, false)]
    [InlineData(500000, true)]
    public void OrbitalDockingSurvivesPlayerInputDelay(long physicalDelayMs, bool restore)
    {
        using var engine = Create(42);
        long time = ApproachAndSynchronize(engine);
        // Five seconds of user input at x1/x100, without teleporting or editing state.
        var before = At(engine, time + physicalDelayMs);
        Assert.True(Distance(before) < 100);
        if (restore)
            engine.LoadScenario(engine.CaptureSaveStateForTests((time + physicalDelayMs) * 300, SimulationSpeed.Speed0, time + physicalDelayMs), isSave: true);
        var docked = Dock(engine, time + physicalDelayMs);
        Assert.True(Player(docked).IsDocked);
    }

    [Fact]
    public void RestoredSolarMapReferencesUseCanonicalObjectIds()
    {
        using var engine = Create(42);
        var save = engine.CaptureSaveState();
        var map = save.GameState.SolarSystem!;
        engine.LoadScenario(save with
        {
            GameState = save.GameState with
            {
                SolarSystem = map with
                {
                    Planets = map.Planets.Select(p => p with { ObjectId = p.ObjectId.ToLowerInvariant() }).ToImmutableArray(),
                    Orbits = map.Orbits.Select(o => o with { ObjectId = o.ObjectId.ToLowerInvariant() }).ToImmutableArray()
                }
            }
        }, isSave: true);
        var snapshot = engine.CaptureSnapshot();
        Assert.All(snapshot.SolarSystemMap!.Planets, p => Assert.Contains(snapshot.Objects, o => o.ObjectId == p.ObjectId));
        Assert.All(snapshot.SolarSystemMap.Orbits, p => Assert.Contains(snapshot.Objects, o => o.ObjectId == p.ObjectId));
    }
}
