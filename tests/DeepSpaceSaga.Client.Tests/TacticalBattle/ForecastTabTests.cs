using DeepSpaceSaga.Client.UI.Screens;
using DeepSpaceSaga.Client.UI.Screens.TacticalBattle;
using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Motion.TacticalBattle;
using Silk.NET.Input;
using SkiaSharp;

namespace DeepSpaceSaga.Client.Tests.TacticalBattle;

[Collection("InterfaceLog")]
public sealed class ForecastTabTests
{
    [Fact]
    public async Task MovementPredictionUsesSharedChanceIncludesCarrierAndSortsContacts()
    {
        await using var fixture = await BattleNavigationTests.Fixture.Create();
        var screen = await Start(fixture);
        var frame = screen.CaptureFrame(1280, 720)!;
        var order = frame.Movement!.SelectedOrder!;
        var own = order.Unit;
        var enemy = frame.Battle.Units.First(u => u.Side != frame.Battle.PlayerSide && u.Kind == TacticalUnitKind.Drone) with
        {
            Pose = new(65, 4, 180),
            Speed = 0
        };
        var carrier = frame.Battle.Units.First(u => u.Side != frame.Battle.PlayerSide && u.Kind == TacticalUnitKind.Carrier) with
        {
            Pose = new(90, 0, 0)
        };
        var battle = frame.Battle with { Units = [own, enemy, carrier] };
        var movement = frame.Movement with { Orders = [order with { Path = [new(0, 0, 0)] }] };
        var row = Assert.Single(TacticalBattleForecastTab.PredictMovement(battle, movement));
        Assert.Equal(2, row.Outgoing.Length);
        Assert.Equal(row.Outgoing.OrderByDescending(s => s.Chance), row.Outgoing);
        Assert.Equal(enemy.Id, Assert.Single(row.Incoming).Shooter);
        var expectedCarrier = row.Outgoing.Single(s => s.Target == carrier.Id);
        var gun = own.Build!;
        var h = battle.Rules.Hit;
        double expected = TacticalHitChance.Chance(new(h.Base, h.MinChance, h.MaxChance, h.RangeBonus, h.AnglePenalty, h.RearBonus, h.RearBonusArc),
            new(own.Accuracy, carrier.Maneuver, gun.HitBonus, gun.Range, gun.ArcDegrees, 90, 0, 0, false, 0, 0,
                battle.Rules.RearArc, gun.RangeBonus));
        Assert.Equal(expected, expectedCarrier.Chance);
        Assert.False(expectedCarrier.Rear);
    }

    [Fact]
    public async Task ActualForecastUsesExactIncomingAndShowsOnlyArrivedResults()
    {
        await using var fixture = await BattleNavigationTests.Fixture.Create();
        var screen = await Start(fixture);
        for (int i = 0; i < 30 && screen.Flow.State?.Phase == TacticalBattlePhase.Move; i++)
            await fixture.Route(ScreenEvent.MoveTacticalBattle);
        fixture.AdvanceAnimation(TacticalBattleMovementAnimation.FireTransitionDurationMs);
        var frame = screen.CaptureFrame(1280, 720)!;
        Assert.Equal(TacticalBattlePhase.Fire, frame.Battle.Phase);
        var forecast = TacticalBattleForecastTab.PredictFire(frame.Battle, frame.FirePlan!);
        Assert.Equal(frame.Battle.Incoming.Select(s => (s.ShooterId, s.TargetId, s.Chance, s.Rear)),
            forecast.Incoming.Select(s => (s.Shooter, s.Target, s.Chance, s.Rear)));
        Assert.NotEmpty(forecast.Incoming);
        var outgoing = forecast.Outgoing.First();
        Assert.Equal((outgoing.DamageMin + outgoing.DamageMax) / 2.0, outgoing.MeanDamage);
        Assert.Equal(outgoing.Chance * outgoing.MeanDamage, outgoing.ExpectedDamage);

        using var bitmap = new SKBitmap(1280, 720);
        using var canvas = new SKCanvas(bitmap);
        screen.Render(canvas, 1280, 720);
        var tabs = screen.PanelTabs;
        screen.OnMouseDown(tabs.Bounds.Right - tabs.Bounds.Width / 8, tabs.Bounds.MidY, MouseButton.Left);
        Assert.Equal("forecast", tabs.SelectedId);
        screen.Render(canvas, 1280, 720);
        Assert.Contains(BattleFormat.Localize("Forecast.FireNotice"), screen.ForecastTab.Text);
        Assert.Contains(BattleFormat.Localize("Forecast.Legend8"), screen.ForecastTab.Text);
        Save(bitmap);

        fixture.ManualAnimation = true;
        await fixture.Route(ScreenEvent.FireTacticalBattle);
        screen.Render(canvas, 1280, 720);
        Assert.Contains(BattleFormat.Localize("Forecast.InFlight"), screen.ForecastTab.Text);
        Assert.DoesNotContain(BattleFormat.Localize("SalvoMiss"), screen.ForecastTab.Text);
        screen.OnKeyDown(Key.Enter);
        screen.Render(canvas, 1280, 720);
        Assert.DoesNotContain(BattleFormat.Localize("Forecast.InFlight"), screen.ForecastTab.Text);
        Assert.Contains(BattleFormat.Localize("Forecast.TargetResults"), screen.ForecastTab.Text);
    }

    private static async Task<TacticalBattleScreen> Start(BattleNavigationTests.Fixture fixture)
    {
        await fixture.Route(ScreenEvent.OpenTacticalBattle);
        var setup = Assert.IsAssignableFrom<TacticalBattleModalScreen>(fixture.Screens.Current);
        await setup.Flow.OptionsTask!;
        await fixture.Route(ScreenEvent.StartTacticalBattle);
        return Assert.IsType<TacticalBattleScreen>(fixture.Screens.Current);
    }

    private static void Save(SKBitmap bitmap)
    {
        if (Environment.GetEnvironmentVariable("DSS_VALIDATION_ROOT") is not { Length: > 0 } root) return;
        using var png = bitmap.Encode(SKEncodedImageFormat.Png, 100);
        using var file = File.Create(Path.Combine(root, "battle-forecast.png"));
        png.SaveTo(file);
    }
}
