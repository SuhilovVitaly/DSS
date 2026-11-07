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
    public async Task Fire_panel_skin_changes_selection_and_popup_pixels_without_changing_target()
    {
        await using var fixture = await BattleNavigationTests.Fixture.Create();
        var classic = await ReachFire(fixture);
        var skin = TacticalBattleSkin.Parse("""
            {"schemaVersion":1,"id":"fire","displayName":"Fire","roles":{
              "orders.fire.card.selected":{"fill":"#634766"},
              "orders.fire.dropdown.option.active":{"fill":"#734488"}
            }}
            """);
        var themed = new TacticalBattleScreen(classic.Flow, classic.UiScale, skin);
        using var classicPixels = new SKBitmap(1280, 720);
        using var themedPixels = new SKBitmap(1280, 720);
        using var classicCanvas = new SKCanvas(classicPixels);
        using var themedCanvas = new SKCanvas(themedPixels);
        Prepare(classic, classicCanvas);
        Prepare(themed, themedCanvas);

        Assert.Equal(classic.FirePanel.Targets.PopupBounds, themed.FirePanel.Targets.PopupBounds);
        Assert.Contains(new SKColor(115, 68, 136), themedPixels.Pixels);
        Assert.DoesNotContain(new SKColor(115, 68, 136), classicPixels.Pixels);
        foreach (var screen in new[] { classic, themed })
        {
            screen.OnKeyDown(Key.Home);
            screen.OnKeyDown(Key.Enter);
            Assert.False(screen.FirePanel.Targets.IsOpen);
            var selected = screen.CaptureFrame(1280, 720)!.FirePlan!.SelectedDroneId;
            Assert.Null(screen.BuildFireRequest().Targets.Single(t => t.DroneId == selected).TargetId);
        }
        Assert.Equal(classic.BuildFireRequest().Targets.ToArray(), themed.BuildFireRequest().Targets.ToArray());

        static void Prepare(TacticalBattleScreen screen, SKCanvas canvas)
        {
            var plan = screen.CaptureFrame(1280, 720)!.FirePlan!;
            for (int i = 0; i < plan.Rows.Length && plan.Selected!.Group.List.IsEmpty; i++)
            {
                screen.OnKeyDown(Key.Tab);
                plan = screen.CaptureFrame(1280, 720)!.FirePlan!;
            }
            screen.Render(canvas, 1280, 720);
            var bounds = screen.FirePanel.Targets.Bounds;
            screen.OnMouseDown(bounds.MidX, bounds.MidY, MouseButton.Left);
            Assert.True(screen.FirePanel.Targets.IsOpen);
            screen.Render(canvas, 1280, 720);
        }
    }

    [Fact]
    public async Task RealDefaultsMapToggleAndDropdownLeadToExactEngineTarget()
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
        Assert.Null(ForShooter().TargetId);
        screen.OnMouseDown(point.X, point.Y, MouseButton.Left);
        Assert.Equal(shooter.TargetId, ForShooter().TargetId);

        var none = screen.FirePanel.NoneBounds;
        screen.OnMouseDown(none.MidX, none.MidY, MouseButton.Left);
        Assert.All(screen.BuildFireRequest().Targets, t => Assert.Null(t.TargetId));
        screen.Render(canvas, 1280, 720);
        var dropdown = screen.FirePanel.Targets;
        Assert.Null(dropdown.Items[0].Id);
        var bounds = dropdown.Bounds;
        screen.OnMouseDown(bounds.MidX, bounds.MidY, MouseButton.Left);
        Assert.True(dropdown.IsOpen);
        screen.OnKeyDown(Key.End);
        Assert.Equal(ScreenEvent.None, screen.OnKeyDown(Key.Enter));
        string chosen = dropdown.Items[^1].Id!;
        Assert.Equal(chosen, ForShooter().TargetId);
        Assert.False(dropdown.IsOpen);
        Assert.Equal(ScreenEvent.FireTacticalBattle, screen.OnKeyDown(Key.Enter));
        await fixture.Route(ScreenEvent.FireTacticalBattle);
        var resolved = screen.Flow.State!;
        var ownShot = Assert.Single(resolved.Shots.Where(s => plan.Rows.Any(row => row.Drone.Id == s.ShooterId)));
        Assert.Equal(shooter.Drone.Id, ownShot.ShooterId);
        Assert.Equal(chosen, ownShot.TargetId);
        Assert.Equal(shooter.Group.List.Single(t => t.TargetId == chosen).Chance, ownShot.Chance);

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
