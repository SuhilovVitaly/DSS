using DeepSpaceSaga.Engine.TacticalBattle;
using DeepSpaceSaga.Engine.TacticalBattle.Content;
using Battle = DeepSpaceSaga.Engine.TacticalBattle.TacticalBattle;

namespace DeepSpaceSaga.Engine.Tests;

public sealed class Ep0029Us0004Tk0003Tests
{
    private static readonly string ClientRoot = Path.GetFullPath(Path.Combine(
        AppContext.BaseDirectory, "..", "..", "..", "..", "..", "src", "DeepSpaceSaga.Client"));

    private static TacticalBattleCatalog Catalog() =>
        TacticalBattleContentLoader.Load(Path.Combine(ClientRoot, "Data", "TacticalBattle"));

    [Fact]
    public void RepeatedCaptureDoesNotReload()
    {
        var battle = Battle.Create(Catalog(), null, 1);
        var drone = battle.Drones.First(d => d.Side == "A");
        drone.AmmoCurrent = 3; // a partially spent magazine must not be refilled by projection

        for (int i = 0; i < 3; i++)
        {
            var units = TacticalBattleSnapshotBuilder.Units(battle);
            var projected = units.First(u => u.Id == drone.Id);
            Assert.Equal(3, projected.AmmoCurrent);
            Assert.Equal(drone.AmmoCapacity, projected.AmmoCapacity);
        }
    }

    [Fact]
    public void FrameCapacityAndOwnedAmmoValidated()
    {
        var catalog = Catalog();
        // "Вера" flies heavy.sword.shield → heavy frame capacity 12; 13 is rejected.
        var loadouts = new Dictionary<string, TacticalLoadoutOverride> { ["Вера"] = new(Ammo: 13) };
        Assert.Throws<ArgumentOutOfRangeException>(() => Battle.Create(catalog, null, 1, loadouts: loadouts));
    }

    [Fact]
    public void EnemyThugStartsWithSix()
    {
        var battle = Battle.Create(Catalog(), null, 1);

        // Every enemy thug drone starts with the thug frame's 6-torpedo magazine (finite AI).
        Assert.All(battle.Drones.Where(d => d.Side == "B"), d =>
        {
            Assert.Equal(6, d.AmmoCapacity);
            Assert.Equal(6, d.AmmoCurrent);
        });

        // Player heavy drones start full at 12, light at 6.
        Assert.Equal(12, battle.Drones.First(d => d.Id == "Вера").AmmoCurrent);
        Assert.Equal(12, battle.Drones.First(d => d.Id == "Юн").AmmoCurrent);
        Assert.Equal(6, battle.Drones.First(d => d.Id == "Марта").AmmoCurrent);
        Assert.Equal(6, battle.Drones.First(d => d.Id == "Олег").AmmoCurrent);
    }
}
