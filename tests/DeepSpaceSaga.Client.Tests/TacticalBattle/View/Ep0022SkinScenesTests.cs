using DeepSpaceSaga.Client.UI.Screens.TacticalBattle;
using SkiaSharp;

namespace DeepSpaceSaga.Client.Tests.TacticalBattle.View;

[Collection("Battle localization")]
public sealed class Ep0022SkinScenesTests
{
    [Theory]
    [InlineData("1-move-no-hover")]
    [InlineData("2-fire-hover-own")]
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
}
