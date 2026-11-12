using DeepSpaceSaga.Client.UI.Screens.TacticalBattle;
using DeepSpaceSaga.Client.Tests.TacticalBattle.View;
using SkiaSharp;

namespace DeepSpaceSaga.Client.Tests.TacticalBattle;

[Collection("InterfaceLog")]
public sealed class TabletopSalvoTests
{
    [Theory]
    [InlineData(0, false, false)]
    [InlineData(300, false, false)]
    [InlineData(500, true, false)]
    [InlineData(1500, true, true)]
    public async Task Results_follow_impact_age_and_do_not_replace_floating_text_early(int age, bool ribbon, bool badge)
    {
        await using var scene = await Ep0019Scenes.Create("3-salvo-flight");
        var frame = scene.Frame;
        var original = frame.Salvo!.Flights[0];
        var flight = original with { ElapsedMs = original.LaunchMs + original.DurationMs + age };
        frame = frame with
        {
            Battle = frame.Battle with { Anomalies = [] },
            Salvo = frame.Salvo with { Flights = [flight], PlaybackSkipped = false }
        };
        var layout = Draw(frame);
        Assert.Equal(ribbon, layout.Rects.ContainsKey("result-ribbon:" + flight.Index));
        Assert.Equal(badge, layout.Rects.ContainsKey("result-badge:" + flight.Index));
        Assert.Equal(age < 1400, layout.Rects.ContainsKey("floating-shot:" + flight.Index));
    }

    [Theory]
    [InlineData("4-salvo-results")]
    [InlineData("4-decoy-results")]
    [InlineData("4-anomaly-absorbed")]
    public async Task Skipped_results_show_each_outcome_and_killed_markers(string name)
    {
        await using var scene = await Ep0019Scenes.Create(name);
        using var locale = Localization.UseLocaleForTests("Russian");
        var frame = scene.Frame;
        frame = frame with { Salvo = frame.Salvo! with { PlaybackSkipped = true } };
        var layout = Draw(frame);
        foreach (var flight in frame.Salvo!.Flights)
        {
            Assert.True(layout.Rects.ContainsKey("result-badge:" + flight.Index));
            var shot = flight.Shot;
            string text = shot.Absorbed ? "поглощён" : shot.Hit ? "−" + shot.Damage + (shot.Killed ? " · сбит" : "")
                : shot.Decoyed ? "промах · ловушка" : "промах";
            Assert.Contains(layout.Texts, t => t.Text == text);
            if (shot.Killed) Assert.True(layout.Rects.ContainsKey("result-killed:" + shot.TargetId));
        }
        Assert.DoesNotContain(layout.Rects.Keys, k => k.StartsWith("floating-shot:", StringComparison.Ordinal) ||
            k.StartsWith("decoy-callout:", StringComparison.Ordinal) || k.StartsWith("absorbed:", StringComparison.Ordinal));
    }

    private static BattleLayout Draw(TacticalBattleFrame frame)
    {
        using var catalog = TacticalBattleSkinCatalog.Load(Path.Combine(AppContext.BaseDirectory, "Skins"));
        using var scope = TacticalBattleSkinRenderScope.Enter(catalog.GetOrClassic("tabletop"));
        using var bitmap = new SKBitmap(1280, 720); using var canvas = new SKCanvas(bitmap);
        var layout = new BattleLayout(); BattleDesign.Recording = layout;
        try { TacticalBattleMapRenderer.Draw(canvas, frame); }
        finally { BattleDesign.Recording = null; }
        return layout;
    }
}
