using DeepSpaceSaga.Client.Tests.TacticalBattle.View;
using DeepSpaceSaga.Client.UI.Screens.TacticalBattle;
using Silk.NET.Input;
using SkiaSharp;
using static DeepSpaceSaga.Client.UI.Screens.TacticalBattle.BattleDesign;

namespace DeepSpaceSaga.Client.Tests.TacticalBattle;

[Collection("InterfaceLog")]
public sealed class FirePanelDecoyTests
{
    [Theory]
    [InlineData("2-decoy-plan")]
    [InlineData("2-decoy-empty")]
    [InlineData("3-decoy-flight")]
    [InlineData("4-decoy-results")]
    public async Task Decoy_scenes_use_actual_engine_data(string name)
    {
        await using var scene = await Ep0019Scenes.Create(name, withoutAnomalies: true);
        using var bitmap = scene.Render();
        var frame = scene.Screen.CaptureFrame(1920, 1080)!;
        if (name == "2-decoy-empty")
        {
            Assert.Equal(0, frame.FirePlan!.Selected!.Drone.DecoyCharges);
            Assert.Equal(DecoyRowState.Unavailable, frame.FirePlan.Selected.DecoyState);
        }
        else if (name == "2-decoy-plan") Assert.True(frame.FirePlan!.Selected!.Decoy);
        else
        {
            Assert.Equal(name == "3-decoy-flight", frame.Salvo!.InFlight);
            Assert.Contains("Марта", frame.Salvo.DecoysLaunched);
        }
        Assert.DoesNotContain(scene.Screen.Layout.Texts, t => t.Text.Contains("TacticalBattle.", StringComparison.Ordinal));
    }

    [Fact]
    public async Task Shot_plan_with_decoy_matches_ui_spec()
    {
        await using var scene = await Ep0019Scenes.Create("2-fire", withoutAnomalies: true);
        using var locale = Localization.UseLocaleForTests("Russian");
        using (var idle = scene.Render())
        {
            Assert.InRange(RasterProbe.Fraction(idle, new(1049.5f, 223, 1053, 236), DecoyIdle), .9, 1);
            foreach (float x in new[] { 1249f, 1258f })
                Assert.InRange(RasterProbe.Fraction(idle, SKRect.Create(x - 1.5f, 228, 3, 3), Green), .5, 1);
        }
        scene.Screen.OnKeyDown(Key.D);
        using var bitmap = scene.Render();
        var frame = scene.Screen.CaptureFrame(1920, 1080)!;
        var row = frame.FirePlan!.Selected!;
        Assert.InRange(RasterProbe.Fraction(bitmap, new(1049.5f, 223, 1053, 236), GreenDark), .9, 1);
        Assert.InRange(RasterProbe.Fraction(bitmap, SKRect.Create(1247.5f, 228, 3, 3), Green), .5, 1);
        Assert.InRange(RasterProbe.Fraction(bitmap, SKRect.Create(1256.5f, 228, 3, 3), GreenDark), .5, 1);
        var incoming = frame.Battle.Incoming.SingleOrDefault(i => i.TargetId == row.Drone.Id && i.ChanceIfDecoy is not null);
        string expected = incoming is null ? M("DecoyArmedNoThreat") : M("DecoyArmed",
            PilotCardModel.Name(frame.Battle.Units.Single(u => u.Id == incoming.ShooterId)),
            Pct(incoming.Chance), Pct(incoming.ChanceIfDecoy!.Value));
        Assert.Equal(expected, PhasePanel.DecoyText(frame.Battle, row));
        Assert.Contains(scene.Screen.Layout.Texts, t => t.Text == expected);
        Assert.Contains(scene.Screen.Layout.Texts, t => t.Text.EndsWith(" · 1 ловушка", StringComparison.Ordinal));
        var r = scene.Screen.CardCommandBounds("decoy");
        Assert.False(r.IsEmpty);
        scene.Screen.OnMouseDown(r.MidX, r.MidY, MouseButton.Left);
        Assert.False(scene.Screen.BuildFireRequest().Targets.Single(t => t.DroneId == row.Drone.Id).Decoy);
        Save(bitmap, "decoy-plan");
    }

    [Fact]
    public async Task No_target_with_decoy_matches_ui_spec()
    {
        await using var scene = await Ep0019Scenes.Create("2-no-target", withoutAnomalies: true);
        using var locale = Localization.UseLocaleForTests("Russian");
        var frame = scene.Screen.CaptureFrame(1920, 1080)!;
        Assert.Empty(frame.FirePlan!.Selected!.Group.List);
        using var bitmap = scene.Render();
        Assert.InRange(RasterProbe.Fraction(bitmap, new(1049.5f, 224, 1053, 236), DecoyIdle), .9, 1);
        Assert.Equal(new SKRect(1048, 222, 1266, 238), scene.Screen.Layout.Rects["button:decoy"]);
        scene.Screen.OnKeyDown(Key.D);
        using var armed = scene.Render();
        frame = scene.Screen.CaptureFrame(1920, 1080)!;
        Assert.True(frame.FirePlan!.Selected!.Decoy);
        Assert.Contains(scene.Screen.Layout.Texts, t => t.Text == PhasePanel.ThreatText(frame.Battle,
            frame.FirePlan.Selected.Drone.Id, false, true));
        Save(armed, "decoy-no-target");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    public void Decoy_row_states(int state)
    {
        using var bitmap = new SKBitmap(300, 60);
        using var canvas = new SKCanvas(bitmap);
        DecoyRow(canvas, new(10, 10, 290, 40), "Decoy", state == 0 ? 0 : 2, 2, (DecoyRowState)state);
        Assert.Equal(state == 0 ? Box : state == 1 ? DecoyIdle : GreenDark, bitmap.GetPixel(15, 15));
    }

    [Fact]
    public async Task Card_and_map_decoy_marks()
    {
        await using var scene = await Ep0019Scenes.Create("2-fire", withoutAnomalies: true);
        scene.Screen.OnKeyDown(Key.D);
        using var bitmap = scene.Render();
        var frame = scene.Screen.CaptureFrame(1920, 1080)!;
        var unit = frame.FirePlan!.Selected!.Drone;
        Assert.True(PilotCardModel.Project(frame).Single(c => c.UnitId == unit.Id).Decoy);
        var q = DecoyPoint(frame.View.Project(unit.Pose), unit.Pose.Course);
        Assert.True(Count(bitmap, SKRect.Create(q.X - 4, q.Y - 4, 8, 8), Green) >= 6);
        var icon = scene.Screen.Layout.Rects["pilot-decoy:" + unit.Id];
        Assert.Equal(13, icon.Height);
        Assert.Equal(13, icon.Width);
        Assert.True(Count(bitmap, icon, Green) >= 4);
        Assert.DoesNotContain(scene.Screen.Layout.Rects.Keys, k => k.StartsWith("decoy:Таг", StringComparison.Ordinal));
        scene.Screen.OnKeyDown(Key.E);
        using var evading = scene.Render();
        Assert.False(scene.Screen.Layout.Rects.ContainsKey("button:decoy"));
        Assert.False(scene.Screen.Layout.Rects.ContainsKey("pilot-decoy:" + unit.Id));
        Assert.False(scene.Screen.Layout.Rects.ContainsKey("decoy:" + unit.Id));
    }

    [Fact]
    public async Task Empty_charges_panel_shows_disabled_row()
    {
        await using var scene = await Ep0019Scenes.Create("2-fire", withoutAnomalies: true);
        using var locale = Localization.UseLocaleForTests("Russian");
        var frame = scene.Screen.CaptureFrame(1920, 1080)!;
        var row = frame.FirePlan!.Selected!;
        row = row with { Drone = row.Drone with { DecoyCharges = 0 } };
        frame = frame with { FirePlan = new([row], row.Drone.Id) };
        using var bitmap = new SKBitmap(1920, 1080);
        using var canvas = new SKCanvas(bitmap);
        canvas.Scale(1.5f);
        var layout = new BattleLayout();
        Recording = layout;
        try { new PhasePanel().Draw(canvas, frame, layout); }
        finally { Recording = null; }
        Assert.Equal(M("DecoyEmpty"), PhasePanel.DecoyText(frame.Battle, row));
        Assert.Contains(layout.Texts, t => t.Text == "Ловушек нет");
        Assert.InRange(RasterProbe.Fraction(bitmap, new(1049.5f, 223, 1053, 236), Box), .9, 1);
    }

    [Theory]
    [InlineData("Russian")]
    [InlineData("English")]
    public async Task Decoy_texts_have_no_resource_fallback(string language)
    {
        await using var scene = await Ep0019Scenes.Create("2-fire", withoutAnomalies: true);
        using var locale = Localization.UseLocaleForTests(language);
        scene.Screen.OnKeyDown(Key.D);
        using var bitmap = scene.Render();
        Assert.DoesNotContain(scene.Screen.Layout.Texts, t => t.Text.Contains("TacticalBattle.", StringComparison.Ordinal));
    }

    [Fact]
    public async Task Armed_row_uses_first_incoming_from_real_later_turn()
    {
        await using var scene = await Ep0019Scenes.Create("2-fire", withoutAnomalies: true);
        for (int step = 0; step < 24; step++)
        {
            var frame = scene.Screen.CaptureFrame(1920, 1080)!;
            var incoming = frame.Battle.Incoming.FirstOrDefault(i => i.ChanceIfDecoy is not null);
            if (frame.FirePlan is { } plan && incoming is not null)
            {
                for (int i = 0; i < plan.Rows.Length && scene.Screen.CaptureFrame(1920, 1080)!.FirePlan!.SelectedDroneId != incoming.TargetId; i++)
                    scene.Screen.OnKeyDown(Key.Tab);
                using var locale = Localization.UseLocaleForTests("Russian");
                scene.Screen.OnKeyDown(Key.D);
                using var bitmap = scene.Render();
                frame = scene.Screen.CaptureFrame(1920, 1080)!;
                string expected = M("DecoyArmed", PilotCardModel.Name(frame.Battle.Units.Single(u => u.Id == incoming.ShooterId)),
                    Pct(incoming.Chance), Pct(incoming.ChanceIfDecoy!.Value));
                Assert.Equal(expected, PhasePanel.DecoyText(frame.Battle, frame.FirePlan!.Selected!));
                Assert.Contains(scene.Screen.Layout.Texts, t => t.Text == expected);
                File.WriteAllText(Path.Combine(Environment.GetEnvironmentVariable("DSS_VALIDATION_ROOT")!, "threat-turn.txt"), frame.Battle.Turn.ToString());
                Save(bitmap, "decoy-threat");
                return;
            }
            Assert.NotEqual(DeepSpaceSaga.Contracts.TacticalBattlePhase.Over, frame.Battle.Phase);
            await scene.Fixture.Route(frame.Battle.Phase == DeepSpaceSaga.Contracts.TacticalBattlePhase.Fire
                ? DeepSpaceSaga.Client.UI.Screens.ScreenEvent.FireTacticalBattle : DeepSpaceSaga.Client.UI.Screens.ScreenEvent.MoveTacticalBattle);
            scene.Fixture.FinishPresentation();
        }
        Assert.Fail("No incoming drone shot within24 real commands.");
    }

    private static int Count(SKBitmap bitmap, SKRect area, SKColor color)
    {
        int count = 0;
        for (int y = (int)Math.Ceiling(area.Top * 1.5f); y < area.Bottom * 1.5f; y++)
            for (int x = (int)Math.Ceiling(area.Left * 1.5f); x < area.Right * 1.5f; x++)
            {
                var pixel = bitmap.GetPixel(x, y);
                if (Math.Abs(pixel.Red - color.Red) <= 40 && Math.Abs(pixel.Green - color.Green) <= 40
                    && Math.Abs(pixel.Blue - color.Blue) <= 40) count++;
            }
        return count;
    }
    private static void Save(SKBitmap bitmap, string name)
    {
        using var encoded = bitmap.Encode(SKEncodedImageFormat.Png, 100);
        File.WriteAllBytes(Path.Combine(Environment.GetEnvironmentVariable("DSS_VALIDATION_ROOT")!, name + ".png"), encoded.ToArray());
    }
}
