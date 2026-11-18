using DeepSpaceSaga.Client.UI;
using DeepSpaceSaga.Client.UI.Demo;
using DeepSpaceSaga.Contracts;
using SkiaSharp;

namespace DeepSpaceSaga.Client.Tests.Demo;

public sealed class DemoEp0036Us0011Tk0002Tests
{
    [Fact]
    public void OnlyTrackedAvailableAreasRenderWithoutCommands()
    {
        var order = new BountyOrder("bounty", "station", "target", 1000, true, BountyEligibility.TargetAvailable, SearchArea: new(0, 0, 100));
        Assert.Single(BountySearchAreaPresentation.Areas(new([order])));
        Assert.Empty(BountySearchAreaPresentation.Areas(new([order with { Tracking = false }, order with { Eligibility = BountyEligibility.Paid }])));
        using var bitmap = new SKBitmap(400, 300); using var canvas = new SKCanvas(bitmap);
        canvas.Clear(SKColors.Transparent);
        BountySearchAreaPresentation.Draw(canvas, new([order]), new CameraState(0, 0, 1), 400, 300);
        Assert.True(bitmap.GetPixel(200, 150).Alpha > 0);
        Assert.Equal(0, bitmap.GetPixel(0, 0).Alpha);
        canvas.Clear(SKColors.Transparent);
        BountySearchAreaPresentation.Draw(canvas, new([order with { Tracking = false }]), new CameraState(0, 0, 1), 400, 300);
        Assert.Equal(0, bitmap.GetPixel(200, 150).Alpha);
    }
}
