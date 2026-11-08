using System.Text.Json;
using DeepSpaceSaga.Contracts;
using SkiaSharp;

namespace DeepSpaceSaga.Client.Tests.TacticalBattle.View;

[Collection("InterfaceLog")]
public sealed partial class Ep0019ViewTests
{
    public static TheoryData<string> Scenes => new() { "1-move", "1-move-no-hover", "2-fire", "2-fire-hover-own", "3-salvo-flight", "4-salvo-results", "5-outcome" };
    [Fact]
    public async Task Seed1_battle_data_matches_assets()
    {
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

}
