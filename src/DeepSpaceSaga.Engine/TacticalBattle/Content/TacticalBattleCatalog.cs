using System.Collections.Immutable;

namespace DeepSpaceSaga.Engine.TacticalBattle.Content;

/// <summary>The array owns iteration order; the index owns only ordinal ID lookup.</summary>
internal sealed class OrderedBattleCatalog<T>
{
    private readonly ImmutableDictionary<string, T> _byId;

    public OrderedBattleCatalog(IEnumerable<(string Id, T Value)> entries)
    {
        Entries = entries.ToImmutableArray();
        _byId = Entries.ToImmutableDictionary(e => e.Id, e => e.Value, StringComparer.Ordinal);
    }

    public ImmutableArray<(string Id, T Value)> Entries { get; }
    public int Count => Entries.Length;
    public T this[string id] => _byId[id];
    public bool Contains(string id) => _byId.ContainsKey(id);
}

internal sealed record TacticalBattleCatalog(TacticalRules Rules, BattleAi Ai,
    OrderedBattleCatalog<BattleFrame> Frames, OrderedBattleCatalog<BattleWeapon> Weapons,
    OrderedBattleCatalog<BattleEngine> Engines, OrderedBattleCatalog<BattleShield> Shields,
    OrderedBattleCatalog<BattleDecoy> Decoys, OrderedBattleCatalog<BattleArmor> Armor, OrderedBattleCatalog<BattlePilot> Pilots,
    OrderedBattleCatalog<BattleLayout> Layouts, OrderedBattleCatalog<BattleScenario> Scenarios);
