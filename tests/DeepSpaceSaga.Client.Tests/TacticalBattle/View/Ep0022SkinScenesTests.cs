using DeepSpaceSaga.Client.UI.Screens.TacticalBattle;
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
    public async Task Xenon_and_Classic_share_layout_and_text(string scene)
    {
        if (Environment.GetEnvironmentVariable("DSS_EXPECT_XENON_STAR") != "1")
            return;

        using var catalog = TacticalBattleSkinCatalog.Load(Path.Combine(AppContext.BaseDirectory, "Skins"));
        Assert.True(catalog.TryGet("xenon-star", out var xenon));
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
    public void Segmented_meter_clips_texture_to_the_value()
    {
        if (Environment.GetEnvironmentVariable("DSS_EXPECT_XENON_STAR") != "1")
            return;

        using var catalog = TacticalBattleSkinCatalog.Load(Path.Combine(AppContext.BaseDirectory, "Skins"));
        Assert.True(catalog.TryGet("xenon-star", out var xenon));
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
}
