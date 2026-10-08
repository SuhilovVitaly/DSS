using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Engine.Scenario;

namespace DeepSpaceSaga.Engine.Tests;

public sealed class KnownMapProjectionTests
{
    [Fact]
    public void AllGeneratedGeographyKnown()
    {
        // SolarSystemMap refines spatial knowledge only; legacy §§38–40 masking
        // and detailed knowledge are not replaced by successful scans or visits.
        var source = SeededWorldBootstrapTests.Scenario();
        source = source with { GameState = source.GameState with { TradingMapGeneration = null, SpaceObjects = source.GameState.SpaceObjects.Select(o => o with { IsKnown = false }).ToArray() } };
        using var engine = new SimulationEngine(SeededWorldBootstrapTests.Registry());
        engine.LoadScenario(source, generation: GenerationInputSchemaTests.Config());
        var snapshot = engine.CaptureSnapshot();
        Assert.NotNull(snapshot.SolarSystemMap);
        Assert.All(snapshot.Objects, o => Assert.Equal(o.ObjectType, o.RenderObjectType));
        Assert.DoesNotContain(snapshot.Objects, o => o.RenderObjectType == SpaceObjectType.UnknownSpaceObject);
        var save = engine.CaptureSaveState();
        foreach (var obj in source.GameState.SpaceObjects)
            Assert.False(save.GameState.SpaceObjects.Single(o => o.ObjectId == obj.ObjectId).IsKnown);
        Assert.All(snapshot.Objects.Where(o => o.ObjectId != snapshot.PlayerShipObjectId), o =>
        {
            if (source.GameState.SpaceObjects.Any(s => s.ObjectId == o.ObjectId))
            {
                Assert.Null(o.CaptainDisplayName);
                Assert.Null(o.HullCombat);
            }
        });
    }

    [Fact]
    public void RemoteMarketRemainsUnavailable()
    {
        using var engine = SimulationEngine.CreateFromSettingsFile(Path.Combine(SeededWorldBootstrapTests.ClientRoot, "Settings.json"));
        var before = engine.CaptureSaveState();
        var snapshot = engine.CaptureSnapshot();
        Assert.Contains(snapshot.Objects, o => o.RenderObjectType == SpaceObjectType.Station);
        Assert.Null(snapshot.DockedStationTrade);
        var quote = engine.GetTradeQuote(new("remote", snapshot.PlayerShipObjectId!, "MOD-PLAYER-CARGO-01", TradeCommandTypes.Buy, "item.ice", 1));
        Assert.NotNull(quote.DisabledReason);
        Assert.All(snapshot.Objects.Where(o => o.Survey is not null), o =>
        {
            Assert.True(o.Survey!.CompositionKnown);
            Assert.Equal(1000, o.Survey.Resources.Sum(r => r.Permille));
        });
        Assert.Equal(before.GameState.StationResourceFields, engine.CaptureSaveState().GameState.StationResourceFields);
    }

    [Fact]
    public void LegacyKnowledgeStillGated()
    {
        var source = SeededWorldBootstrapTests.Scenario();
        source = source with { GameState = source.GameState with { TradingMapGeneration = null, SpaceObjects = source.GameState.SpaceObjects.Select(o => o with { IsKnown = false }).ToArray() } };
        using var engine = new SimulationEngine(SeededWorldBootstrapTests.Registry());
        engine.LoadScenario(source);
        Assert.Null(engine.CaptureSnapshot().SolarSystemMap);
        Assert.All(engine.CaptureSnapshot().Objects.Where(o => o.ObjectId != source.GameState.PlayerShipObjectId && source.GameState.SpaceObjects.Any(s => s.ObjectId == o.ObjectId)), o =>
        {
            Assert.Null(o.ObjectType);
            Assert.Equal(SpaceObjectType.UnknownSpaceObject, o.RenderObjectType);
            Assert.Null(o.DisplayName);
        });
    }
}

