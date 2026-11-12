using DeepSpaceSaga.Client.UI.Screens.TacticalBattle;
using DeepSpaceSaga.Client.UI.Screens;
using DeepSpaceSaga.Client.Tests.TacticalBattle.View;
using SkiaSharp;

namespace DeepSpaceSaga.Client.Tests.TacticalBattle;

[Collection("InterfaceLog")]
public sealed class TabletopSalvoTests
{
    [Theory]
    [InlineData("classic")]
    [InlineData("tabletop")]
    [InlineData("custom-slim")]
    public async Task Space_skips_only_tabletop_flight_and_keeps_results_visible(string skin)
    {
        using var profile = new TabletopTestProfile();
        bool tabletop = skin != "classic";
        if (skin == "custom-slim")
        {
            string manifest = Path.Combine(Path.GetDirectoryName(profile.SettingsPath)!, "Skins", "tabletop", "skin.json");
            File.WriteAllText(manifest, File.ReadAllText(manifest).Replace("\"tabletop\"", "\"custom-slim\"", StringComparison.Ordinal));
            File.WriteAllText(profile.SettingsPath, File.ReadAllText(profile.SettingsPath).Replace("tabletop", "custom-slim", StringComparison.Ordinal));
        }
        await using var scene = await Ep0019Scenes.Create("3-salvo-flight", settingsPath: tabletop ? profile.SettingsPath : null);
        Assert.Equal(skin, scene.Screen.SkinId);
        Assert.True(scene.Frame.Salvo!.InFlight);
        scene.Screen.OnKeyDown(Silk.NET.Input.Key.Space, KeyModifiers.Control, false);
        scene.Screen.OnKeyDown(Silk.NET.Input.Key.Space, KeyModifiers.None, true);
        Assert.True(scene.Frame.Salvo!.InFlight);
        var revision = scene.Frame.Battle.Revision;
        scene.Screen.OnKeyDown(Silk.NET.Input.Key.Space);
        Assert.Equal(!tabletop, scene.Frame.Salvo!.InFlight);
        Assert.Equal(revision, scene.Frame.Battle.Revision);
        scene.Screen.OnKeyDown(Silk.NET.Input.Key.Space);
        Assert.True(scene.Screen.HasSalvoPresentation);
        Assert.Equal(revision, scene.Frame.Battle.Revision);
    }

    [Theory]
    [InlineData(0, 0)]
    [InlineData(100, 1)]
    [InlineData(450, 8)]
    [InlineData(880, -1)]
    [InlineData(1000, -1)]
    public async Task Kill_explosion_frames_end_before_target_marker_appears(int age, int expectedFrame)
    {
        await using var scene = await Ep0019Scenes.Create("3-salvo-flight");
        var frame = scene.Frame;
        var original = frame.Salvo!.Flights[0];
        var flight = original with
        {
            Shot = original.Shot with { Hit = true, Killed = true, Absorbed = false },
            ElapsedMs = original.LaunchMs + original.DurationMs + age
        };
        frame = frame with
        {
            Battle = frame.Battle with { Anomalies = [] },
            Salvo = frame.Salvo with { Flights = [flight], PlaybackSkipped = false }
        };
        var layout = Draw(frame, "kill-" + age);
        var effects = layout.Rects.Keys.Where(k => k.StartsWith("explosion:", StringComparison.Ordinal)).ToArray();
        if (expectedFrame < 0) Assert.Empty(effects);
        else Assert.Equal($"explosion:{flight.Index}:boom-kill:{expectedFrame}", Assert.Single(effects));
        Assert.Equal(age >= 880, layout.Rects.ContainsKey("result-marker:" + flight.Shot.TargetId));
    }

    [Fact]
    public async Task Flying_torpedo_uses_sheet_and_effects_stay_opt_in()
    {
        await using var scene = await Ep0019Scenes.Create("3-salvo-flight");
        Assert.True(Draw(scene.Frame).Rects.ContainsKey("projectile"));
        using var catalog = TacticalBattleSkinCatalog.Load(Path.Combine(AppContext.BaseDirectory, "Skins"));
        using var bitmap = new SKBitmap(96, 96); using var canvas = new SKCanvas(bitmap);
        canvas.Clear(SKColors.Transparent);
        using (TacticalBattleSkinRenderScope.Enter(catalog.GetOrClassic("tabletop")))
        {
            Assert.True(TacticalShipSprites.DrawTorpedo(canvas, new(48, 48), 0, true));
            Assert.InRange(bitmap.Pixels.Count(p => p.Alpha > 0), 20, 1024);
            Assert.True(TacticalShipSprites.DrawEffect(canvas, "boom-hit", 9, new(48, 48)));
            Assert.False(TacticalShipSprites.DrawEffect(canvas, "boom-hit", 10, new(48, 48)));
            Assert.True(TacticalShipSprites.DrawEffect(canvas, "puff", 7, new(48, 48)));
            Assert.False(TacticalShipSprites.DrawEffect(canvas, "puff", 8, new(48, 48)));
        }
        using (TacticalBattleSkinRenderScope.Enter(TacticalBattleSkin.Classic))
        {
            Assert.False(TacticalShipSprites.DrawTorpedo(canvas, new(48, 48), 0, true));
            Assert.False(TacticalShipSprites.DrawEffect(canvas, "boom-kill", 0, new(48, 48)));
        }
    }

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
            k.StartsWith("decoy-callout:", StringComparison.Ordinal) || k.StartsWith("absorbed:", StringComparison.Ordinal) || k.StartsWith("explosion:", StringComparison.Ordinal));
    }

    private static BattleLayout Draw(TacticalBattleFrame frame, string? evidenceName = null)
    {
        using var catalog = TacticalBattleSkinCatalog.Load(Path.Combine(AppContext.BaseDirectory, "Skins"));
        using var scope = TacticalBattleSkinRenderScope.Enter(catalog.GetOrClassic("tabletop"));
        using var bitmap = new SKBitmap(1280, 720); using var canvas = new SKCanvas(bitmap);
        var layout = new BattleLayout(); BattleDesign.Recording = layout;
        try { TacticalBattleMapRenderer.Draw(canvas, frame); }
        finally { BattleDesign.Recording = null; }
        if (evidenceName is not null && Environment.GetEnvironmentVariable("DSS_VALIDATION_ROOT") is { Length: > 0 } folder)
        {
            using var png = bitmap.Encode(SKEncodedImageFormat.Png, 100);
            using var file = File.Create(Path.Combine(folder, evidenceName + ".png")); png.SaveTo(file);
        }
        return layout;
    }
}
