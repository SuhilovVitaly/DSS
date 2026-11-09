using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Engine.TacticalBattle;
using Battle = DeepSpaceSaga.Engine.TacticalBattle.TacticalBattle;

namespace DeepSpaceSaga.Engine.Tests.TacticalBattle;

public sealed class ConsciousnessTests
{
    [Fact]
    public void Every_drone_starts_battle_with_rules_start()
    {
        var battle = Battle.Create(AssemblyTests.Catalog(), null, 1);
        Assert.Equal(42, battle.Random.Calls);
        Assert.All(battle.Drones, d => Assert.Equal(100, d.Consciousness));
        var units = TacticalBattleSnapshotBuilder.Units(battle);
        Assert.All(units.Where(u => u.Kind == TacticalUnitKind.Drone), u => Assert.Equal(100, u.Consciousness));
        Assert.All(units.Where(u => u.Kind == TacticalUnitKind.Carrier), u => Assert.Null(u.Consciousness));
    }

    [Fact]
    public void Recovery_adds_per_turn_up_to_max()
    {
        var battle = Battle.Create(AssemblyTests.Catalog(), null, 1);
        battle.Drones[0].Consciousness = 80;
        battle.Drones[1].Consciousness = 98;
        battle.Drones[2].Consciousness = 70;
        battle.Drones[2].Status = TacticalDroneStatus.Disabled;
        battle.Planning(_ => { });
        Assert.Equal(80, battle.Drones[0].Consciousness); // no recovery on turn one
        battle.Movement();
        battle.Firing((_, _) => new(null, false));
        long calls = battle.Random.Calls;
        battle.Planning(_ => { });
        Assert.Equal(85, battle.Drones[0].Consciousness);
        Assert.Equal(100, battle.Drones[1].Consciousness);
        Assert.Equal(70, battle.Drones[2].Consciousness);
        Assert.Equal(calls, battle.Random.Calls);
    }

    [Fact]
    public void Session_and_direct_paths_agree_and_draw_no_random()
    {
        var catalog = AssemblyTests.Catalog();
        var session = TacticalBattleSession.TryCreate(catalog, new("mind", 1), out var code)!;
        var controlCatalog = catalog with { Rules = catalog.Rules with
        { Consciousness = catalog.Rules.Consciousness with { RecoveryPerTurn = 0 } } };
        var control = TacticalBattleSession.TryCreate(controlCatalog, new("mind", 1), out _)!;
        Assert.Equal(TacticalBattleRejectionCode.None, code);
        var direct = Battle.Create(catalog, null, 1);
        direct.Planning(_ => { });
        session.Battle.Drones[0].Consciousness = direct.Drones[0].Consciousness = 80;
        var state = session.Capture();
        state = session.Move(new("move", state.BattleId, state.Revision)).State!;
        if (state.Phase == TacticalBattlePhase.Fire)
            state = session.Fire(new("fire", state.BattleId, state.Revision)).State!;
        direct.Movement();
        direct.Firing((_, _) => new(null, false));
        direct.Planning(_ => { });
        Assert.Equal(85, state.Units.Single(u => u.Id == direct.Drones[0].Id).Consciousness);
        Assert.Equal(direct.Drones.Select(d => d.Consciousness), session.Battle.Drones.Select(d => d.Consciousness));
        Assert.Equal(42, direct.Random.Calls);
        var controlState = control.Capture();
        controlState = control.Move(new("move", controlState.BattleId, controlState.Revision)).State!;
        if (controlState.Phase == TacticalBattlePhase.Fire)
            control.Fire(new("fire", controlState.BattleId, controlState.Revision));
        Assert.Equal(control.Battle.Random.Calls, session.Battle.Random.Calls);
        Assert.Equal(control.Battle.Random.State, session.Battle.Random.State);
    }
}
