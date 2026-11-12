using System.Collections.Immutable;
using DeepSpaceSaga.Client.UI.Screens.TacticalBattle;
using DeepSpaceSaga.Client.Tests.TacticalBattle.View;
using DeepSpaceSaga.Contracts;
using SkiaSharp;

namespace DeepSpaceSaga.Client.Tests.TacticalBattle;

[Collection("InterfaceLog")]
public sealed class TabletopCameraTests
{
    [Fact]
    public async Task Movement_fits_own_group_and_suggestion_ends_without_history_or_enemy_carrier()
    {
        await using var scene = await Ep0019Scenes.Create("1-move-no-hover");
        var state = scene.Frame.Battle;
        var camera = new TacticalBattleCamera(); camera.Update(state, 1280, 720, tabletop: true);
        var safe = new SKRect(401.9f, 205.9f, 834.1f, 514.1f);
        foreach (var unit in state.Units.Where(u => u.Side == state.PlayerSide)) Assert.True(safe.Contains(camera.View.Project(unit.Pose)), unit.Id);
        foreach (var row in scene.Frame.Movement!.Orders) Assert.True(safe.Contains(camera.View.Project(row.Path[^1])), row.Unit.Id);
        var changed = state with
        {
            BattleId = state.BattleId + "-copy",
            Paths = [new(1, "old", [new(999999, 999999, 0)])],
            Units = state.Units.Select(u => u.Side != state.PlayerSide ? u with { Pose = new(99999, 99999, 0) } : u).ToImmutableArray()
        };
        var other = new TacticalBattleCamera(); other.Update(changed, 1280, 720, tabletop: true);
        Assert.Equal(camera.View, other.View);
        using var catalog = TacticalBattleSkinCatalog.Load(Path.Combine(AppContext.BaseDirectory, "Skins"));
        var screen = new TacticalBattleScreen(scene.Screen.Flow, 1, catalog.GetOrClassic("tabletop"));
        Assert.Equal(camera.View, screen.CaptureFrame(1280, 720)!.View);
        screen.OnDeactivated();
    }

    [Fact]
    public async Task Fire_frame_contains_targets_threats_and_last_shots_then_freezes_until_salvo_exits()
    {
        await using var scene = await Ep0019Scenes.Create("2-fire-hover-own");
        var state = scene.Frame.Battle;
        var camera = new TacticalBattleCamera(); camera.Update(state, 1280, 720, tabletop: true);
        var ids = state.Targets.SelectMany(g => g.List).Select(t => t.TargetId).Concat(state.Incoming.Select(i => i.ShooterId))
            .Concat(state.Units.Where(u => u.Side == state.PlayerSide && u.Kind == TacticalUnitKind.Drone && u.State != TacticalUnitState.Destroyed).Select(u => u.Id)).ToHashSet();
        var safe = new SKRect(311.9f, 115.9f, 924.1f, 604.1f);
        foreach (var unit in state.Units.Where(u => ids.Contains(u.Id))) Assert.True(safe.Contains(camera.View.Project(unit.Pose)), unit.Id);
        var before = camera.View;
        var moved = state with
        {
            Phase = TacticalBattlePhase.Move,
            Turn = state.Turn + 1,
            Units = state.Units.Select(u => u with { Pose = u.Pose with { X = u.Pose.X + 1000 } }).ToImmutableArray()
        };
        camera.Update(moved, 1280, 720, salvo: true, tabletop: true);
        Assert.Equal(before, camera.View);
        camera.Update(moved, 1280, 720, salvo: true, tabletop: true);
        Assert.Equal(before, camera.View);
        camera.Update(moved, 1280, 720, tabletop: true);
        Assert.NotEqual(before, camera.View);
        camera.Zoom(600, 400, -1); var manual = camera.View;
        camera.Update(state, 1280, 720, tabletop: true);
        Assert.True(camera.UserView); Assert.Equal(manual, camera.View);
        camera.Update(state with { BattleId = "new" }, 1280, 720, tabletop: true);
        Assert.False(camera.UserView); Assert.Equal(before, camera.View);
    }

    [Fact]
    public async Task Shot_participants_are_included_even_if_no_longer_targetable()
    {
        await using var scene = await Ep0019Scenes.Create("4-salvo-results");
        var state = scene.Frame.Battle with { Phase = TacticalBattlePhase.Fire, Targets = [], Incoming = [] };
        Assert.NotEmpty(state.Shots);
        var camera = new TacticalBattleCamera(); camera.Update(state, 1280, 720, tabletop: true);
        var last = state.Shots.Max(s => s.Turn);
        var ids = state.Shots.Where(s => s.Turn == last).SelectMany(s => new[] { s.ShooterId, s.TargetId }).ToHashSet();
        var safe = new SKRect(311.9f, 115.9f, 924.1f, 604.1f);
        foreach (var unit in state.Units.Where(u => ids.Contains(u.Id))) Assert.True(safe.Contains(camera.View.Project(unit.Pose)), unit.Id);
    }
}
