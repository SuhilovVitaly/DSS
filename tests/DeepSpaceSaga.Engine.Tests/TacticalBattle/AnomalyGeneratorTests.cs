using System.Text.Json;
using DeepSpaceSaga.Engine.TacticalBattle;
using DeepSpaceSaga.Motion.TacticalBattle;
using Battle = DeepSpaceSaga.Engine.TacticalBattle.TacticalBattle;

namespace DeepSpaceSaga.Engine.Tests.TacticalBattle;

public sealed class AnomalyGeneratorTests
{
    [Theory]
    [InlineData(1L)]
    [InlineData(42L)]
    [InlineData(-1L)]
    [InlineData(long.MinValue)]
    public void Same_seed_same_anomalies(long seed)
    {
        var catalog = AssemblyTests.Catalog();
        var a = Battle.Create(catalog, null, seed); var b = Battle.Create(catalog, null, seed);
        Assert.Equal(JsonSerializer.Serialize(a.Anomalies), JsonSerializer.Serialize(b.Anomalies));
        Assert.Equal(JsonSerializer.Serialize(TacticalBattleSnapshotBuilder.Anomalies(a)),
            JsonSerializer.Serialize(TacticalBattleSnapshotBuilder.Anomalies(b)));
        Assert.NotEqual(JsonSerializer.Serialize(a.Anomalies), JsonSerializer.Serialize(Battle.Create(catalog, null, seed + 1).Anomalies));
    }

    [Fact]
    public void Counts_and_placement_follow_rules()
    {
        var battle = Battle.Create(AssemblyTests.Catalog(), null, 1);
        Check(battle);
        Assert.Equal(battle.Anomalies.Select(a => a.Kind).OrderBy(k => k), battle.Anomalies.Select(a => a.Kind));
        var session = TacticalBattleSession.TryCreate(battle.Catalog, new("anomaly-snapshot", 1), out _)!;
        var first = session.Capture(); long calls = session.Battle.Random.Calls;
        Assert.Equal(battle.Anomalies.Length, first.Anomalies.Length);
        Assert.Equal(JsonSerializer.Serialize(first.Anomalies), JsonSerializer.Serialize(session.Capture().Anomalies));
        Assert.Equal(calls, session.Battle.Random.Calls);
        Assert.Equal(new Contracts.TacticalAnomalyRulesSnapshot(170, 100, 200), first.Rules.Anomalies);
    }

    [Fact]
    public void Main_random_stream_is_untouched()
    {
        var catalog = AssemblyTests.Catalog();
        var empty = catalog with
        {
            Rules = catalog.Rules with
            { Anomalies = catalog.Rules.Anomalies with { HeatCount = [0, 0], DustCount = [0, 0] } }
        };
        var a = Battle.Create(catalog, null, 1); var b = Battle.Create(empty, null, 1);
        Assert.NotEmpty(a.Anomalies); Assert.Empty(b.Anomalies);
        Assert.Equal(42, a.Random.Calls);
        Assert.Equal((a.Random.Calls, a.Random.State), (b.Random.Calls, b.Random.State));
        Assert.Equal((a.InitiativeRandom.Calls, a.InitiativeRandom.State), (b.InitiativeRandom.Calls, b.InitiativeRandom.State));
        Assert.Equal(a.Drones.Select(d => d.Pose), b.Drones.Select(d => d.Pose));
    }

    [Fact]
    public void Empty_fields_preserve_pre_anomaly_outcomes()
    {
        var catalog = AssemblyTests.Catalog();
        catalog = catalog with { Rules = catalog.Rules with { Anomalies = catalog.Rules.Anomalies with { HeatCount = [0, 0], DustCount = [0, 0] } } };
        // Independent US8/TK3 pre-effect whole-battle signatures, seed1, all five tactics.
        (string Tactic, string Hash)[] previous =
        [
            ("balanced", "F4761D96FC47CBFBE0121F904D017C578B1CF0A95C06735583EBBEAFEFA24D1B"),
            ("commander", "A1C2B743AB794E37F116DC50EC27ABAE9B600D32C4FB1F4973338519B4576DB2"),
            ("defence", "C02508446BB2EFD8E9004699034BEF58E1CFC9F44B8F09A9B7687E431C270A49"),
            ("dogfight", "EC07E8E50C89DB2C66BA134776FE50372F467E7B76DB40B81E3CB920189A7E89"),
            ("raid", "EDA03C4AEEDA1DECE456FB3827F762224537A1D5167C2C570D26403CBF9B5E0D")
        ];
        foreach (var row in previous)
        {
            var json = System.Text.Json.Nodes.JsonNode.Parse(GoldenBattleTests.Signature(GoldenBattleTests.Run(catalog, 1, row.Tactic, false)))!;
            json.AsObject().Remove("Absorbed"); json.AsObject().Remove("DustSkips");
            foreach (var unit in json["Units"]!.AsArray())
            {
                unit!.AsObject().Remove("InDust"); unit.AsObject().Remove("Slots");
                if (unit["Build"] is { } slotBuild) { slotBuild.AsObject().Remove("EngineSlots"); slotBuild.AsObject().Remove("ModuleSlots"); }
            }
            Assert.Equal(row.Hash, Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(json.ToJsonString()))));
        }
    }

    [Fact]
    public void Thousand_seeds_generate()
    {
        var catalog = AssemblyTests.Catalog();
        string? evidenceRoot = Environment.GetEnvironmentVariable("DSS_VALIDATION_ROOT");
        var lines = new List<string> { "seed,heat,dust,clumps" };
        for (int seed = 1; seed <= 1000; seed++)
        {
            var battle = Battle.Create(catalog, null, seed); Check(battle);
            Assert.Equal(42, battle.Random.Calls);
            lines.Add(FormattableString.Invariant($"{seed},{battle.Anomalies.Count(a => a.Kind == TacticalAnomalyKind.Heat)},{battle.Anomalies.Count(a => a.Kind == TacticalAnomalyKind.Dust)},{battle.Anomalies.Sum(a => a.Clumps.Length)}"));
            if (seed == 1 && evidenceRoot is { Length: > 0 }) File.WriteAllText(Path.Combine(evidenceRoot, "anomalies-seed1.json"),
                JsonSerializer.Serialize(battle.Anomalies, new JsonSerializerOptions { WriteIndented = true }));
        }
        if (evidenceRoot is { Length: > 0 }) File.WriteAllLines(Path.Combine(evidenceRoot, "anomaly-generation.csv"), lines);
    }

    [Fact]
    public void Impossible_clearance_or_zero_attempts_omits_anomalies()
    {
        var catalog = AssemblyTests.Catalog();
        foreach (var rules in new[] { catalog.Rules.Anomalies with { Clearance = 1e9 }, catalog.Rules.Anomalies with { MaxAttempts = 0 } })
        {
            var battle = Battle.Create(catalog with { Rules = catalog.Rules with { Anomalies = rules } }, null, 1);
            Assert.Empty(battle.Anomalies); Assert.Equal(42, battle.Random.Calls);
        }
    }

    [Fact]
    public void Named_stream_anchor_matches_independent_vector()
    {
        var catalog = AssemblyTests.Catalog();
        catalog = catalog with
        {
            Rules = catalog.Rules with
            {
                Anomalies = catalog.Rules.Anomalies with
                { HeatCount = [1, 1], DustCount = [0, 0], Clumps = [0, 0], Spread = [40, 40], Clearance = 0, BandFraction = 1, MaxAttempts = 1 }
            }
        };
        var battle = Battle.Create(catalog, null, 1); var anomaly = Assert.Single(battle.Anomalies);
        // Independent FNV-1a64 vector: seed1/name -> 10816105672073054107, low32=1962968987.
        // Two fixed count draws precede these third/fourth Mulberry32 uniforms.
        double xA = battle.Drones.Where(d => d.Side == "A").Average(d => d.Pose.X);
        double xB = battle.Drones.Where(d => d.Side == "B").Average(d => d.Pose.X);
        double y0 = battle.Drones.Min(d => d.Pose.Y), y1 = battle.Drones.Max(d => d.Pose.Y);
        Assert.Equal(xA + (xB - xA) * .9286327394656837, anomaly.CenterX, 12);
        Assert.Equal(y0 + (y1 - y0) * .5588995071593672, anomaly.CenterY, 12);
        Assert.Empty(anomaly.Clumps);
    }

    private static void Check(Battle battle)
    {
        var r = battle.Catalog.Rules.Anomalies;
        Assert.InRange(battle.Anomalies.Count(a => a.Kind == TacticalAnomalyKind.Heat), r.HeatCount[0], r.HeatCount[1]);
        Assert.InRange(battle.Anomalies.Count(a => a.Kind == TacticalAnomalyKind.Dust), r.DustCount[0], r.DustCount[1]);
        Assert.Equal(battle.Anomalies.Length, battle.Anomalies.Select(a => a.Id).Distinct().Count());
        double xA = battle.Drones.Where(d => d.Side == "A").Average(d => d.Pose.X);
        double xB = battle.Drones.Where(d => d.Side == "B").Average(d => d.Pose.X);
        double inset = (1 - r.BandFraction) / 2 * (xB - xA);
        double left = Math.Min(xA + inset, xB - inset), right = Math.Max(xA + inset, xB - inset);
        foreach (var a in battle.Anomalies)
        {
            Assert.InRange(a.CenterX, left, right);
            Assert.InRange(a.CenterY, battle.Drones.Min(d => d.Pose.Y), battle.Drones.Max(d => d.Pose.Y));
            Assert.All(battle.Drones.Cast<TacticalUnit>().Concat(battle.Carriers), u =>
                Assert.True(TacticalGeometry.Dist(a.CenterX, a.CenterY, u.Pose.X, u.Pose.Y) >= r.Clearance));
            Assert.InRange(a.Clumps.Length, r.Clumps[0], r.Clumps[1]);
            foreach (var c in a.Clumps)
            {
                Assert.True(double.IsFinite(c.X) && double.IsFinite(c.Y));
                Assert.InRange(c.Amplitude, r.ClumpAmp[0], r.ClumpAmp[1]);
                Assert.InRange(c.SigmaX, r.Spread[0] * r.ClumpSigmaX[0], r.Spread[1] * r.ClumpSigmaX[1]);
                Assert.InRange(c.SigmaY, r.Spread[0] * r.ClumpSigmaY[0], r.Spread[1] * r.ClumpSigmaY[1]);
                Assert.InRange(c.Rotation, 0, Math.PI);
            }
        }
    }
}
