using DeepSpaceSaga.Client.UI.Screens.TacticalBattle;
using DeepSpaceSaga.Client.UI.Screens;
using Silk.NET.Input;
using SkiaSharp;

namespace DeepSpaceSaga.Client.Tests.TacticalBattle.View;

[Collection("Battle localization")]
public sealed class Ep0022SkinScenesTests
{
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
    public async Task Xenon_and_Classic_share_layout_and_text(string scene)
    {
        using var catalog = TacticalBattleSkinCatalog.Load(Path.Combine(AppContext.BaseDirectory, "Skins"));
        if (!TryGetXenon(catalog, out var xenon))
            return;
        await using var scenes = await Ep0019Scenes.Create(scene);
        var frame = scenes.Frame;
        var classic = AnomalySceneSearch.Render(frame, "classic-" + scene);
        using var classicBitmap = classic.Bitmap;
        using var scope = TacticalBattleSkinRenderScope.Enter(xenon);
        var styled = AnomalySceneSearch.Render(frame, "xenon-" + scene);
        using var styledBitmap = styled.Bitmap;
        Assert.Equal(classic.Layout.Camera, styled.Layout.Camera);
        Assert.Equal(classic.Layout.Rects, styled.Layout.Rects);
        Assert.Equal(classic.Layout.Texts.Select(text => text.Text), styled.Layout.Texts.Select(text => text.Text));
        Assert.NotEqual(classicBitmap.GetPixel(540, 138), styledBitmap.GetPixel(540, 138));
        Assert.IsType<SKImage>(xenon.Image("field.hud.frame"));
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
}
