using DeepSpaceSaga.Client.UI.Screens.TacticalBattle;
using DeepSpaceSaga.Client.UI.Screens;
using DeepSpaceSaga.Client.Tests.TacticalBattle.View;
using SkiaSharp;

namespace DeepSpaceSaga.Client.Tests.TacticalBattle;

[Collection("InterfaceLog")]
public sealed class TabletopSalvoTests
{
    [Fact]
    public async Task Completed_shot_panels_accumulate_and_clear_on_unselected_movement()
    {
        using var profile = new TabletopTestProfile();
        await using var scene = await Ep0019Scenes.Create("3-salvo-flight", settingsPath: profile.SettingsPath);
        var frame = scene.Frame;
        var ownFlights = frame.Salvo!.Flights.Where(flight => frame.Battle.Units.Any(
            unit => unit.Id == flight.Shot.ShooterId && unit.Side == frame.Battle.PlayerSide)).ToArray();
        Assert.True(ownFlights.Length >= 2);
        var first = ownFlights[0];
        var next = ownFlights[1];
        scene.Fixture.AdvanceAnimation((long)Math.Ceiling(first.LaunchMs + first.DurationMs - first.ElapsedMs) + 10);
        using (var image = scene.Render()) { }
        Assert.Contains("pilot-panel:" + first.Shot.ShooterId, scene.Screen.Layout.Rects.Keys);
        Assert.Contains("pilot-flight-progress", scene.Screen.Layout.Rects.Keys);
        var second = scene.Frame.Salvo!.Flights[next.Index];
        scene.Fixture.AdvanceAnimation((long)Math.Ceiling(second.LaunchMs + second.DurationMs - second.ElapsedMs) + 10);
        using (var image = scene.Render()) { }
        Assert.Contains("pilot-panel:" + first.Shot.ShooterId, scene.Screen.Layout.Rects.Keys);
        Assert.Contains("pilot-panel:" + next.Shot.ShooterId, scene.Screen.Layout.Rects.Keys);

        if (scene.Frame.Salvo!.InFlight) scene.Screen.OnKeyDown(Silk.NET.Input.Key.Space);
        using (var image = scene.Render()) { }
        foreach (var flight in ownFlights)
            Assert.Contains("pilot-panel:" + flight.Shot.ShooterId, scene.Screen.Layout.Rects.Keys);
        await scene.Fixture.Route(scene.Screen.OnKeyDown(Silk.NET.Input.Key.Enter));
        Assert.Null(scene.Frame.Salvo);
        Assert.NotNull(scene.Frame.Movement);
        Assert.Null(scene.Frame.SelectedDroneId);
        using (var image = scene.Render()) { }
        Assert.DoesNotContain(scene.Screen.Layout.Rects.Keys, key => key.StartsWith("pilot-panel:", StringComparison.Ordinal));
        scene.Screen.OnKeyDown(Silk.NET.Input.Key.Tab);
        Assert.NotNull(scene.Frame.SelectedDroneId);
    }

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
    public async Task Kill_explosion_finishes_without_leaving_a_target_marker(int age, int expectedFrame)
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
        Assert.DoesNotContain("result-marker:" + flight.Shot.TargetId, layout.Rects.Keys);
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
    [InlineData(-1, false)]
    [InlineData(0, true)]
    [InlineData(500, true)]
    [InlineData(2000, true)]
    public async Task Only_hovering_the_shooter_reveals_an_arrived_shot(int age, bool visible)
    {
        await using var scene = await Ep0019Scenes.Create("3-salvo-flight");
        var frame = scene.Frame;
        var original = frame.Salvo!.Flights[0];
        var flight = original with { ElapsedMs = original.LaunchMs + original.DurationMs + age };
        frame = frame with
        {
            Battle = frame.Battle with { Anomalies = [] },
            HoverUnitId = null,
            Salvo = frame.Salvo with { Flights = [flight], PlaybackSkipped = false, ShowResultCallouts = age < 1400 }
        };
        var layout = Draw(frame);
        string ribbon = "result-ribbon:" + flight.Index;
        string detail = "result-detail:" + flight.Index;
        Assert.DoesNotContain("floating-shot:" + flight.Index, layout.Rects.Keys);
        Assert.DoesNotContain(ribbon, layout.Rects.Keys);
        Assert.DoesNotContain(detail, layout.Rects.Keys);
        var other = Draw(frame with { HoverUnitId = flight.Shot.TargetId });
        Assert.DoesNotContain(ribbon, other.Rects.Keys);
        var hovered = Draw(frame with { HoverUnitId = flight.Shot.ShooterId });
        Assert.Equal(visible, hovered.Rects.ContainsKey(ribbon));
        Assert.Equal(visible, hovered.Rects.ContainsKey(detail));
        Assert.DoesNotContain("result-badge:" + flight.Index, hovered.Rects.Keys);
        Assert.DoesNotContain(ribbon, Draw(frame with { HoverUnitId = flight.Shot.ShooterId, Salvo = null }).Rects.Keys);
    }

    [Fact]
    public async Task Enemy_decoy_leaves_drone_and_intercepts_torpedo_off_axis()
    {
        using var profile = new TabletopTestProfile();
        await using var scene = await Ep0019Scenes.Create("2-fire", withoutAnomalies: true, settingsPath: profile.SettingsPath);
        scene.Fixture.ManualAnimation = true;
        await scene.Fixture.Route(ScreenEvent.FireTacticalBattle);
        var frame = scene.Frame;
        var actual = scene.Screen.Flow.State!;
        var shot = actual.Shots.First(s => s.Decoyed && !s.Hit && !s.Absorbed &&
            actual.Units.Single(u => u.Id == s.TargetId).Side != actual.PlayerSide);
        var flight = frame.Salvo!.Flights.Single(f => f.Shot.ShooterId == shot.ShooterId);
        Assert.False(flight.Shot.Decoyed);
        Assert.True(flight.InterceptedByDecoy);

        BattleLayout At(double progress, bool hit = false, bool absorbed = false, bool skipped = false, string? evidence = null) => Draw(frame with
        {
            Salvo = frame.Salvo with
            {
                InFlight = progress < 1 && !skipped,
                Flights = [flight with
                {
                    ElapsedMs = flight.LaunchMs + flight.DurationMs * progress,
                    Shot = flight.Shot with { Hit = hit, Absorbed = absorbed }
                }],
                PlaybackSkipped = skipped
            }
        }, evidence);

        string decoy = "decoy-flight:" + flight.Index, impact = $"explosion:{flight.Index}:boom-hit:0";
        var target = frame.View.Project(shot.To);
        static SKPoint Center(SKRect rect) => new(rect.MidX, rect.MidY);
        Assert.DoesNotContain(decoy, At(.2).Rects.Keys);
        Assert.InRange(SKPoint.Distance(target, Center(At(.46).Rects[decoy])), 0, 5);
        var late = At(.99, evidence: "decoy-approach");
        Assert.True(SKPoint.Distance(target, Center(late.Rects[decoy])) > 30 * frame.View.UnitScale);
        Assert.InRange(SKPoint.Distance(Center(late.Rects[decoy]), Center(late.Rects["projectile"])), 0, 12);
        var landed = At(1.001, evidence: "decoy-impact");
        Assert.DoesNotContain(decoy, landed.Rects.Keys);
        Assert.InRange(SKPoint.Distance(Center(late.Rects[decoy]), Center(landed.Rects[impact])), 0, 2);
        Assert.InRange(SKPoint.Distance(target, Center(At(1.001, hit: true).Rects[impact])), 0, 1);
        Assert.DoesNotContain(impact, At(1.001, absorbed: true).Rects.Keys);
        Assert.DoesNotContain(impact, At(1.001, skipped: true).Rects.Keys);
    }

    [Fact]
    public async Task Floating_damage_rises_after_impact_without_revealing_the_shot_line()
    {
        await using var scene = await Ep0019Scenes.Create("3-salvo-flight");
        var frame = scene.Frame;
        var hit = frame.Salvo!.Flights.First(flight => flight.Shot.Hit && !flight.Shot.Absorbed);
        string key = "floating-shot:" + hit.Index;
        BattleLayout At(int age) => Draw(frame with
        {
            HoverUnitId = null,
            Salvo = frame.Salvo with
            {
                Flights = [hit with { ElapsedMs = hit.LaunchMs + hit.DurationMs + age }],
                PlaybackSkipped = false
            }
        });
        Assert.DoesNotContain(key, At(-1).Rects.Keys);
        var impact = At(0);
        Assert.Contains(impact.Texts, entry => entry.Text.StartsWith("−", StringComparison.Ordinal));
        Assert.DoesNotContain("result-ribbon:" + hit.Index, impact.Rects.Keys);
        Assert.True(At(600).Rects[key].Top < impact.Rects[key].Top);
        Assert.DoesNotContain(key, At(1400).Rects.Keys);
    }

    [Theory]
    [InlineData("4-salvo-results")]
    [InlineData("4-decoy-results")]
    [InlineData("4-anomaly-absorbed")]
    public async Task Skipped_results_show_the_hovered_shooters_outcome_and_hide_counters_on_wrecks(string name)
    {
        await using var scene = await Ep0019Scenes.Create(name);
        using var locale = Localization.UseLocaleForTests("Russian");
        var frame = scene.Frame;
        frame = frame with { HoverUnitId = null, Salvo = frame.Salvo! with { PlaybackSkipped = true } };
        var layout = Draw(frame);
        Assert.DoesNotContain(layout.Rects.Keys, key => key.StartsWith("result-", StringComparison.Ordinal));
        foreach (var flight in frame.Salvo!.Flights)
        {
            var shot = flight.Shot;
            if (frame.Battle.Units.Any(unit => unit.Id == shot.ShooterId && unit.State != DeepSpaceSaga.Contracts.TacticalUnitState.Destroyed))
            {
                var hovered = Draw(frame with { HoverUnitId = shot.ShooterId });
                Assert.Equal("result-ribbon:" + flight.Index, Assert.Single(hovered.Rects.Keys, key =>
                    key.StartsWith("result-ribbon:", StringComparison.Ordinal)));
                string text = shot.Absorbed ? "поглощён" : shot.Hit ? "попадание" : "промах";
                Assert.Contains(hovered.Texts, entry => entry.Text.Contains(text, StringComparison.Ordinal));
                var roll = shot.Absorbed ? shot.AbsorptionRoll : shot.HitRoll;
                Assert.NotNull(roll);
                Assert.Contains(hovered.Texts, entry => entry.Text.Contains("Бросок", StringComparison.Ordinal) &&
                    entry.Text.Contains(BattleFormat.Probability(roll.Value, 2), StringComparison.Ordinal));
            }
            if (shot.Killed)
            {
                Assert.False(layout.Rects.ContainsKey("plate:armor:" + shot.TargetId));
                Assert.False(layout.Rects.ContainsKey("plate:hull:" + shot.TargetId));
            }
        }
        Assert.DoesNotContain(layout.Rects.Keys, k => k.StartsWith("floating-shot:", StringComparison.Ordinal) ||
            k.StartsWith("decoy-callout:", StringComparison.Ordinal) || k.StartsWith("absorbed:", StringComparison.Ordinal) || k.StartsWith("explosion:", StringComparison.Ordinal));
    }

    [Fact]
    public async Task Tabletop_doubles_torpedo_flight_duration()
    {
        using var profile = new TabletopTestProfile();
        await using var classic = await Ep0019Scenes.Create("3-salvo-flight");
        await using var tabletop = await Ep0019Scenes.Create("3-salvo-flight", settingsPath: profile.SettingsPath);
        var baseline = classic.Frame.Salvo!.Flights[0];
        var slowed = tabletop.Frame.Salvo!.Flights[0];
        Assert.Equal(baseline.LaunchMs, slowed.LaunchMs);
        Assert.Equal(2 * baseline.DurationMs, slowed.DurationMs);
        Assert.Equal(baseline.Progress / 2, slowed.Progress, 6);
        var last = tabletop.Frame.Salvo!.Flights[^1];
        long untilImpact = (long)Math.Ceiling(last.LaunchMs + last.DurationMs - last.ElapsedMs);
        tabletop.Fixture.AdvanceAnimation(untilImpact);
        Assert.False(tabletop.Frame.Salvo!.InFlight);
        Assert.True(tabletop.Frame.Salvo.ShowResultCallouts);
        tabletop.Fixture.AdvanceAnimation(1400);
        Assert.False(tabletop.Frame.Salvo!.ShowResultCallouts);
        Assert.True(tabletop.Screen.HasSalvoPresentation);
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
