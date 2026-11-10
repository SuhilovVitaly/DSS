using DeepSpaceSaga.Client.Tests.TacticalBattle.View;
using DeepSpaceSaga.Client.UI.Screens.TacticalBattle;
using DeepSpaceSaga.Contracts;
using Silk.NET.Input;
using SkiaSharp;

namespace DeepSpaceSaga.Client.Tests.TacticalBattle;

[Collection("InterfaceLog")]
public sealed class MapFireTests
{
    [Fact]
    public async Task Damage_text_starts_on_torpedo_impact_during_the_salvo()
    {
        await using var scene = await Ep0019Scenes.Create("3-salvo-flight");
        using var locale = Localization.UseLocaleForTests("Russian");
        var hit = scene.Frame.Salvo!.Flights.First(flight => flight.Shot.Hit);
        string key = "floating-shot:" + hit.Index;
        scene.Fixture.AdvanceAnimation((long)Math.Ceiling(hit.LaunchMs + hit.DurationMs) - (long)hit.ElapsedMs - 1);
        using var before = scene.Render();
        Assert.DoesNotContain(key, scene.Screen.Layout.Rects.Keys);
        scene.Fixture.AdvanceAnimation(1);
        using var impact = scene.Render();
        Assert.True(scene.Frame.Salvo!.InFlight);
        var first = scene.Screen.Layout.Rects[key];
        var damage = scene.Frame.Battle.FireSteps.Single(step => step.ShooterId == hit.Shot.ShooterId)
            .Events.Single(entry => entry.Kind == TacticalBattleLogKind.DamageApplied).Damage!;
        Assert.Contains(scene.Screen.Layout.Texts, item => item.Text == $"−{damage.Hull + damage.Armor}");
        scene.Fixture.AdvanceAnimation(550);
        using var later = scene.Render();
        Assert.True(scene.Screen.Layout.Rects[key].Top < first.Top);
        scene.Fixture.AdvanceAnimation(900);
        using var expired = scene.Render();
        Assert.DoesNotContain(key, scene.Screen.Layout.Rects.Keys);
    }

    [Fact]
    public async Task Skipping_flight_does_not_start_impact_text_in_results()
    {
        await using var scene = await Ep0019Scenes.Create("4-salvo-results");
        using var raster = scene.Render();
        Assert.DoesNotContain(scene.Screen.Layout.Rects.Keys, key => key.StartsWith("floating-shot:", StringComparison.Ordinal));
        Assert.Contains(scene.Screen.Layout.Rects.Keys, key => key.StartsWith("decoy-callout:", StringComparison.Ordinal));
        Assert.Contains(scene.Screen.Layout.Rects.Keys, key => key.StartsWith("callout:", StringComparison.Ordinal));
        scene.Fixture.AdvanceAnimation(1500);
        using var expired = scene.Render();
        Assert.DoesNotContain(scene.Screen.Layout.Rects.Keys, key => key.StartsWith("decoy-callout:", StringComparison.Ordinal) ||
            key.StartsWith("callout:", StringComparison.Ordinal));
    }

    [Fact]
    public async Task Right_panel_can_assign_an_alternative_target()
    {
        await using var scene = await Ep0019Scenes.Create("2-fire");
        using var raster = scene.Render();
        var plan = scene.Frame.FirePlan!.Selected!;
        var alternative = plan.Group.List.First(candidate => candidate.TargetId != plan.TargetId);
        var bounds = scene.Screen.Layout.Rects["button:target:" + alternative.TargetId];
        scene.Screen.OnMouseDown(bounds.MidX * 1.5f, bounds.MidY * 1.5f, MouseButton.Left);
        Assert.Equal(alternative.TargetId, scene.Screen.BuildFireRequest().Targets.Single(order => order.DroneId == plan.Drone.Id).TargetId);
        Assert.Equal(alternative.TargetId, scene.Frame.FirePlan!.Selected!.TargetId);
    }

    [Fact]
    public async Task Alternative_target_is_visible_and_can_replace_the_planned_shot()
    {
        await using var scene = await Ep0019Scenes.Create("2-fire");
        using var locale = Localization.UseLocaleForTests("Russian");
        using var raster = scene.Render();
        var frame = scene.Frame;
        var shooter = frame.FirePlan!.Selected!;
        Assert.Equal("Юн", frame.SelectedDroneId);
        Assert.Contains(shooter.Group.List, candidate => candidate.TargetId == "Таг-6");
        Assert.Contains(shooter.Group.List, candidate => candidate.TargetId == "Таг-5");
        Assert.Contains("целей 2", PilotCardModel.Project(frame).Single(card => card.UnitId == shooter.Drone.Id).PhaseText);
        var alternative = shooter.Group.List.First(candidate => candidate.TargetId != shooter.TargetId);
        var position = TacticalBattleMapRenderer.MarkerPoint(frame.View, frame.Battle.Units.Single(unit => unit.Id == alternative.TargetId));
        var bounds = scene.Screen.Layout.Rects["candidate:" + alternative.TargetId];
        Assert.Equal(position, new SKPoint(bounds.MidX, bounds.MidY));
        Assert.DoesNotContain("candidate:" + shooter.TargetId, scene.Screen.Layout.Rects.Keys);
        Assert.Contains("chance", scene.Screen.Layout.Rects.Keys);

        scene.Screen.OnMouseDown(position.X * 1.5f, position.Y * 1.5f, MouseButton.Left);
        var order = Assert.Single(scene.Screen.BuildFireRequest().Targets.Where(target => target.DroneId == shooter.Drone.Id));
        Assert.Equal(alternative.TargetId, order.TargetId);
        using var changed = scene.Render();
        Assert.DoesNotContain("candidate:" + alternative.TargetId, scene.Screen.Layout.Rects.Keys);
        Assert.Contains("candidate:" + shooter.TargetId, scene.Screen.Layout.Rects.Keys);
    }

    [Fact]
    public async Task Fire_sector_arrow_projectile_and_result_callout_match_spec()
    {
        await using (var s = await Ep0019Scenes.Create("2-fire"))
        {
            using var locale = Localization.UseLocaleForTests("Russian");
            using var b = s.Render(); var f = s.Screen.CaptureFrame(1920, 1080)!; var row = f.FirePlan!.Selected!;
            var a = f.View.Project(row.Drone.Pose); var target = f.Battle.Units.Single(u => u.Id == row.TargetId); var to = f.View.Project(target.Pose);
            Assert.Contains(new[] { .35f, .5f, .65f }, fraction =>
            {
                var point = a + BattleDesign.Scale(to - a, fraction);
                return RasterProbe.Fraction(b, SKRect.Create(point.X - 1, point.Y - 1, 2, 2), BattleDesign.Yellow) >= .6;
            });
        }
        await using (var s = await Ep0019Scenes.Create("3-salvo-flight"))
        {
            using var locale = Localization.UseLocaleForTests("Russian");
            using var b = s.Render(); var f = s.Screen.CaptureFrame(1920, 1080)!; var flight = f.Salvo!.Flights.Single(s => s.Flying);
            Assert.InRange(flight.Progress, .619, .621); var r = s.Screen.Layout.Rects["projectile"];
            Assert.True(RasterProbe.Fraction(b, SKRect.Create(r.MidX - 1, r.MidY - 1, 2, 2), BattleDesign.Shell) >= .75);
        }
        await using (var s = await Ep0019Scenes.Create("4-salvo-results"))
        {
            using var locale = Localization.UseLocaleForTests("Russian");
            using var b = s.Render();
            Assert.Contains(s.Screen.Layout.Texts, t => t.Text == "Ловушка · промах Марта");
            Assert.True(s.Screen.Layout.Rects.ContainsKey("decoy-callout:0"));
            Assert.False(s.Screen.Layout.Rects.ContainsKey("callout:Марта"));
        }
        await using (var legacy = await Ep0019Scenes.Create("4-salvo-results", withoutModules: true))
        {
            using var locale = Localization.UseLocaleForTests("Russian");
            using var bitmap = legacy.Render();
            Assert.Contains(legacy.Screen.Layout.Texts, t => t.Text == "−41 брони");
            Assert.True(legacy.Screen.Layout.Rects.ContainsKey("callout:Марта"));
        }
    }
}
