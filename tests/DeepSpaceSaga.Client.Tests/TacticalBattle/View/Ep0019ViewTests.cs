using System.Text.Json;
using DeepSpaceSaga.Contracts;
using SkiaSharp;

namespace DeepSpaceSaga.Client.Tests.TacticalBattle.View;

[Collection("InterfaceLog")]
public sealed class Ep0019ViewTests
{
    public static TheoryData<string> Scenes => new() { "1-move", "1-move-no-hover", "2-fire", "2-fire-hover-own", "3-salvo-flight", "4-salvo-results", "5-outcome" };
    [Fact]
    public async Task Seed1_battle_data_matches_assets()
    {
        using var locale = Localization.UseLocaleForTests("Russian");
        var states = new List<TacticalBattleSnapshot>();
        foreach (string name in new[] { "1-move", "2-fire", "4-salvo-results", "5-outcome" })
        {
            await using var scene = await Ep0019Scenes.Create(name);
            var state = scene.Screen.Flow.State!;
            states.Add(state with { BattleId = "seed1" });
            if (name == "1-move")
            {
                Assert.Equal(1, state.Turn);
                Assert.Equal(4, state.Suggestions.Length);
                Assert.InRange(state.Suggestions[0].Speed, 77.45, 77.55);
                Assert.InRange(state.Suggestions[0].Turn, 5.45, 5.55);
            }
            if (name == "2-fire")
            {
                var frame = scene.Screen.CaptureFrame(1920, 1080)!;
                Assert.Equal("Марта", frame.FirePlan!.Selected!.Drone.Build!.PilotName);
                Assert.InRange(frame.FirePlan.Selected.Target!.Chance, 0.555, 0.565);
                Assert.InRange(frame.FirePlan.Selected.Target.Distance, 108.05, 108.15);
            }
            if (name == "4-salvo-results")
            {
                Assert.Equal(3, state.Shots.Length);
                Assert.Single(state.Shots.Where(s => s.Hit));
                Assert.Equal(39, state.Units.Single(u => u.Build?.PilotName == "Марта").Armor);
            }
            if (name == "5-outcome")
            {
                Assert.Equal(20, state.Outcome!.Turns);
                Assert.Equal(TacticalBattleOutcomeCode.A, state.Outcome.Kind);
                Assert.Equal(30, state.Outcome.Statistics.Shots);
                Assert.Equal(18, state.Outcome.Statistics.Hits);
            }
        }
        string json = JsonSerializer.Serialize(states, new JsonSerializerOptions { WriteIndented = true });
        if (Environment.GetEnvironmentVariable("DSS_EP0019_CAPTURE_REFERENCE") == "1")
            File.WriteAllText(Path.Combine(Environment.GetEnvironmentVariable("DSS_VALIDATION_ROOT")!, "battle-data-seed1.txt"), json);
        else
            Assert.Equal(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "battle-data-seed1.txt")).Replace("\r\n", "\n"), json.Replace("\r\n", "\n"));
    }

    [Theory(Skip = "Enabled by EP-0019-US-0006-TK-0002"), MemberData(nameof(Scenes)), Trait("Category", "Ep0019View")]
    public async Task Scene_layout_matches_design_spec(string name)
    {
        await using var scene = await Ep0019Scenes.Create(name);
        using var bitmap = scene.Render();
        Assert.NotEmpty(scene.Screen.Layout.Rects);
    }

    [Theory(Skip = "Enabled by EP-0019-US-0006-TK-0002"), MemberData(nameof(Scenes)), Trait("Category", "Ep0019View")]
    public async Task Scene_texts_match_design_spec(string name)
    {
        using var locale = Localization.UseLocaleForTests("Russian");
        await using var scene = await Ep0019Scenes.Create(name);
        using var bitmap = scene.Render();
        Assert.Contains(scene.Screen.Layout.Texts, t => t.Text.StartsWith("ХОД ", StringComparison.Ordinal));
    }

    [Theory(Skip = "Enabled by EP-0019-US-0006-TK-0002"), MemberData(nameof(Scenes)), Trait("Category", "Ep0019View")]
    public async Task Scene_probes_pass(string name)
    {
        await using var scene = await Ep0019Scenes.Create(name);
        using var bitmap = scene.Render();
        Assert.True(RasterProbe.Fraction(bitmap, new(1046, 62, 1270, 66), new SKColor(14, 27, 31)) >= 0.9);
    }
}
