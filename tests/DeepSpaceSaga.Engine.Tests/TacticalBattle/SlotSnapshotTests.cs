using System.Collections.Immutable;
using System.Text.Json;
using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Engine.TacticalBattle;
using DeepSpaceSaga.Engine.TacticalBattle.Ai;
using Battle = DeepSpaceSaga.Engine.TacticalBattle.TacticalBattle;

namespace DeepSpaceSaga.Engine.Tests.TacticalBattle;

public sealed class SlotSnapshotTests
{
    [Theory]
    [InlineData("light", 2, 1)]
    [InlineData("heavy", 2, 2)]
    [InlineData("thug", 1, 1)]
    public void Slots_follow_frame_order_with_empty_slots(string frame, int engines, int shields)
    {
        var catalog = AssemblyTests.Catalog();
        catalog = catalog with
        {
            Layouts = new(catalog.Layouts.Entries.Select(e =>
            (e.Id, e.Value with { Shield = null, ShieldCount = 0, Module = null })))
        };
        var battle = Battle.Create(catalog, "initial", 1);
        var unit = TacticalBattleSnapshotBuilder.Units(battle).First(u => u.FrameId == frame);
        Assert.Equal(engines, unit.Build!.EngineSlots); Assert.Equal(shields, unit.Build.ShieldSlots); Assert.Equal(1, unit.Build.ModuleSlots);
        Assert.Equal(2 + engines + shields + 1, unit.Slots.Length);
        Assert.Equal(new[] { TacticalSlotKind.Weapon, TacticalSlotKind.Armor }.Concat(Enumerable.Repeat(TacticalSlotKind.Shield, shields))
            .Concat(Enumerable.Repeat(TacticalSlotKind.Engine, engines)).Append(TacticalSlotKind.Module), unit.Slots.Select(s => s.Kind));
        Assert.All(unit.Slots.Where(s => s.Kind is TacticalSlotKind.Shield or TacticalSlotKind.Module), s =>
        { Assert.Null(s.ModuleId); Assert.Null(s.ModuleName); Assert.Equal(TacticalSlotState.Ok, s.State); Assert.Equal(1, s.Factor); });
        Check(battle, TacticalBattleSnapshotBuilder.Units(battle));
    }

    [Fact]
    public void Damaged_systems_are_marked()
    {
        var battle = Battle.Create(AssemblyTests.Catalog(), "initial", 1); var drone = battle.Drones[0];
        drone.Jammed = true; drone.Engines[0].Factor = .5; drone.DecoyCharges = 0;
        var unit = TacticalBattleSnapshotBuilder.Units(battle).Single(u => u.Id == drone.Id);
        Assert.Equal(TacticalSlotState.Jammed, unit.Slots.Single(s => s.Kind == TacticalSlotKind.Weapon).State);
        var engine = unit.Slots.Single(s => s.Kind == TacticalSlotKind.Engine && s.Index == 0);
        Assert.Equal(TacticalSlotState.Damaged, engine.State); Assert.Equal(.5, engine.Factor);
        Assert.All(unit.Slots.Where(s => s.Kind is TacticalSlotKind.Armor or TacticalSlotKind.Shield or TacticalSlotKind.Module), s => Assert.Equal(TacticalSlotState.Ok, s.State));
        Assert.Equal("decoy.t1", unit.Slots.Single(s => s.Kind == TacticalSlotKind.Module).ModuleId); Assert.Equal(0, unit.DecoyCharges);
        Assert.All(TacticalBattleSnapshotBuilder.Units(battle).Where(u => u.Kind == TacticalUnitKind.Carrier), u => Assert.Empty(u.Slots));
        Check(battle, TacticalBattleSnapshotBuilder.Units(battle));
    }

    public static IEnumerable<object[]> Seeds() => Enumerable.Range(1, 20).Select(seed => new object[] { seed });
    [Theory, MemberData(nameof(Seeds))]
    public void Slots_agree_with_existing_fields(int seed)
    {
        var catalog = AssemblyTests.Catalog(); var battle = Battle.Create(catalog, "initial", seed);
        int snapshots = 0;
        while (battle.Result is null && battle.Turn < catalog.Rules.SeriesTurnCap)
        {
            Capture(); battle.Planning(TacticalBattleAi.Plan); Capture(); battle.Movement(); Capture();
            if (battle.Result is null) { battle.Firing(); Capture(); foreach (var step in battle.LastSalvo!.Steps) Check(battle, step.UnitsAfter); }
        }
        battle.Run(TacticalBattleAi.Plan); Capture(); Assert.True(snapshots > 3);
        var independent = Battle.Create(catalog, "initial", seed); independent.Run(TacticalBattleAi.Plan);
        Assert.Equal(GoldenBattleTests.Signature(independent), GoldenBattleTests.Signature(battle));

        void Capture()
        {
            var state = (battle.Random.State, battle.Random.Calls, battle.InitiativeRandom.State, battle.InitiativeRandom.Calls);
            var units = TacticalBattleSnapshotBuilder.Units(battle); Check(battle, units);
            Assert.Equal(JsonSerializer.Serialize(units), JsonSerializer.Serialize(TacticalBattleSnapshotBuilder.Units(battle)));
            Assert.Equal(state, (battle.Random.State, battle.Random.Calls, battle.InitiativeRandom.State, battle.InitiativeRandom.Calls)); snapshots++;
        }
    }

    private static void Check(Battle battle, ImmutableArray<TacticalUnitSnapshot> units)
    {
        foreach (var u in units)
        {
            if (u.Kind == TacticalUnitKind.Carrier) { Assert.Empty(u.Slots); continue; }
            var build = u.Build!; var frame = battle.Catalog.Frames[u.FrameId];
            Assert.Equal(frame.EngineSlots, build.EngineSlots); Assert.Equal(frame.ModuleSlots, build.ModuleSlots);
            Assert.Equal(2 + frame.ShieldSlots + frame.EngineSlots + frame.ModuleSlots, u.Slots.Length);
            Assert.Equal(u.Slots.OrderBy(s => s.Kind).ThenBy(s => s.Index), u.Slots);
            foreach (var group in u.Slots.GroupBy(s => s.Kind)) Assert.Equal(Enumerable.Range(0, group.Count()), group.Select(s => s.Index));
            var weapon = u.Slots.Single(s => s.Kind == TacticalSlotKind.Weapon); Assert.Equal(build.WeaponId, weapon.ModuleId); Assert.Equal(build.WeaponName, weapon.ModuleName);
            Assert.Equal(u.Jammed ? TacticalSlotState.Jammed : TacticalSlotState.Ok, weapon.State);
            var armor = u.Slots.Single(s => s.Kind == TacticalSlotKind.Armor); Assert.Equal(build.ArmorId, armor.ModuleId); Assert.Equal(build.ArmorName, armor.ModuleName);
            foreach (var slot in u.Slots.Where(s => s.Kind == TacticalSlotKind.Shield))
            { Assert.Equal(slot.Index < build.ShieldCount ? build.ShieldId : null, slot.ModuleId); Assert.Equal(TacticalSlotState.Ok, slot.State); }
            foreach (var slot in u.Slots.Where(s => s.Kind == TacticalSlotKind.Engine))
            {
                var engine = slot.Index < u.Engines.Length ? u.Engines[slot.Index] : null;
                Assert.Equal(engine?.Id, slot.ModuleId); Assert.Equal(engine?.Factor ?? 1, slot.Factor);
                Assert.Equal(engine?.Factor < 1 ? TacticalSlotState.Damaged : TacticalSlotState.Ok, slot.State);
            }
            foreach (var slot in u.Slots.Where(s => s.Kind == TacticalSlotKind.Module))
            { Assert.Equal(slot.Index == 0 ? build.DecoyId : null, slot.ModuleId); Assert.Equal(TacticalSlotState.Ok, slot.State); }
        }
    }
}
