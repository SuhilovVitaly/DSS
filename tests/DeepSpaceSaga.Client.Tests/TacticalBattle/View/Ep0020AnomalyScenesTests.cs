using System.Text.Json;
using DeepSpaceSaga.Client.UI.Screens.TacticalBattle;
using DeepSpaceSaga.Contracts;

namespace DeepSpaceSaga.Client.Tests.TacticalBattle.View;

[Collection("InterfaceLog")]
public sealed class Ep0020AnomalyScenesTests
{
    public static TheoryData<string, int, int> Scenes => new()
    {
        { "1-anomaly-move-dust", 1, 3 }, { "2-anomaly-absorption", 1, 4 }, { "2-anomaly-dust", 6, 5 },
        { "1-anomaly-hover", 1, 1 }, { "4-anomaly-absorbed", 1, 12 }
    };
    [Theory, MemberData(nameof(Scenes))]
    public async Task Real_scene_uses_production_handlers(string name, int seed, int turn)
    {
        await using var scene = await Ep0019Scenes.Create(name); using var locale = Localization.UseLocaleForTests("Russian");
        using var bitmap = scene.Render(); var frame = scene.Screen.CaptureFrame(1920, 1080)!;
        Assert.Equal(seed, frame.Battle.Seed); Assert.Equal(turn, scene.Screen.Flow.State!.Turn);
        if (name == "1-anomaly-move-dust") Assert.Contains(scene.Screen.Layout.Texts, t => t.Text == "Конец пути в пыли: без огня");
        if (name == "2-anomaly-absorption") { Assert.Equal(.355, frame.FirePlan!.Selected!.Target!.Absorption); Assert.Contains(scene.Screen.Layout.Texts, t => t.Text == "40% · поглощение 36%"); }
        if (name == "2-anomaly-dust") { Assert.True(frame.FirePlan!.Selected!.Drone.InDust); Assert.Contains(scene.Screen.Layout.Texts, t => t.Text == "ОГОНЬ НЕВОЗМОЖЕН"); }
        if (name == "1-anomaly-hover") Assert.Contains(scene.Screen.Layout.Texts, t => t.Text == "ПОД КУРСОРОМ · АНОМАЛИЯ");
        if (name == "4-anomaly-absorbed") { Assert.True(PilotCardModel.ResultShot(frame)!.Absorbed); Assert.Contains(scene.Screen.Layout.Texts, t => t.Text == "ПОГЛОЩЁН"); }
        Assert.DoesNotContain(scene.Screen.Layout.Texts, t => t.Text.Contains("TacticalBattle.", StringComparison.Ordinal));
        if (Environment.GetEnvironmentVariable("DSS_VALIDATION_ROOT") is { Length: > 0 } root)
            File.WriteAllText(Path.Combine(root, name + ".json"), JsonSerializer.Serialize(new { seed, turn, frame.Battle, scene.Screen.Layout.Texts }, new JsonSerializerOptions { WriteIndented = true }));
    }
}
