using System.Collections.Immutable;
using DeepSpaceSaga.Client.Tests.TacticalBattle;
using DeepSpaceSaga.Client.UI.Screens;
using DeepSpaceSaga.Client.UI.Screens.TacticalBattle;
using DeepSpaceSaga.Contracts;
using Silk.NET.Input;
using SkiaSharp;

namespace DeepSpaceSaga.Client.Tests.Demo;

[Collection("InterfaceLog")]
public sealed class Ep0035Us15Tk1Tests
{
    [Fact]
    public async Task CameraCardsAndFireSelectionKeepAllThreeCarrierIdentities()
    {
        await using var fixture = await BattleNavigationTests.Fixture.Create();
        await fixture.Route(ScreenEvent.OpenTacticalBattle);
        var setup = (TacticalBattleModalScreen)fixture.Screens.Current;
        await setup.Flow.OptionsTask!;
        await fixture.Route(setup.OnKeyDown(Key.Enter));
        var state = ((TacticalBattleScreen)fixture.Screens.Current).Flow.State!;
        var first = state.Units.Single(u => u.Kind == TacticalUnitKind.Carrier && u.Side != state.PlayerSide);
        var second = first with { Id = "second-lair", Pose = first.Pose with { Y = first.Pose.Y + 300 }, Hull = first.Hull - 50 };
        state = state with { Units = state.Units.Add(second), BattleId = "squad" };
        var camera = new TacticalBattleCamera();
        camera.Update(state, 1280, 720, tabletop: true);
        foreach (var carrier in state.Units.Where(u => u.Kind == TacticalUnitKind.Carrier))
        {
            var point = camera.View.Project(carrier.Pose);
            Assert.InRange(point.X, 0, 1280); Assert.InRange(point.Y, 0, 720);
        }
        var frame = new TacticalBattleFrame(state, camera.View, SKRect.Empty, 1, false, "", TacticalBattlePresentation.Current);
        var cards = TacticalBattleSidesTab.Cards(frame).Where(c => c.Unit.Kind == TacticalUnitKind.Carrier).ToArray();
        Assert.Equal(3, cards.Length);
        Assert.Equal(first.Hull, cards.Single(c => c.Unit.Id == first.Id).Unit.Hull);
        Assert.Equal(second.Hull, cards.Single(c => c.Unit.Id == second.Id).Unit.Hull);
        var shooter = state.Units.First(u => u.Kind == TacticalUnitKind.Drone && u.Side == state.PlayerSide);
        state = state with { Phase = TacticalBattlePhase.Fire, Targets = [new(shooter.Id,
            [new(first.Id, 100, 0, false, 0, .5), new(second.Id, 150, 0, false, 0, .5)], first.Id)] };
        var orders = new TacticalBattleFireOrders();
        orders.Observe(state, shooter.Id);
        Assert.True(orders.Select(shooter.Id));
        foreach (var carrier in new[] { first, second })
        {
            Assert.True(orders.Choose(carrier.Id));
            Assert.Equal(carrier.Id, orders.BuildTargets().Single(t => t.DroneId == shooter.Id).TargetId);
        }
        Assert.False(orders.Choose("unknown"));
    }
}
