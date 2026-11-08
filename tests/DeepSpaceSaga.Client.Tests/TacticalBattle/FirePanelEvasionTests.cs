using DeepSpaceSaga.Client.Tests.TacticalBattle.View;
using DeepSpaceSaga.Client.UI.Screens.TacticalBattle;
using Silk.NET.Input;
using SkiaSharp;
using static DeepSpaceSaga.Client.UI.Screens.TacticalBattle.BattleDesign;

namespace DeepSpaceSaga.Client.Tests.TacticalBattle;

[Collection("InterfaceLog")]
public sealed class FirePanelEvasionTests
{
    [Fact]
    public async Task Evade_plan_panel_matches_ui_spec()
    {
        await using var scene = await Ep0019Scenes.Create("2-evasion-plan");
        using var locale = Localization.UseLocaleForTests("Russian");
        using var bitmap = scene.Render();
        Assert.InRange(RasterProbe.Fraction(bitmap, new(1049.5f, 94, 1053, 108), PurpleDark), .9, 1);
        Assert.InRange(RasterProbe.Fraction(bitmap, new(1162, 244, 1264, 246), PurpleDark), .9, 1);
        Assert.InRange(RasterProbe.Fraction(bitmap, new(1050, 122, 1100, 150), Purple), .1, 1);
        var frame = scene.Screen.CaptureFrame(1920, 1080)!;
        var selected = frame.FirePlan!.Selected!;
        Assert.Equal("Таг-4: 47% → 27%", PhasePanel.ThreatText(frame.Battle, selected.Drone.Id, true));
        Assert.Contains(scene.Screen.Layout.Texts, t => t.Text == "Сознание 100 → 90");
        Assert.Equal("Уклонение", PilotCardModel.Project(frame).Single(c => c.UnitId == selected.Drone.Id).Chip);
        Save(bitmap, "evade-plan");
        var point = frame.View.Project(selected.Drone.Pose);
        int purple = 0;
        for (int i = 0; i < 16; i++)
        {
            double a = i * Math.PI / 8;
            var color = bitmap.GetPixel((int)Math.Round((point.X + 15 * Math.Cos(a)) * 1.5),
                (int)Math.Round((point.Y + 15 * Math.Sin(a)) * 1.5));
            if (Math.Abs(color.Red - Purple.Red) <= 40 && Math.Abs(color.Green - Purple.Green) <= 40
                && Math.Abs(color.Blue - Purple.Blue) <= 40) purple++;
        }
        Assert.True(purple >= 6, $"Ring probes: {purple}/16, point={point}, pixelsPerUnit={frame.View.PixelsPerUnit}");
        Save(bitmap, "evade-plan");
    }

    [Fact]
    public async Task No_target_panel_matches_ui_spec()
    {
        await using var scene = await Ep0019Scenes.Create("2-no-target");
        using var locale = Localization.UseLocaleForTests("Russian");
        scene.Screen.OnMouseDown(30, 30, MouseButton.Left); // safe before the first Render
        using (var initial = scene.Render()) { }
        scene.Screen.OnMouseDown(30, 30, MouseButton.Left);
        using var bitmap = scene.Render();
        Assert.InRange(RasterProbe.Fraction(bitmap, new(1049.5f, 94, 1053, 108), YellowDark), .9, 1);
        Assert.InRange(RasterProbe.Fraction(bitmap, new(1162, 244, 1264, 246), EvadeIdle), .9, 1);
        Assert.Contains(scene.Screen.Layout.Texts, t => t.Text == "Уклонение: −20 к попаданию, сознание −10");
        var button = scene.Screen.Layout.Rects["button:evade"];
        scene.Screen.OnMouseDown(button.MidX * 1.5f, button.MidY * 1.5f, MouseButton.Left);
        Assert.True(scene.Screen.BuildFireRequest().Targets.Single(t => t.DroneId == "Вера").Evade);
        Save(bitmap, "no-target");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    public void Evade_button_states(int state)
    {
        using var bitmap = new SKBitmap(200, 50);
        using var canvas = new SKCanvas(bitmap);
        EvadeButton(canvas, new(10, 10, 190, 40), "Evade", (EvadeButtonState)state);
        Assert.Equal(state == 0 ? Box : state == 1 ? EvadeIdle : PurpleDark, bitmap.GetPixel(15, 15));
    }

    private static void Save(SKBitmap bitmap, string name)
    {
        string root = Environment.GetEnvironmentVariable("DSS_VALIDATION_ROOT")!;
        using var encoded = bitmap.Encode(SKEncodedImageFormat.Png, 100);
        using var file = File.Create(Path.Combine(root, name + ".png"));
        encoded.SaveTo(file);
    }
}
