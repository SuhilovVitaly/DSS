using System.Text.Json;
using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Engine.Scenario;
using DeepSpaceSaga.Motion;

namespace DeepSpaceSaga.Engine.Tests;

public sealed class MovingTerritoryDataTests
{
    [Fact]
    public void TerritoriesFollowBasesAtAllSpeeds()
    {
        using var engine = SeededAiBasesTests.Create();
        var initial = engine.CaptureSnapshot();
        Assert.Equal(initial.AiMap!.Bases.Length, initial.AiMap.Territories.Length);
        foreach (var speed in Enum.GetValues<SimulationSpeed>())
        {
            var snapshot = engine.CaptureSnapshotForTests(86400000, speed, 288000);
            Assert.Equal(JsonSerializer.Serialize(initial.AiMap), JsonSerializer.Serialize(snapshot.AiMap));
            foreach (var t in snapshot.AiMap!.Territories)
            {
                Assert.Equal(200, t.DefenceRadiusKm); Assert.Equal(1000, t.PatrolRadiusKm);
                var obj = initial.Objects.Single(o => o.ObjectId == t.BaseObjectId);
                var expected = OrbitalMotionMath.At(obj, obj.Orbit!, 288000);
                var actual = snapshot.Objects.Single(o => o.ObjectId == t.BaseObjectId);
                Assert.Equal(expected.X, actual.X, 6); Assert.Equal(expected.Y, actual.Y, 6);
            }
        }
    }

    [Fact]
    public void StartGraphSegmentsStayOutside()
    {
        using var engine = SeededAiBasesTests.Create();
        var state = engine.CaptureSaveState().GameState;
        Assert.Null(AiBaseGenerator.StartNetworkOverlap(state, 0));
        var home = state.ClusterMap!.Clusters.First(c => c.Id == state.ClusterMap.StartClusterId);
        var link = state.ClusterMap.Links.First(l => home.StationIds.Contains(l.FromStationId) && home.StationIds.Contains(l.ToStationId));
        var a = state.SpaceObjects.Single(o => o.ObjectId == link.FromStationId);
        var b = state.SpaceObjects.Single(o => o.ObjectId == link.ToStationId);
        string ai = state.AiMap!.Bases[0].ObjectId;
        var crafted = state with
        {
            AiMap = state.AiMap with { Territories = [new("area", ai, 0.001, 0.01)] },
            SpaceObjects = state.SpaceObjects.Select(o => o.ObjectId != ai ? o : o with
            { Orbit = null, PositionX = (a.PositionX + b.PositionX) / 2, PositionY = (a.PositionY + b.PositionY) / 2 }).ToArray()
        };
        Assert.Contains("link=", AiBaseGenerator.StartNetworkOverlap(crafted, 0));
        string before = ScenarioLoader.Serialize(engine.CaptureSaveState());
        Assert.Throws<ScenarioException>(() => engine.LoadScenario(SeededWorldBootstrapTests.Scenario("MarketProfiles"), generation:
            SeededAiBasesTests.Config() with { Ai = new(2, 2, 1e12, 1e12, 2) }));
        Assert.Equal(before, ScenarioLoader.Serialize(engine.CaptureSaveState()));
    }

    [Fact]
    public void FlyingThroughTerritoryHasNoGameplayEffect()
    {
        using var withAreas = SeededAiBasesTests.Create();
        var save = withAreas.CaptureSaveState();
        var center = save.GameState.SpaceObjects.Single(o => o.ObjectId == save.GameState.AiMap!.Bases[0].ObjectId);
        var prepared = save with
        {
            GameState = save.GameState with
            {
                SpaceObjects = save.GameState.SpaceObjects.Select(o => o.ObjectId != save.GameState.PlayerShipObjectId ? o : o with
                { PositionX = center.PositionX, PositionY = center.PositionY, SpeedMps = 1000, DirectionDegrees = 90 }).ToArray()
            }
        };
        withAreas.LoadScenario(prepared, isSave: true);
        using var control = new SimulationEngine(SeededWorldBootstrapTests.Registry());
        control.LoadScenario(prepared with { GameState = prepared.GameState with { AiMap = prepared.GameState.AiMap! with { Territories = [] } } }, isSave: true);
        foreach (long time in new[] { 0L, 500, 1000 })
        {
            var a = withAreas.CaptureSnapshotForTests(time * 300, SimulationSpeed.Speed1, time);
            var b = control.CaptureSnapshotForTests(time * 300, SimulationSpeed.Speed1, time);
            Assert.Equal(JsonSerializer.Serialize(a with { AiMap = null }), JsonSerializer.Serialize(b with { AiMap = null }));
        }
    }
}
