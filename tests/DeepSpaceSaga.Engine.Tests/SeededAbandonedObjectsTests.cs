using System.Text.Json;
using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Engine.Content;
using DeepSpaceSaga.Engine.Scenario;

namespace DeepSpaceSaga.Engine.Tests;

public sealed class SeededAbandonedObjectsTests
{
    internal static SolarSystemGenerationConfig Config() => SeededEnvironmentFieldsTests.Config() with
    { PoiTemplates = [new("relay", "Ретранслятор", "Неактивный узел связи."), new("platform", "Платформа", "Нет действий.")] };

    internal static SimulationEngine Create(SolarSystemGenerationConfig? config = null, bool resources = true)
    {
        var engine = new SimulationEngine(SeededWorldBootstrapTests.Registry());
        if (resources) engine.ConfigureStationResourceFields(JsonSerializer.Deserialize<StationResourceFieldConfig>(
            File.ReadAllText(Path.Combine(SeededWorldBootstrapTests.ClientRoot, "Data/World/station-resource-fields.json")))!);
        var source = SeededWorldBootstrapTests.Scenario("MarketProfiles");
        engine.LoadScenario(source with { GameState = source.GameState with { MasterSeed = 42 } }, generation: config ?? Config());
        return engine;
    }

    [Fact]
    public void PoiSeedIdentityAndText()
    {
        using var a = Create(); using var b = Create();
        var map = a.CaptureSnapshot().AiMap!;
        Assert.Equal(JsonSerializer.Serialize(map), JsonSerializer.Serialize(b.CaptureSnapshot().AiMap));
        Assert.Equal(new[] { "SYS-POI-1", "SYS-POI-2" }, map.PointsOfInterest.Select(p => p.ObjectId));
        Assert.Equal("Платформа", map.PointsOfInterest[0].Name); Assert.Equal("Неактивный узел связи.", map.PointsOfInterest[1].Description);
        var state = a.CaptureSaveState().GameState;
        Assert.All(map.PointsOfInterest, p =>
        {
            Assert.NotNull(p.ParentObjectId); Assert.Null(p.Orbit);
            Assert.Contains(state.StationResourceFields!.Asteroids, r => r.ObjectId == p.ParentObjectId);
            Assert.InRange(double.Hypot(p.OffsetX, p.OffsetY), 25, 125);
            Assert.DoesNotContain(state.SpaceObjects, o => o.ObjectId == p.ObjectId);
        });
        using var withoutResources = Create(resources: false);
        var snapshot = withoutResources.CaptureSnapshot();
        Assert.All(snapshot.AiMap!.PointsOfInterest, p =>
        {
            Assert.Null(p.ParentObjectId); Assert.NotNull(p.Orbit);
            Assert.Contains(snapshot.SolarSystemMap!.Belts, belt => p.Orbit.SemiMajorAxis >= belt.InnerRadius && p.Orbit.SemiMajorAxis <= belt.OuterRadius);
        });
    }

    [Fact]
    public void ResourcesRetainCanonicalIdentity()
    {
        using var a = Create(); using var b = Create(Config() with { PoiTemplates = null });
        var state = a.CaptureSaveState().GameState; var baseline = b.CaptureSaveState().GameState;
        Assert.NotEmpty(state.StationResourceFields!.Asteroids); Assert.NotEmpty(state.ClusterMap!.ResourceBindings);
        Assert.Equal(JsonSerializer.Serialize(baseline.StationResourceFields), JsonSerializer.Serialize(state.StationResourceFields));
        Assert.Equal(JsonSerializer.Serialize(baseline.ClusterMap), JsonSerializer.Serialize(state.ClusterMap));
        Assert.Equal(JsonSerializer.Serialize(baseline), JsonSerializer.Serialize(state with { AiMap = state.AiMap! with { PointsOfInterest = [] } }));
    }

    [Fact]
    public void PoiVisitCreatesNoGameplayEvent()
    {
        using var a = Create(); var save = a.CaptureSaveState(); var poi = save.GameState.AiMap!.PointsOfInterest[0];
        var parent = save.GameState.SpaceObjects.Single(o => o.ObjectId == poi.ParentObjectId);
        save = save with
        {
            GameState = save.GameState with
            {
                SpaceObjects = save.GameState.SpaceObjects.Select(o => o.ObjectId != save.GameState.PlayerShipObjectId ? o : o with
                { PositionX = parent.PositionX + poi.OffsetX, PositionY = parent.PositionY + poi.OffsetY, IsDocked = false, DockedStationObjectId = null }).ToArray()
            }
        };
        a.LoadScenario(save, true); using var b = new SimulationEngine(SeededWorldBootstrapTests.Registry());
        b.LoadScenario(save with { GameState = save.GameState with { AiMap = save.GameState.AiMap with { PointsOfInterest = [] } } }, true);
        a.SetObjectInteractionState(poi.ObjectId, poi.ObjectId);
        foreach (long calendar in new long[] { 0, 3600000, 86400000 })
        {
            var sa = a.CaptureSnapshotForTests(calendar, SimulationSpeed.Speed1, calendar / 300);
            var sb = b.CaptureSnapshotForTests(calendar, SimulationSpeed.Speed1, calendar / 300);
            Assert.Null(sa.SelectedObjectId); Assert.Null(sa.ActiveObjectId);
            Assert.Equal(JsonSerializer.Serialize(sa with { AiMap = null }), JsonSerializer.Serialize(sb with { AiMap = null }));
            var wa = a.CaptureSaveStateForTests(calendar, SimulationSpeed.Speed1, calendar / 300);
            var wb = b.CaptureSaveStateForTests(calendar, SimulationSpeed.Speed1, calendar / 300);
            Assert.Equal(ScenarioLoader.Serialize(wa with { GameState = wa.GameState with { AiMap = null } }),
                ScenarioLoader.Serialize(wb with { GameState = wb.GameState with { AiMap = null } }));
        }
    }

    [Fact]
    public void MalformedPoiDoesNotReplaceWorld()
    {
        using var engine = Create(); var save = engine.CaptureSaveState(); string before = ScenarioLoader.Serialize(save);
        var rows = save.GameState.AiMap!.PointsOfInterest; var poi = rows[0];
        foreach (var bad in new[] { poi with { ParentObjectId = poi.ObjectId }, poi with { ParentObjectId = null },
            poi with { ObjectId = rows[1].ObjectId.ToLowerInvariant() }, poi with { ObjectId = save.GameState.AiMap.Fields[0].Id },
            poi with { Name = " " }, poi with { Description = "" }, poi with { OffsetX = double.NaN } })
        {
            Assert.Throws<ScenarioException>(() => engine.LoadScenario(save with
            {
                GameState = save.GameState with
                { AiMap = save.GameState.AiMap with { PointsOfInterest = rows.SetItem(0, bad) } }
            }, true));
            Assert.Equal(before, ScenarioLoader.Serialize(engine.CaptureSaveState()));
        }
        Assert.Throws<ContentException>(() => SolarSystemGeneration.ValidateConfig(Config() with
        { PoiTemplates = [new("same", "A", "A"), new("SAME", "B", "B")] }));
    }
}
