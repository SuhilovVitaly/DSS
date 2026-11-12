using DeepSpaceSaga.Client.UI.Screens.TacticalBattle;
using DeepSpaceSaga.Client.UI.Screens;
using Silk.NET.Input;
using SkiaSharp;

namespace DeepSpaceSaga.Client.Tests.TacticalBattle.View;

[Collection("Battle localization")]
public sealed class Ep0022SkinScenesTests
{
    [Fact]
    public async Task Xenon_map_has_black_space_and_classic_grid()
    {
        using var catalog = TacticalBattleSkinCatalog.Load(Path.Combine(AppContext.BaseDirectory, "Skins"));
        if (!TryGetXenon(catalog, out var xenon))
            return;

        await using var scenes = await Ep0019Scenes.Create("1-move-no-hover");
        var frame = scenes.Frame with { Battle = scenes.Frame.Battle with { Units = [], Anomalies = [] } };
        using var scope = TacticalBattleSkinRenderScope.Enter(xenon);
        using var bitmap = new SKBitmap(frame.View.PixelWidth, frame.View.PixelHeight);
        using var canvas = new SKCanvas(bitmap);
        TacticalBattleMapRenderer.Draw(canvas, frame);

        Assert.Null(xenon.Image("field.atmosphere"));
        Assert.Equal(BattleDesign.Grid, TacticalBattleSkinRenderScope.Color("field.grid", "stroke", BattleDesign.Grid));
        Assert.Equal(SKColors.Black, bitmap.GetPixel(100, 12));
        Assert.NotEqual(SKColors.Black, bitmap.GetPixel(104, 12));
    }

    [Theory]
    [InlineData("1-move-no-hover")]
    [InlineData("1-anomaly-move-dust")]
    [InlineData("2-fire-hover-own")]
    [InlineData("2-build-damaged")]
    [InlineData("2-no-target")]
    [InlineData("2-anomaly-absorption")]
    [InlineData("2-evasion-plan")]
    [InlineData("2-decoy-empty")]
    [InlineData("3-salvo-flight")]
    [InlineData("4-salvo-results")]
    [InlineData("4-anomaly-absorbed")]
    [InlineData("5-outcome")]
    public async Task Shipped_skins_share_card_and_phase_geometry_at_both_scales(string scene)
    {
        using var catalog = TacticalBattleSkinCatalog.Load(Path.Combine(AppContext.BaseDirectory, "Skins"));
        Assert.True(catalog.TryGet("contrast", out var contrast));
        var skins = new List<TacticalBattleSkin> { contrast };
        Assert.True(catalog.TryGet("tabletop", out var tabletop));
        skins.Add(tabletop);
        if (TryGetXenon(catalog, out var xenon))
        {
            skins.Add(xenon);
            Assert.IsType<SKImage>(xenon.Image("field.hud.frame"));
        }

        await using var scenes = await Ep0019Scenes.Create(scene);
        using var locale = Localization.UseLocaleForTests("Russian");
        var frame = scenes.Frame;
        foreach (float scale in new[] { 1f, 1.5f })
        {
            var classic = AnomalySceneSearch.Render(frame, $"classic-{scene}-{scale}", scale);
            using var classicBitmap = classic.Bitmap;
            foreach (var skin in skins)
            {
                using var scope = TacticalBattleSkinRenderScope.Enter(skin);
                var styled = AnomalySceneSearch.Render(frame, $"{skin.Id}-{scene}-{scale}", scale);
                using var styledBitmap = styled.Bitmap;
                Assert.Equal(classic.Layout.Camera, styled.Layout.Camera);
                if (skin.Id == "tabletop")
                {
                    var originalWindows = Windows(TacticalBattleSkin.Classic);
                    var styledWindows = Windows(skin);
                    Assert.Equal(originalWindows.Rects, styledWindows.Rects);
                    Assert.Equal(originalWindows.Texts.Select(t => t.Text), styledWindows.Texts.Select(t => t.Text));
                }
                else
                {
                    Assert.Equal(classic.Layout.Rects, styled.Layout.Rects);
                    Assert.Equal(classic.Layout.Texts.Select(text => text.Text), styled.Layout.Texts.Select(text => text.Text));
                }
                Assert.NotEqual(classicBitmap.GetPixel((int)(360 * scale), (int)(92 * scale)),
                    styledBitmap.GetPixel((int)(360 * scale), (int)(92 * scale)));
                Assert.Equal(PhasePanel.Bottom(frame.Battle.Phase, frame.Salvo), styled.Layout.Rects["phase"].Bottom);

                if (skin.Id == "xenon-star" && scene == "2-fire-hover-own")
                {
                    var targetTone = SKColor.Parse("#BEDDEA");
                    Assert.Contains(styled.Layout.Texts, text => text.Text == "Таг-6" && text.Color == targetTone);
                    Assert.Contains(styled.Layout.Texts, text => text.Text == "73%" && text.Color == targetTone);
                }

                var selected = PilotCardModel.Project(frame).SingleOrDefault(card => card.Selected);
                if (selected is null) continue;
                var card = styled.Layout.Rects["pilot:" + selected.UnitId];
                if (skin.Id == "tabletop")
                {
                    Assert.Equal((150, 40), (card.Width, card.Height));
                    Assert.Equal(150, styled.Layout.Rects["pilot-panel:" + selected.UnitId].Width);
                    continue;
                }
                Assert.True(card.Height > 82);
                for (int i = 0; i < selected.Stats.Count; i++)
                {
                    if (selected.Stats[i].ValueTone is not null) continue;
                    float x = card.Left + 129 + i * 22;
                    Assert.True(HasReadableValue(styledBitmap, scale, x, card.Top),
                        $"{skin.Id}/{scene}/{scale}: unreadable {selected.Stats[i].Kind} value");
                }
            }
        }

        BattleLayout Windows(TacticalBattleSkin skin)
        {
            using var scope = TacticalBattleSkinRenderScope.Enter(skin);
            using var bitmap = new SKBitmap(1280, 720); using var canvas = new SKCanvas(bitmap);
            var layout = new BattleLayout(); BattleDesign.Recording = layout;
            try
            {
                new PhasePanel().Draw(canvas, frame, layout);
                new HoverCard().Draw(canvas, frame, PilotPortraitCatalog.Default);
            }
            finally { BattleDesign.Recording = null; }
            return layout;
        }
    }

    private static bool HasReadableValue(SKBitmap bitmap, float scale, float center, float top)
    {
        int bright = 0;
        for (int y = (int)((top + 5) * scale); y < (top + 13) * scale; y++)
            for (int x = (int)((center - 8) * scale); x < (center + 8) * scale; x++)
            {
                var pixel = bitmap.GetPixel(x, y);
                if (pixel.Red >= 180 && pixel.Green >= 180 && pixel.Blue >= 180)
                    bright++;
            }

        var background = bitmap.GetPixel((int)((center + 9) * scale), (int)((top + 9) * scale));
        return bright >= 3 && background.Green < 140;
    }

    [Fact]
    public async Task Exit_confirmation_keeps_the_same_actions_at_compact_size()
    {
        using var catalog = TacticalBattleSkinCatalog.Load(Path.Combine(AppContext.BaseDirectory, "Skins"));
        if (!TryGetXenon(catalog, out var xenon))
            return;
        await using var scenes = await Ep0019Scenes.Create("1-move-no-hover");
        var classic = scenes.Screen;
        var themed = new TacticalBattleScreen(classic.Flow, 1, xenon);
        using var first = RenderConfirmation(classic);
        using var second = RenderConfirmation(themed);
        Assert.NotEqual(first.GetPixel(640, 365), second.GetPixel(640, 365));
        Assert.Equal(ScreenEvent.CloseTacticalBattle, classic.OnKeyDown(Key.Enter));
        Assert.Equal(ScreenEvent.CloseTacticalBattle, themed.OnKeyDown(Key.Enter));

        static SKBitmap RenderConfirmation(TacticalBattleScreen screen)
        {
            Assert.Equal(ScreenEvent.None, screen.OnKeyDown(Key.Escape));
            Assert.True(screen.ConfirmExit);
            var bitmap = new SKBitmap(1280, 720);
            using var canvas = new SKCanvas(bitmap);
            screen.Render(canvas, bitmap.Width, bitmap.Height);
            return bitmap;
        }
    }

    [Fact]
    public void Segmented_meter_clips_texture_to_the_value()
    {
        using var catalog = TacticalBattleSkinCatalog.Load(Path.Combine(AppContext.BaseDirectory, "Skins"));
        if (!TryGetXenon(catalog, out var xenon))
            return;
        using var scope = TacticalBattleSkinRenderScope.Enter(xenon);
        using var bitmap = new SKBitmap(110, 42);
        using var canvas = new SKCanvas(bitmap);
        BattleDesign.Bar(canvas, 5, 2, 100, 8, 0, BattleDesign.Cyan);
        BattleDesign.Bar(canvas, 5, 15, 100, 8, .4, BattleDesign.Cyan);
        BattleDesign.Bar(canvas, 5, 28, 100, 8, 1, BattleDesign.Cyan);
        Assert.Equal(bitmap.GetPixel(25, 6), bitmap.GetPixel(75, 6));
        Assert.Equal(bitmap.GetPixel(75, 6), bitmap.GetPixel(75, 19));
        Assert.NotEqual(bitmap.GetPixel(25, 6), bitmap.GetPixel(25, 19));
        Assert.NotEqual(bitmap.GetPixel(75, 6), bitmap.GetPixel(75, 32));
    }

    private static bool TryGetXenon(TacticalBattleSkinCatalog catalog, out TacticalBattleSkin skin)
    {
        bool installed = catalog.TryGet("xenon-star", out skin);
        if (!installed)
            Assert.NotEqual("1", Environment.GetEnvironmentVariable("DSS_EXPECT_XENON_STAR"));
        return installed;
    }

    [Fact]
    public void Pilot_meters_do_not_inherit_the_dark_panel_fill()
    {
        using var catalog = TacticalBattleSkinCatalog.Load(Path.Combine(AppContext.BaseDirectory, "Skins"));
        if (!TryGetXenon(catalog, out var xenon))
            return;

        using var scope = TacticalBattleSkinRenderScope.Enter(xenon);
        using var bitmap = new SKBitmap(202, 92);
        using var canvas = new SKCanvas(bitmap);
        var card = new PilotCard("pilot", "Pilot", "", "", false,
            [
                new("armor", "Armor", "80/80", BattleDesign.Purple, 1),
                new("hull", "Hull", "60/60", BattleDesign.Cyan, 1),
                new("mind", "Mind", "100", BattleDesign.Green, 1),
            ], "", false, []);
        new PilotCardColumn().Draw(canvas, [card], PilotPortraitCatalog.Default);
        SKColor[] fills = [bitmap.GetPixel(135, 50), bitmap.GetPixel(157, 50), bitmap.GetPixel(179, 50)];
        Assert.All(fills, fill => Assert.True(Math.Max(fill.Green, fill.Blue) >= 140, $"Unclear meter fill {fill}"));
        Assert.Equal(3, fills.Distinct().Count());
    }

    [Fact]
    public void User_pack_panel_color_does_not_leak_into_gauge_defaults()
    {
        var skin = TacticalBattleSkin.Parse("""
            {"schemaVersion":1,"id":"local-test","displayName":"Local test","roles":{
                "ui.pilot-card":{"fill":"#382040","stroke":"#FF9922"},
                "ui.pilot-card.gauge.track":{"fill":"#191F28"},
                "ui.pilot-card.icon.mind":{"stroke":"#F0A0EF"}
            }}
            """);
        using var scope = TacticalBattleSkinRenderScope.Enter(skin);
        using var bitmap = new SKBitmap(202, 92);
        using var canvas = new SKCanvas(bitmap);
        var card = new PilotCard("pilot", "Pilot", "", "", false,
            [
                new("armor", "Armor", "40/80", BattleDesign.Purple, .5),
                new("hull", "Hull", "30/60", BattleDesign.Cyan, .5),
                new("mind", "Mind", "50", BattleDesign.Green, .5),
            ], "", false, []);
        new PilotCardColumn().Draw(canvas, [card], PilotPortraitCatalog.Default);
        Assert.Equal(SKColor.Parse("#191F28"), bitmap.GetPixel(135, 29));
        Assert.Equal(BattleDesign.Purple, bitmap.GetPixel(135, 53));
        Assert.Equal(BattleDesign.Cyan, bitmap.GetPixel(157, 53));
        Assert.Equal(BattleDesign.Green, bitmap.GetPixel(179, 53));
        Assert.Equal(SKColor.Parse("#F0A0EF"), bitmap.GetPixel(179, 71));
    }
}
