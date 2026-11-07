using System.Collections.Immutable;
using DeepSpaceSaga.Engine.TacticalBattle;
using DeepSpaceSaga.Engine.TacticalBattle.Content;
using Battle = DeepSpaceSaga.Engine.TacticalBattle.TacticalBattle;

namespace DeepSpaceSaga.Engine.Tests.TacticalBattle;

internal sealed record InitiativeReferenceUnit(string Id, string Side, int Initiative, int Hull, int Armor, string? TargetId);
internal sealed record InitiativeReferenceCase(string Name, int Damage, bool Hit, bool Jam,
    ImmutableArray<InitiativeReferenceUnit> Units, int[] ExpectedHull, string[] ExpectedShots);

internal static class InitiativeReferenceCases
{
    public static ImmutableArray<InitiativeReferenceCase> All =>
    [
        new("kill", 40, true, false, [new("Вера", "A", 80, 40, 0, "Таг-1"), new("Таг-1", "B", 60, 40, 0, "Вера")],
            [40, 0], ["Вера"]),
        new("disable", 80, true, false, [new("Вера", "A", 80, 100, 0, "Таг-1"), new("Таг-1", "B", 60, 100, 0, "Вера")],
            [100, 20], ["Вера"]),
        new("jam", 40, true, true, [new("Вера", "A", 80, 100, 0, "Таг-1"), new("Таг-1", "B", 60, 100, 0, "Вера")],
            [100, 60], ["Вера"]),
        new("armor", 15, true, false, [new("Вера", "A", 80, 100, 0, "Таг-1"), new("Юн", "A", 70, 100, 0, "Таг-1"),
            new("Таг-1", "B", 60, 100, 10, null)], [100, 100, 80], ["Вера", "Юн"]),
        new("no-retarget", 40, true, false, [new("Вера", "A", 80, 40, 0, "Таг-1"), new("Юн", "A", 70, 40, 0, "Таг-1"),
            new("Таг-1", "B", 60, 40, 0, "Вера"), new("Таг-2", "B", 50, 40, 0, null)], [40, 40, 0, 40], ["Вера"]),
        new("miss-80-60-40", 40, false, false, [new("Вера", "A", 80, 100, 0, "Таг-1"), new("Таг-1", "B", 60, 100, 0, "Вера"),
            new("Юн", "A", 40, 100, 0, "Таг-1")], [100, 100, 100], ["Вера", "Таг-1", "Юн"])
    ];

    public static Battle Create(InitiativeReferenceCase example)
    {
        var catalog = AssemblyTests.Catalog();
        var starts = catalog.Scenarios["initial"].Sides.SelectMany(s => s.Drones).ToArray();
        catalog = catalog with
        {
            Rules = catalog.Rules with
            {
                Hit = catalog.Rules.Hit with { MinChance = example.Hit ? 100 : 0, MaxChance = example.Hit ? 100 : 0 },
                Systems = catalog.Rules.Systems with { MinHullLoss = example.Jam ? 1 : int.MaxValue, Table = [new("weaponJammed", 1)] }
            },
            Weapons = new(catalog.Weapons.Entries.Select(e => (e.Id, e.Value with
            { DamageMin = example.Damage, DamageMax = example.Damage, Piercing = 0, RearKillChance = 0 }))),
            Pilots = new(catalog.Pilots.Entries.Select(e => (e.Id, e.Value with
            { Initiative = PilotSkill.Fixed(example.Units.FirstOrDefault(u => starts.Single(s => s.Id == u.Id).Pilot == e.Id)?.Initiative ?? 0) })))
        };
        var loadouts = starts.ToDictionary(s => s.Id, s => new TacticalLoadoutOverride(Weapon: "gun.sword.t1", Armor: "armor.none",
            Shield: "", Hull: example.Units.FirstOrDefault(u => u.Id == s.Id)?.Hull ?? 100), StringComparer.Ordinal);
        var battle = Battle.Create(catalog, "initial", 1, loadouts: loadouts);
        foreach (var drone in battle.Drones)
        {
            var unit = example.Units.FirstOrDefault(u => u.Id == drone.Id);
            drone.Status = unit is null ? TacticalDroneStatus.Disabled : TacticalDroneStatus.Air;
            if (unit is null) continue;
            drone.Hull = unit.Hull; drone.Armor = unit.Armor; drone.Role = "fighter";
            drone.Pose = new(unit.Side == "A" ? 0 : 10, 0, unit.Side == "A" ? 0 : 180);
        }
        battle.Carrier("A").Pose = new(-1e9, 0, 0);
        battle.Carrier("B").Pose = new(1e9, 0, 180);
        return battle;
    }
}
