using DeepSpaceSaga.Client.Tests.TacticalBattle.View;
using DeepSpaceSaga.Client.UI.Screens.TacticalBattle;
using SkiaSharp;

namespace DeepSpaceSaga.Client.Tests.TacticalBattle;

[Collection("InterfaceLog")]
public sealed class MapFireTests
{
    [Fact]
    public async Task Fire_sector_arrow_projectile_and_result_callout_match_spec()
    {
        await using (var s = await Ep0019Scenes.Create("2-fire"))
        {
            using var locale = Localization.UseLocaleForTests("Russian");
            using var b = s.Render(); var f = s.Screen.CaptureFrame(1920, 1080)!; var row = f.FirePlan!.Selected!;
            var a = f.View.Project(row.Drone.Pose); var target = f.Battle.Units.Single(u => u.Id == row.TargetId); var to = f.View.Project(target.Pose);
            var middle = new SKPoint((a.X + to.X) / 2, (a.Y + to.Y) / 2);
            Assert.True(RasterProbe.Fraction(b, SKRect.Create(middle.X - 1, middle.Y - 1, 2, 2), BattleDesign.Yellow) >= .6);
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
