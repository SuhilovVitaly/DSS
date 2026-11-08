using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Nodes;
using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Engine.TacticalBattle;
using DeepSpaceSaga.Engine.TacticalBattle.Content;
using DeepSpaceSaga.Motion.TacticalBattle;
using Battle = DeepSpaceSaga.Engine.TacticalBattle.TacticalBattle;

namespace DeepSpaceSaga.Engine.Tests.TacticalBattle;

public sealed class DecoyStateTests
{
    [Fact]
    public void Every_drone_starts_with_module_charges()
    {
        var battle = Battle.Create(AssemblyTests.Catalog(), null, 1);
        Assert.All(battle.Drones, d =>
        {
            Assert.Equal("decoy.t1", d.Decoy!.Id);
            Assert.Equal(2, d.DecoyCharges);
            var expected = TacticalLimits.Of(new(d.Frame.FrameMassKg, d.Frame.ReferenceMassKg, d.Frame.MinSpeed,
                d.Frame.MaxSpeedCap, d.Frame.TurnCapDeg), d.Plates.MassKg + 10, d.Weapon.MassKg,
                d.Shield?.MassKg ?? 0, d.ShieldCount, d.Engines.Select(e => e.Motion).ToArray());
            Assert.Equal(expected, d.Limits);
        });
        Assert.All(TacticalBattleSnapshotBuilder.Units(battle).Where(u => u.Kind == TacticalUnitKind.Drone), u =>
        {
            Assert.Equal(2, u.DecoyCharges);
            Assert.Equal("decoy.t1", u.Build!.DecoyId);
            Assert.Equal("Ловушки I", u.Build.DecoyName);
        });
    }

    [Fact]
    public void Carrier_has_no_decoy()
    {
        var battle = Battle.Create(AssemblyTests.Catalog(), null, 1);
        Assert.All(TacticalBattleSnapshotBuilder.Units(battle).Where(u => u.Kind == TacticalUnitKind.Carrier), u =>
        {
            Assert.Null(u.DecoyCharges);
            Assert.Null(u.Build);
        });
    }

    [Fact]
    public void Assembly_draws_same_random_calls()
    {
        var catalog = AssemblyTests.Catalog();
        var noModule = catalog with
        {
            Layouts = new OrderedBattleCatalog<BattleLayout>(
            catalog.Layouts.Entries.Select(e => (e.Id, e.Value with { Module = null })))
        };
        var a = Battle.Create(catalog, null, 1);
        var b = Battle.Create(noModule, null, 1);
        Assert.Equal(42, a.Random.Calls);
        Assert.Equal((a.Random.Calls, a.Random.State), (b.Random.Calls, b.Random.State));
        Assert.Equal((a.InitiativeRandom.Calls, a.InitiativeRandom.State), (b.InitiativeRandom.Calls, b.InitiativeRandom.State));
        Assert.Equal(a.Drones.Select(d => (d.Pose, d.Accuracy, d.Maneuver, d.Initiative)),
            b.Drones.Select(d => (d.Pose, d.Accuracy, d.Maneuver, d.Initiative)));
        Assert.All(b.Drones, d =>
        {
            Assert.Null(d.Decoy);
            Assert.Equal(0, d.DecoyCharges);
            Assert.Equal(TacticalLimits.Of(new(d.Frame.FrameMassKg, d.Frame.ReferenceMassKg, d.Frame.MinSpeed,
                d.Frame.MaxSpeedCap, d.Frame.TurnCapDeg), d.Plates.MassKg, d.Weapon.MassKg,
                d.Shield?.MassKg ?? 0, d.ShieldCount, d.Engines.Select(e => e.Motion).ToArray()), d.Limits);
        });
        // M1 signatures captured before module mass/state was implemented. Remove only the new DTO fields.
        (string Tactic, string Hash)[] previous =
        [
            ("balanced", "EFF002B7A071AC20951E969325CE5523B5C96E8FF3E29D4B535555CBA9627783"),
            ("commander", "38886CA312B60E132001F8CCCCCD8435293114C11B2BA7ED14E53CEF4D1D1527"),
            ("defence", "24F3E128811327E93D9B67805E1D258CF8D4370F47D4DD6BD76027A309E40751"),
            ("dogfight", "3ED60C2AE3B48800A9AD8A1332DAB1BBEE416ADC75301529BBFBF0D0C6BD5196"),
            ("raid", "EC548C9D4BA3CBA6F2B2B40EC076AE60032F1F95BBE8E69F80CD391997F84877")
        ];
        foreach (var row in previous)
        {
            var legacy = GoldenBattleTests.Run(noModule, 1, row.Tactic, false);
            var json = JsonNode.Parse(GoldenBattleTests.Signature(legacy))!;
            foreach (string added in new[] { "DecoysLaunched", "ShotsDecoyed", "HitsDecoyed", "DecoyChanceBefore", "DecoyChanceAfter" })
                json.AsObject().Remove(added);
            foreach (var unit in json["Units"]!.AsArray())
            {
                unit!.AsObject().Remove("DecoyCharges");
                if (unit["Build"] is { } build)
                {
                    build.AsObject().Remove("DecoyId");
                    build.AsObject().Remove("DecoyName");
                }
            }
            Assert.Equal(row.Hash, Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(json.ToJsonString()))));
        }
    }
}
