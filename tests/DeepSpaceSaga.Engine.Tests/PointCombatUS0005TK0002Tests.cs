using System.Text.Json;
using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Engine.Scenario;
using static DeepSpaceSaga.Engine.Tests.PointCombatUS0003TK0002Tests;
using static DeepSpaceSaga.Engine.Tests.PointCombatUS0004TK0001Tests;

namespace DeepSpaceSaga.Engine.Tests;

public sealed class PointCombatUS0005TK0002Tests
{
    private const string Secret = "ENEMY-PRIVATE-";
    private sealed class Time { internal long Real; }
    private static ScenarioFile Scenario(bool known) => TorpedoImpactTests.Scenario(o => o.ObjectId != Target ? o : o with
    {
        IsKnown = known,
        PositionX = 400,
        PositionY = 0,
        Ai = null,
        Modules = o.Modules!.Select(m => m with
        {
            ModuleId = Secret + m.ModuleId,
            PowerState = m.ModuleTypeId == "module.engine.basic" ? "Off" : "On"
        }).ToArray()
    });

    private static SimulationEngine Create(Time time, ScenarioFile scenario)
    {
        var engine = new SimulationEngine(CounterRegistry(0, 0), clock: new SimulationClock(SimulationSpeed.Speed0, () => time.Real));
        engine.LoadScenario(scenario);
        engine.SetPresentationTimestampForTests(() => 0);
        engine.EnablePresentationLease();
        engine.SetSpeed(SimulationSpeed.Speed1);
        return engine;
    }

    private static void LaunchFlights(SimulationEngine engine)
    {
        Assert.Equal(CommandStartDisposition.Started,
            engine.TryStartHostilePointLaunch(Target, Secret + "MOD-PIRATE-TORPEDO-01", 731.234, 0, "slow", 0).Disposition);
        Assert.Equal(CommandStartDisposition.Started,
            engine.TryStartHostilePointLaunch(Target, Secret + "MOD-PIRATE-COUNTERMEASURE-01", 650, 0, "wide", 0, ProjectileKind.Countermissile).Disposition);
        engine.ReceiveCommand(Launch(x: 600, y: 0, profile: "slow"));
    }

    private static void AssertJson(AuthoritativeSnapshot snapshot, bool known = true, bool requireOwn = true)
    {
        var json = JsonSerializer.Serialize(snapshot);
        Assert.DoesNotContain(Secret, json);
        using var document = JsonDocument.Parse(json);
        var rows = document.RootElement.GetProperty("Objects").EnumerateArray().ToArray();
        var ownRows = rows.Where(r => r.TryGetProperty("OwnProjectile", out _)).ToArray();
        if (requireOwn) Assert.Single(ownRows);
        Assert.InRange(ownRows.Length, 0, 1);
        foreach (var ownRow in ownRows)
        {
            var own = ownRow.GetProperty("OwnProjectile");
            Assert.Equal(600, own.GetProperty("DetonationX").GetDouble());
            Assert.Equal(10000, own.GetProperty("DetonationMotionTimeMs").GetDouble());
            Assert.True(own.TryGetProperty("LauncherModuleId", out _));
        }
        var foreign = rows.Where(r => r.TryGetProperty("ObservedProjectile", out _)).ToArray();
        Assert.Equal(2, foreign.Length);
        foreach (var row in foreign)
        {
            Assert.False(row.TryGetProperty("OwnProjectile", out _));
            Assert.Equal(JsonValueKind.Null, row.GetProperty("Torpedo").ValueKind);
            Assert.Equal(JsonValueKind.Null, row.GetProperty("Countermeasure").ValueKind);
            var observed = row.GetProperty("ObservedProjectile");
            Assert.Equal(new[] { "BlastRadiusKm", "DirectionX", "DirectionY", "Kind", "ProfileId", "SpeedKmS", "X", "Y" },
                observed.EnumerateObject().Select(p => p.Name).Order(StringComparer.Ordinal));
            foreach (var hidden in new[] { "DetonationX", "DetonationY", "DetonationMotionTimeMs", "LaunchMotionTimeMs", "Damage", "ArmorPiercingPercent", "LauncherModuleId", "OwnerObjectId", "Reporter" })
                Assert.False(observed.TryGetProperty(hidden, out _), hidden);
        }
        var enemy = Assert.Single(rows, r => r.GetProperty("ObjectId").GetString() == Target);
        Assert.Empty(enemy.GetProperty("ModuleHealth").EnumerateArray());
        Assert.Equal(JsonValueKind.Null, enemy.GetProperty("Defense").ValueKind);
        Assert.Equal(known, enemy.TryGetProperty("EnemyStatus", out _));
        Assert.NotEmpty(rows.Single(r => r.GetProperty("ObjectId").GetString() == Player).GetProperty("ModuleHealth").EnumerateArray());
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Serialized_payload_does_not_leak_enemy_fields(bool known)
    {
        var time = new Time(); using var engine = Create(time, Scenario(known));
        LaunchFlights(engine);
        var first = engine.CapturePresentationUpdate(fullSnapshot: true);
        AssertJson(first.FullSnapshot!, known);
        time.Real = 2000;
        var next = engine.CapturePresentationUpdate(fullSnapshot: true);
        AssertJson(next.FullSnapshot!, known);
        Assert.NotEmpty(next.Frames);
        foreach (var frame in next.Frames) AssertJson(frame.State, known);
    }

    [Fact]
    public void Reload_reset_and_delta_have_same_privacy()
    {
        var time = new Time(); var scenario = Scenario(true);
        using var engine = Create(time, scenario);
        LaunchFlights(engine);
        var original = engine.CapturePresentationUpdate();
        AssertJson(original.FullSnapshot!);
        engine.LoadScenario(scenario);
        var bootstrap = engine.CapturePresentationUpdate();
        Assert.NotEqual(original.SessionId, bootstrap.SessionId);
        Assert.Equal(1, bootstrap.Sequence);
        Assert.NotNull(bootstrap.FullSnapshot);
        Assert.DoesNotContain(bootstrap.FullSnapshot.Objects, o => o.ObservedProjectile is not null || o.OwnProjectile is not null);
        LaunchFlights(engine);
        var launched = engine.CapturePresentationUpdate();
        Assert.Null(launched.FullSnapshot);
        Assert.NotEmpty(launched.Frames);
        foreach (var frame in launched.Frames) AssertJson(frame.State, requireOwn: false);
        AssertJson(launched.Frames[^1].State);
        time.Real = 2000;
        var delta = engine.CapturePresentationUpdate();
        Assert.Null(delta.FullSnapshot);
        Assert.NotEmpty(delta.Frames);
        foreach (var frame in delta.Frames) AssertJson(frame.State);
    }
}
