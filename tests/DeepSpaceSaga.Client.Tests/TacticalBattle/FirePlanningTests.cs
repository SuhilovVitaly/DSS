using DeepSpaceSaga.Client.UI.Screens;
using DeepSpaceSaga.Client.UI.Screens.TacticalBattle;
using DeepSpaceSaga.Contracts;
using Silk.NET.Input;
using SkiaSharp;

namespace DeepSpaceSaga.Client.Tests.TacticalBattle;

[Collection("InterfaceLog")]
public sealed class FirePlanningTests
{
    [Fact]
    public async Task Decoy_toggle_reaches_request_and_target_choice_keeps_it()
    {
        await using var fixture = await BattleNavigationTests.Fixture.Create();
        var screen = await ReachFire(fixture);
        var orders = new TacticalBattleFireOrders();
        orders.Observe(screen.Flow.State!, null);
        var row = orders.Capture().Selected!;
        Assert.Equal(DecoyRowState.Available, row.DecoyState);
        Assert.True(orders.ToggleDecoy(row.Drone.Id));
        Assert.True(orders.BuildTargets().Single(t => t.DroneId == row.Drone.Id).Decoy);
        Assert.Equal(row.TargetId, orders.Capture().Selected!.TargetId);
        Assert.Equal(DecoyRowState.Selected, orders.Capture().Selected!.DecoyState);
        orders.Choose(null);
        Assert.True(orders.Capture().Selected!.Decoy);
        orders.Choose(row.TargetId);
        Assert.True(orders.Capture().Selected!.Decoy);
        orders.ClearAll();
        Assert.All(orders.BuildTargets(), t => Assert.False(t.Decoy || t.Evade));
        screen.OnKeyDown(Key.D);
        var frame = screen.CaptureFrame(1280, 720)!;
        Assert.True(PilotCardModel.Project(frame).Single(c => c.UnitId == frame.FirePlan!.SelectedDroneId).Decoy);
    }

    [Fact]
    public async Task Evade_clears_decoy_and_decoy_is_unavailable_while_evading()
    {
        await using var fixture = await BattleNavigationTests.Fixture.Create();
        var screen = await ReachFire(fixture);
        var orders = new TacticalBattleFireOrders();
        orders.Observe(screen.Flow.State!, null);
        string id = orders.Capture().SelectedDroneId!;
        Assert.True(orders.ToggleDecoy(id));
        Assert.True(orders.ToggleEvade(id));
        Assert.False(orders.Capture().Selected!.Decoy);
        Assert.False(orders.ToggleDecoy(id));
        Assert.True(orders.BuildTargets().Single(t => t.DroneId == id).Evade);
        Assert.True(orders.ToggleEvade(id));
        Assert.True(orders.ToggleDecoy(id));
        Assert.False(orders.Capture().Selected!.Evade);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(null)]
    public async Task Decoy_is_unavailable_without_charges(int? charges)
    {
        await using var fixture = await BattleNavigationTests.Fixture.Create();
        var screen = await ReachFire(fixture);
        var state = screen.Flow.State!;
        var orders = new TacticalBattleFireOrders();
        orders.Observe(state with { Units = [.. state.Units.Select(u => u with { DecoyCharges = charges })] }, null);
        Assert.False(orders.ToggleDecoy(orders.Selected!));
        Assert.Equal(DecoyRowState.Unavailable, orders.Capture().Selected!.DecoyState);
        Assert.All(orders.BuildTargets(), t => Assert.False(t.Decoy));
    }

    [Fact]
    public async Task Evade_toggle_reaches_request_and_choosing_target_clears_it()
    {
        await using var fixture = await BattleNavigationTests.Fixture.Create();
        var screen = await ReachFire(fixture);
        var orders = new TacticalBattleFireOrders();
        orders.Observe(screen.Flow.State!, null);
        var row = orders.Capture().Selected!;
        Assert.NotNull(row.TargetId);
        Assert.True(orders.ToggleEvade(row.Drone.Id));
        var target = orders.BuildTargets().Single(t => t.DroneId == row.Drone.Id);
        Assert.True(target.Evade);
        Assert.Null(target.TargetId);
        Assert.True(orders.Choose(row.TargetId));
        Assert.False(orders.BuildTargets().Single(t => t.DroneId == row.Drone.Id).Evade);
        Assert.Equal(row.TargetId, orders.Capture().Selected!.TargetId);
        orders.ToggleEvade(row.Drone.Id);
        orders.Choose(null);
        Assert.False(orders.Capture().Selected!.Evade);
    }

    [Fact]
    public async Task Evade_is_unavailable_below_min_consciousness()
    {
        await using var fixture = await BattleNavigationTests.Fixture.Create();
        var screen = await ReachFire(fixture);
        var state = screen.Flow.State!;
        state = state with { Units = [.. state.Units.Select(u => u with { Consciousness = 9 })] };
        var orders = new TacticalBattleFireOrders();
        orders.Observe(state, null);
        var row = orders.Capture().Selected!;
        Assert.False(row.CanEvade);
        Assert.False(orders.ToggleEvade(row.Drone.Id));
        Assert.All(orders.BuildTargets(), t => Assert.False(t.Evade));
    }

    [Fact]
    public async Task Fire_phase_without_targets_allows_evade_or_hold()
    {
        await using var fixture = await BattleNavigationTests.Fixture.Create();
        var screen = await ReachFire(fixture);
        var state = screen.Flow.State! with { Targets = [] };
        var orders = new TacticalBattleFireOrders();
        orders.Observe(state, null);
        Assert.NotEmpty(orders.Capture().Rows);
        Assert.All(orders.Capture().Rows, r => Assert.Empty(r.Group.List));
        Assert.True(orders.ToggleEvade(orders.Capture().Selected!.Drone.Id));
        Assert.Contains(orders.BuildTargets(), t => t.Evade && t.TargetId is null);
        orders.ClearAll();
        Assert.All(orders.BuildTargets(), t => { Assert.False(t.Evade); Assert.Null(t.TargetId); });
    }

    [Fact]
    public async Task FirePlanningDisplaysEngineQueueAcrossBothSides()
    {
        await using var fixture = await BattleNavigationTests.Fixture.Create();
        var screen = await ReachFire(fixture);
        var battle = screen.Flow.State! with
        {
            FireQueue = [new(0, "A80", 80), new(1, "B60", 60), new(2, "A40", 40), new(3, "B0", 0)]
        };
        string text = TacticalBattleFirePanel.QueueText(battle);
        Assert.Contains("1. A80", text);
        Assert.Contains("2. B60", text);
        Assert.Contains("3. A40", text);
        Assert.Contains("4. B0", text);
        Assert.True(text.IndexOf("B60", StringComparison.Ordinal) < text.IndexOf("A40", StringComparison.Ordinal));
        Assert.Contains(string.Format(System.Globalization.CultureInfo.InvariantCulture, BattleFormat.Localize("Initiative"), 0), text);
        // The client must preserve the supplied tie order, even if alphabetical order differs.
        Assert.StartsWith(BattleFormat.Localize("FireQueue") + "\n1. Z", TacticalBattleFirePanel.QueueText(battle with { FireQueue = [new(0, "Z", 50), new(1, "A", 50)] })
, StringComparison.Ordinal);
    }

    [Fact]
    public async Task QueueDoesNotChangeWithSelectionWindowOrSkin()
    {
        await using var fixture = await BattleNavigationTests.Fixture.Create();
        var screen = await ReachFire(fixture);
        string expected = TacticalBattleFirePanel.QueueText(screen.Flow.State!);
        using var skins = TacticalBattleSkinCatalog.Load(Path.Combine(AppContext.BaseDirectory, "Skins"));
        foreach (string skin in new[] { "classic", "contrast" })
        {
            var themed = new TacticalBattleScreen(screen.Flow, 1.5f, skins.GetOrClassic(skin));
            using var bitmap = new SKBitmap(1024, 768);
            using var canvas = new SKCanvas(bitmap);
            themed.Render(canvas, 1024, 768);
            themed.OnKeyDown(Key.Tab);
            Assert.Equal(expected, TacticalBattleFirePanel.QueueText(themed.CaptureFrame(1024, 768)!.Battle));
        }
    }



    [Fact]
    public async Task RealDefaultsMapSelectionAndHoldFireLeadToExactEngineTarget()
    {
        await using var fixture = await BattleNavigationTests.Fixture.Create();
        var screen = await ReachFire(fixture);
        var state = screen.Flow.State!;
        var plan = screen.CaptureFrame(1280, 720)!.FirePlan!;
        Assert.NotEmpty(plan.Rows);
        foreach (var row in plan.Rows)
            Assert.Equal(state.Targets.Single(g => g.DroneId == row.Drone.Id).DefaultTargetId, row.TargetId);
        var shooter = plan.Rows.First(r => r.TargetId is not null);
        for (int i = 0; i < plan.Rows.Length && plan.SelectedDroneId != shooter.Drone.Id; i++)
        {
            screen.OnKeyDown(Key.Tab);
            plan = screen.CaptureFrame(1280, 720)!.FirePlan!;
        }
        Assert.Equal(shooter.Drone.Id, plan.SelectedDroneId);
        using var bitmap = new SKBitmap(1280, 720);
        using var canvas = new SKCanvas(bitmap);
        screen.Render(canvas, 1280, 720);
        Save(bitmap);

        var target = state.Units.Single(u => u.Id == shooter.TargetId);
        var point = screen.View.Project(target.Pose);
        Assert.Equal(ScreenEvent.None, screen.OnMouseDown(point.X, point.Y, MouseButton.Left));
        Assert.Equal(shooter.TargetId, ForShooter().TargetId); // repeated target clicks are idempotent
        for (int i = 0; i < plan.Rows.Length; i++)
        {
            screen.OnMouseDown(20, 20 + 87 * i, MouseButton.Left);
            screen.Render(canvas, 1280, 720);
            var hold = screen.Layout.Rects["button:hold"];
            screen.OnMouseDown(hold.MidX, hold.MidY, MouseButton.Left);
        }
        Assert.All(screen.BuildFireRequest().Targets, t => Assert.Null(t.TargetId));
        int index = Array.FindIndex(plan.Rows.ToArray(), r => r.Drone.Id == shooter.Drone.Id);
        screen.OnMouseDown(20, 20 + 87 * index, MouseButton.Left);
        string chosen = shooter.TargetId!;
        screen.OnMouseDown(point.X, point.Y, MouseButton.Left);
        Assert.Equal(chosen, ForShooter().TargetId);
        Assert.Equal(ScreenEvent.FireTacticalBattle, screen.OnKeyDown(Key.Enter));
        await fixture.Route(ScreenEvent.FireTacticalBattle);
        var resolved = screen.Flow.State!;
        var ownShot = Assert.Single(resolved.Shots.Where(s => plan.Rows.Any(row => row.Drone.Id == s.ShooterId)));
        Assert.Equal(shooter.Drone.Id, ownShot.ShooterId);
        Assert.Equal(chosen, ownShot.TargetId);
        Assert.Equal(shooter.Group.List.Single(t => t.TargetId == chosen).Chance, ownShot.ChanceWithoutDecoy ?? ownShot.Chance);
        Assert.Equal(ownShot.Decoyed, ownShot.ChanceWithoutDecoy is not null);
        Assert.True(ownShot.Chance <= shooter.Group.List.Single(t => t.TargetId == chosen).Chance);

        TacticalFireTarget ForShooter() => screen.BuildFireRequest().Targets.Single(t => t.DroneId == shooter.Drone.Id);
    }

    [Fact]
    public async Task JammedAndEmptySectorsHaveDistinctExplanationsAndRearChanceUsesRatio()
    {
        await using var fixture = await BattleNavigationTests.Fixture.Create();
        var screen = await ReachFire(fixture);
        var battle = screen.Flow.State!;
        var row = screen.CaptureFrame(1280, 720)!.FirePlan!.Rows.First(r => r.Drone.Build is not null);
        var empty = row.Group with { List = [], DefaultTargetId = null };
        Assert.Equal(BattleFormat.Localize("FireJammed"),
            TacticalBattleFirePanel.UnavailableText(row with { Drone = row.Drone with { Jammed = true }, Group = empty }));
        var gun = row.Drone.Build!;
        var text = TacticalBattleFirePanel.UnavailableText(row with { Group = empty });
        Assert.Contains(BattleFormat.Degrees(2 * gun.ArcDegrees), text);
        Assert.Contains(BattleFormat.Number(gun.Range), text);

        var carrier = battle.Units.First(u => u.Kind == TacticalUnitKind.Carrier && u.Side != battle.PlayerSide);
        var candidate = new TacticalTargetCandidateSnapshot(carrier.Id, 100, 12.25, true, 0.25, 0.75);
        string caption = TacticalBattleFirePanel.CandidateCaption(candidate, battle);
        Assert.Contains(BattleFormat.Localize("FireCarrier"), caption);
        Assert.Contains("75%", caption);
        Assert.Contains("25%", caption);
        Assert.Contains("12.2", caption);
    }

    private static async Task<TacticalBattleScreen> ReachFire(BattleNavigationTests.Fixture fixture)
    {
        await fixture.Route(ScreenEvent.OpenTacticalBattle);
        var setup = Assert.IsAssignableFrom<TacticalBattleModalScreen>(fixture.Screens.Current);
        await setup.Flow.OptionsTask!;
        await fixture.Route(ScreenEvent.StartTacticalBattle);
        var screen = Assert.IsType<TacticalBattleScreen>(fixture.Screens.Current);
        for (int i = 0; i < 30 && screen.Flow.State?.Phase == TacticalBattlePhase.Move; i++)
            await fixture.Route(ScreenEvent.MoveTacticalBattle);
        fixture.AdvanceAnimation(TacticalBattleMovementAnimation.FireTransitionDurationMs);
        Assert.Equal(TacticalBattlePhase.Fire, screen.Flow.State?.Phase);
        return screen;
    }

    private static void Save(SKBitmap bitmap)
    {
        if (Environment.GetEnvironmentVariable("DSS_VALIDATION_ROOT") is not { Length: > 0 } root) return;
        using var png = bitmap.Encode(SKEncodedImageFormat.Png, 100);
        using var file = File.Create(Path.Combine(root, "battle-fire-planning.png"));
        png.SaveTo(file);
    }
}
