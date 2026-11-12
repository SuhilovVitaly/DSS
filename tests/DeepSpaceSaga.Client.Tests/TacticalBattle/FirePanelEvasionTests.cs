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
        await using var scene = await Ep0019Scenes.Create("2-evasion-plan", withoutAnomalies: true);
        using var locale = Localization.UseLocaleForTests("Russian");
        using var bitmap = scene.Render();
        var frame = scene.Screen.CaptureFrame(1920, 1080)!;
        var selected = frame.FirePlan!.Selected!;
        var card = scene.Screen.Layout.Rects["pilot:" + selected.Drone.Id];
        var evade = scene.Screen.Layout.Rects["card-command:evade"];
        Assert.InRange(RasterProbe.Fraction(bitmap, SKRect.Create(card.Left + 8, card.Top + 108, 3, 22), PurpleDark), .9, 1);
        Assert.InRange(RasterProbe.Fraction(bitmap, SKRect.Create(evade.Left + 2, evade.MidY - 3, 1, 6), PurpleDark), .9, 1);
        Assert.InRange(RasterProbe.Fraction(bitmap, SKRect.Create(card.Left + 12, card.Top + 108, 115, 12), Purple), .05, 1);
        Assert.Equal("Никто не целится", PhasePanel.ThreatText(frame.Battle, selected.Drone.Id, true));
        Assert.Contains(scene.Screen.Layout.Texts, t => t.Text.StartsWith("Сознание 100 → 90", StringComparison.Ordinal));
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
        await using var scene = await Ep0019Scenes.Create("2-no-target", withoutAnomalies: true);
        using var locale = Localization.UseLocaleForTests("Russian");
        scene.Screen.OnMouseDown(30, 30, MouseButton.Left); // safe before the first Render
        using (var initial = scene.Render()) { }
        scene.Screen.OnMouseDown(30, 30, MouseButton.Left);
        using var bitmap = scene.Render();
        var card = scene.Screen.Layout.Rects["pilot:Вера"];
        var evade = scene.Screen.Layout.Rects["card-command:evade"];
        Assert.InRange(RasterProbe.Fraction(bitmap, SKRect.Create(card.Left + 8, card.Top + 108, 3, 22), Box), .9, 1);
        Assert.InRange(RasterProbe.Fraction(bitmap, SKRect.Create(evade.Left + 4, evade.Top + 3, 5, 2), EvadeIdle), .9, 1);
        Assert.Contains(scene.Screen.Layout.Texts, t => t.Text == "Уклонение: −20 к попаданию, сознание −10");
        var button = scene.Screen.CardCommandBounds("evade");
        Assert.False(button.IsEmpty);
        scene.Screen.OnMouseDown(button.MidX, button.MidY, MouseButton.Left);
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
