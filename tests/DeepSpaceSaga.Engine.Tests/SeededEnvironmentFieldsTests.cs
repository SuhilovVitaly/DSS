using System.Collections.Immutable;
using System.Text.Json;
using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Engine.Content;
using DeepSpaceSaga.Engine.Scenario;
using DeepSpaceSaga.Motion;

namespace DeepSpaceSaga.Engine.Tests;

public sealed class SeededEnvironmentFieldsTests
{
    internal static SolarSystemGenerationConfig Config() => SeededAiBasesTests.Config() with
    { Environment = new(1, 2, 2, 5000, 100, 250, 0.5) };

    [Fact]
    public void FieldsSeedAndBindingAtAllSpeeds()
    {
        using var engine = SeededAiBasesTests.Create(config: Config());
        using var repeat = SeededAiBasesTests.Create(config: Config());
        using var control = SeededAiBasesTests.Create(config: Config() with { Environment = null });
        var initial = engine.CaptureSnapshot();
        Assert.Equal(5, initial.AiMap!.Fields.Length);
        Assert.Equal(JsonSerializer.Serialize(initial.AiMap), JsonSerializer.Serialize(repeat.CaptureSnapshot().AiMap));
        Assert.Equal(JsonSerializer.Serialize(control.CaptureSnapshot()), JsonSerializer.Serialize(initial with { AiMap = initial.AiMap with { Fields = [] } }));
        Assert.Equal(new[] { "Orbit", "Parent", "Sun" }, initial.AiMap.Fields.Select(f => f.AnchorKind).Distinct().Order().ToArray());
        var dust = initial.AiMap.Fields.Where(f => f.Kind == "Dust");
        Assert.All(dust, f => Assert.Contains(initial.SolarSystemMap!.Belts, b => f.InnerRadius >= b.InnerRadius && f.OuterRadius <= b.OuterRadius));
        foreach (long day in new long[] { 0, 1, 365 })
            foreach (var speed in Enum.GetValues<SimulationSpeed>())
            {
                long motion = day * 288000;
                var snapshot = engine.CaptureSnapshotForTests(day * 86400000, speed, motion);
                Assert.Equal(JsonSerializer.Serialize(initial.AiMap.Fields), JsonSerializer.Serialize(snapshot.AiMap!.Fields));
                foreach (var field in snapshot.AiMap.Fields.Where(f => f.AnchorKind == "Parent"))
                {
                    var anchor = initial.Objects.Single(o => o.ObjectId == field.ParentObjectId);
                    var expected = OrbitalMotionMath.At(anchor, anchor.Orbit!, motion);
                    var actual = snapshot.Objects.Single(o => o.ObjectId == field.ParentObjectId);
                    Assert.Equal(expected.X, actual.X, 6); Assert.Equal(expected.Y, actual.Y, 6);
                }
            }
    }

    [Fact]
    public void InvalidFieldGeometryIsAtomic()
    {
        using var engine = SeededAiBasesTests.Create(config: Config());
        var save = engine.CaptureSaveState(); var fields = save.GameState.AiMap!.Fields;
        var field = fields[0];
        string before = ScenarioLoader.Serialize(save);
        foreach (var bad in new[]
        {
            field with { Intensity = double.NaN }, field with { Intensity = 2 }, field with { InnerRadius = -1 },
            field with { OuterRadius = field.InnerRadius }, field with { SweepDegrees = 0 }, field with { SweepDegrees = 361 },
            field with { StartAngleDegrees = double.PositiveInfinity }, field with { OffsetX = double.NaN }, field with { Kind = "Fuel" },
            field with { AnchorKind = "Parent", ParentObjectId = "missing" },
            field with { AnchorKind = "Parent", ParentObjectId = field.Id },
            field with { AnchorKind = "Orbit", ParentObjectId = null, Orbit = null },
            field with { Id = fields[1].Id.ToLowerInvariant() }
        })
        {
            Assert.Throws<ScenarioException>(() => engine.LoadScenario(save with
            { GameState = save.GameState with { AiMap = save.GameState.AiMap with { Fields = fields.SetItem(0, bad) } } }, true));
            Assert.Equal(before, ScenarioLoader.Serialize(engine.CaptureSaveState()));
        }
        foreach (var bad in new[] { Config().Environment! with { DustCount = 65 }, Config().Environment! with { Intensity = -0.1 }, Config().Environment! with { DebrisRadiusKm = double.PositiveInfinity } })
            Assert.Throws<ContentException>(() => engine.LoadScenario(SeededWorldBootstrapTests.Scenario("MarketProfiles"), generation: Config() with { Environment = bad }));
        Assert.Equal(before, ScenarioLoader.Serialize(engine.CaptureSaveState()));
    }

    [Fact]
    public void AllFieldKindsHaveNoEffects()
    {
        using var withFields = SeededAiBasesTests.Create(config: Config());
        var save = withFields.CaptureSaveState();
        // Every kind covers the whole journey; decorations cannot mask a gameplay effect.
        save = save with
        {
            GameState = save.GameState with
            {
                SpaceObjects = save.GameState.SpaceObjects.Select(o => o.ObjectId != save.GameState.PlayerShipObjectId ? o : o with
                { SpeedMps = 1000, DirectionDegrees = 90, MovementType = "Linear", IsDocked = false, DockedStationObjectId = null }).ToArray(),
                AiMap = save.GameState.AiMap! with
                { Fields = save.GameState.AiMap!.Fields.Select(f => f with { InnerRadius = 0, OuterRadius = 1e10, SweepDegrees = 360 }).ToImmutableArray() }
            }
        };
        withFields.LoadScenario(save, true);
        using var control = new SimulationEngine(SeededWorldBootstrapTests.Registry());
        control.LoadScenario(save with { GameState = save.GameState with { AiMap = save.GameState.AiMap with { Fields = [] } } }, true);
        bool observedMarketEvent = false;
        double initialX = save.GameState.SpaceObjects.Single(o => o.ObjectId == save.GameState.PlayerShipObjectId).PositionX;
        for (int hour = 0; hour <= 48; hour++)
        {
            long calendar = hour * 3600000L, motion = calendar / 300;
            var a = withFields.CaptureSnapshotForTests(calendar, SimulationSpeed.Speed1, motion);
            var b = control.CaptureSnapshotForTests(calendar, SimulationSpeed.Speed1, motion);
            Assert.Equal(JsonSerializer.Serialize(a with { AiMap = null }), JsonSerializer.Serialize(b with { AiMap = null }));
            var sa = withFields.CaptureSaveStateForTests(calendar, SimulationSpeed.Speed1, motion);
            var sb = control.CaptureSaveStateForTests(calendar, SimulationSpeed.Speed1, motion);
            Assert.Equal(ScenarioLoader.Serialize(sa with { GameState = sa.GameState with { AiMap = null } }),
                ScenarioLoader.Serialize(sb with { GameState = sb.GameState with { AiMap = null } }));
            observedMarketEvent |= sa.GameState.SpaceObjects.Any(o => o.Events?.Any(e => e.DefinitionId is not null) == true);
            if (hour == 48) Assert.True(a.Objects.Single(o => o.ObjectId == a.PlayerShipObjectId).X > initialX + 1000);
        }
        Assert.True(observedMarketEvent);
    }
}
