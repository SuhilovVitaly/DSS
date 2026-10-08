using System.Collections.Immutable;
using System.Text.Json;
using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Engine.Scenario;

namespace DeepSpaceSaga.Engine.Tests;

public sealed class MapEnvironmentSaveTests
{
    internal static void SameMap(ScenarioFile expected, ScenarioFile actual)
    {
        Assert.Equal(JsonSerializer.Serialize(expected.GameState.AiMap), JsonSerializer.Serialize(actual.GameState.AiMap));
        Assert.Equal(JsonSerializer.Serialize(expected.GameState.SolarSystem), JsonSerializer.Serialize(actual.GameState.SolarSystem));
        Assert.Equal(JsonSerializer.Serialize(expected.GameState.ClusterMap), JsonSerializer.Serialize(actual.GameState.ClusterMap));
    }

    [Fact]
    public void AiEnvironmentJsonContinuation()
    {
        using var continuous = new ClusterVoyageFixture(42, true, 1000000);
        Assert.NotEmpty(continuous.Snapshot.AiMap!.Fields); Assert.NotEmpty(continuous.Snapshot.AiMap.PointsOfInterest);
        continuous.Trade(TradeCommandTypes.Buy, continuous.OutboundItem);
        ClusterVoyageFixture? resumed = null;
        try
        {
            continuous.FlyTo(continuous.Destination, midpoint =>
            {
                resumed = midpoint.Reload(); SameMap(midpoint.Save(), resumed.Save()); ClusterSaveStateTests.SameFacts(midpoint, resumed);
                Assert.Equal(VoyagePhases.InTransit, resumed.Snapshot.Voyage!.Phase);
            });
            resumed!.FinishFlightTo(resumed.Destination); SameMap(continuous.Save(), resumed.Save()); ClusterSaveStateTests.SameFacts(continuous, resumed);
            continuous.Trade(TradeCommandTypes.Sell, continuous.OutboundItem); resumed.Trade(TradeCommandTypes.Sell, resumed.OutboundItem);
            continuous.Advance(288000); resumed.Advance(288000); ClusterSaveStateTests.SameFacts(continuous, resumed);
        }
        finally { resumed?.Dispose(); }
    }

    [Fact]
    public void ParentOverlapAndDecorationSurviveLoad()
    {
        using var original = SeededAbandonedObjectsTests.Create(); var source = original.CaptureSaveState();
        var map = source.GameState.AiMap!;
        // Deliberately overlapping legal informational circles must survive restoration.
        map = map with { Territories = map.Territories.Select(t => t with { PatrolRadiusKm = 10000000 }).ToImmutableArray() };
        source = source with { GameState = source.GameState with { AiMap = map } };
        using var continuous = new SimulationEngine(SeededWorldBootstrapTests.Registry()); continuous.LoadScenario(source, true);
        using var resumed = new SimulationEngine(SeededWorldBootstrapTests.Registry());
        // Invalid generation configuration would throw if save restoration tried to regenerate.
        resumed.LoadScenario(ScenarioLoader.LoadFromJson(ScenarioLoader.Serialize(source), true), true,
            SeededAbandonedObjectsTests.Config() with { GeneratorVersion = 999 });
        Assert.Contains(map.Fields, f => f.ParentObjectId is not null); Assert.Contains(map.Fields, f => f.Orbit is not null);
        foreach (long day in new[] { 0L, 1, 30, 365 })
        {
            long motion = day * 288000, calendar = day * 86400000;
            var a = continuous.CaptureSnapshotForTests(calendar, SimulationSpeed.Speed0, motion);
            var b = resumed.CaptureSnapshotForTests(calendar, SimulationSpeed.Speed0, motion);
            Assert.Equal(JsonSerializer.Serialize(a), JsonSerializer.Serialize(b));
            SameMap(continuous.CaptureSaveState(), resumed.CaptureSaveState());
        }
        using var legacy = SeededAbandonedObjectsTests.Create(SeededAbandonedObjectsTests.Config() with { Ai = null, Environment = null, PoiTemplates = null });
        var old = legacy.CaptureSaveState(); Assert.Null(old.GameState.AiMap);
        using var oldLoaded = new SimulationEngine(SeededWorldBootstrapTests.Registry()); oldLoaded.LoadScenario(old, true, SeededAbandonedObjectsTests.Config());
        Assert.Null(oldLoaded.CaptureSnapshot().AiMap); Assert.Equal(old.GameState.SpaceObjects.Count, oldLoaded.CaptureSnapshot().Objects.Length);
    }

    [Fact]
    public void InvalidAiSaveIsAtomic()
    {
        using var engine = SeededAbandonedObjectsTests.Create();
        engine.CaptureSnapshotForTests(86400000, SimulationSpeed.Speed0, 288000);
        var save = engine.CaptureSaveStateForTests(86400000, SimulationSpeed.Speed0, 288000); var map = save.GameState.AiMap!;
        using var valid = new SimulationEngine(SeededWorldBootstrapTests.Registry()); valid.LoadScenario(save, true);
        var territory = map.Territories[0]; var field = map.Fields[0]; var poi = map.PointsOfInterest[0]; var ai = map.Bases[0];
        string before = ScenarioLoader.Serialize(save);
        var bad = new List<AiMapEnvironmentSnapshot>
        {
            map with { RulesVersion = 999 }, map with { Bases = default },
            map with { Bases = map.Bases.Add(ai with { ObjectId = ai.ObjectId.ToLowerInvariant() }) },
            map with { Bases = map.Bases.SetItem(0, ai with { Owner = "Human" }) },
            map with { Bases = map.Bases.SetItem(0, ai with { ParentObjectId = ai.ObjectId }) },
            map with { Territories = map.Territories.SetItem(0, territory with { BaseObjectId = "missing" }) },
            map with { Territories = map.Territories.SetItem(0, territory with { DefenceRadiusKm = 0 }) },
            map with { Territories = map.Territories.SetItem(0, territory with { PatrolRadiusKm = double.PositiveInfinity }) },
            map with { Territories = map.Territories.SetItem(0, territory with { Id = save.GameState.ClusterMap!.Links[0].Id.ToLowerInvariant() }) },
            map with { Fields = map.Fields.SetItem(0, field with { AnchorKind = "Parent", ParentObjectId = "missing" }) },
            map with { Fields = map.Fields.SetItem(0, field with { AnchorKind = "Parent", ParentObjectId = poi.ObjectId }) },
            map with { Fields = map.Fields.SetItem(0, field with { InnerRadius = field.OuterRadius }) },
            map with { Fields = map.Fields.SetItem(0, field with { Intensity = 1.01 }) },
            map with { Fields = map.Fields.SetItem(0, field with { SweepDegrees = 0 }) },
            map with { Fields = map.Fields.SetItem(0, field with { Id = territory.Id.ToLowerInvariant() }) },
            map with { PointsOfInterest = map.PointsOfInterest.SetItem(0, poi with { ParentObjectId = poi.ObjectId, Orbit = null }) },
            map with { PointsOfInterest = map.PointsOfInterest.SetItem(0, poi with { ParentObjectId = null, Orbit = null }) },
            map with { PointsOfInterest = map.PointsOfInterest.SetItem(0, poi with { ObjectId = field.Id.ToLowerInvariant() }) },
            map with { PointsOfInterest = map.PointsOfInterest.SetItem(0, poi with { OffsetX = double.NaN }) }
        };
        foreach (var invalid in bad)
        {
            Assert.Throws<ScenarioException>(() => engine.LoadScenario(save with { GameState = save.GameState with { AiMap = invalid } }, true));
            Assert.Equal(before, ScenarioLoader.Serialize(engine.CaptureSaveStateForTests(86400000, SimulationSpeed.Speed0, 288000)));
        }
        Assert.Throws<ScenarioException>(() => engine.LoadScenario(save with { SaveFormatVersion = SaveFormat.CurrentSaveFormatVersion + 1 }, true));
        Assert.Equal(before, ScenarioLoader.Serialize(engine.CaptureSaveStateForTests(86400000, SimulationSpeed.Speed0, 288000)));
    }
}
