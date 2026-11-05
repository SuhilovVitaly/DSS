namespace DeepSpaceSaga.Engine.TacticalBattle;

internal sealed class TacticalWeaponStatistics(string weaponId)
{
    public string WeaponId { get; } = weaponId;
    public long Shots { get; set; }
    public long Hits { get; set; }
    public long RearHits { get; set; }
    public long RearKills { get; set; }
    public long Hull { get; set; }
}

internal sealed class TacticalBattleStatistics
{
    private readonly List<TacticalWeaponStatistics> _weapons = [];
    public long Shots { get; set; }
    public long Hits { get; set; }
    public long RearHits { get; set; }
    public long RearKills { get; set; }
    public long Systems { get; set; }
    public long Disabled { get; set; }
    public long Hull { get; set; }
    public long Armor { get; set; }
    public long CarrierHull { get; set; }
    public string? FirstLoss { get; set; }
    public IReadOnlyList<TacticalWeaponStatistics> Weapons => _weapons;
    public TacticalWeaponStatistics ForWeapon(string id)
    {
        var value = _weapons.FirstOrDefault(w => w.WeaponId == id);
        if (value is not null) return value;
        value = new(id);
        _weapons.Add(value);
        return value;
    }
}

internal sealed partial class TacticalBattle
{
    public TacticalBattleStatistics Statistics { get; } = new();
}
