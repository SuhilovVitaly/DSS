using DeepSpaceSaga.Client.UI.Portraits;
using DeepSpaceSaga.Contracts;

namespace DeepSpaceSaga.Client.Tests.Demo;

public sealed class DemoEp0036Us0009Tk0006Tests
{
    private static LootPilotSnapshot Pilot(string suffix = "JFG71U", bool captured = true) =>
        new("battle/drone/" + suffix, "Pilot", "Images/Persons/W4/Portraits/CHR-20261117-011947-" + suffix + ".png", true, null, captured);
    [Theory]
    [InlineData("pilot")]
    [InlineData("engineer")]
    [InlineData("medic")]
    [InlineData("security")]
    [InlineData("explorer")]
    [InlineData("eva")]
    [InlineData("shorts")]
    public void AllSevenOutfits_UseRepairedV3Assemblies(string outfit)
    {
        using var portraits = new PrisonerPortraits(AppContext.BaseDirectory);
        var image = portraits.Get(Pilot(), outfit);
        Assert.NotNull(image); Assert.True(portraits.Kneeling);
        Assert.Equal(1024, image.Width); Assert.Equal(1536, image.Height);
        Assert.Same(image, portraits.Get(Pilot(), outfit));
    }
    [Fact]
    public void PendingOrRejected_DoesNotShowCapturedPose_AndSwitchPreservesIdentity()
    {
        using var portraits = new PrisonerPortraits(AppContext.BaseDirectory);
        portraits.Get(Pilot(captured: false)); Assert.False(portraits.Kneeling);
        var first = portraits.Get(Pilot())!;
        using var firstCopy = first.Copy();
        var second = portraits.Get(Pilot("SFGMKX"))!;
        Assert.True(portraits.Kneeling);
        Assert.NotEqual(firstCopy.Bytes, second.Bytes);
        portraits.Get(Pilot() with { PortraitAssetId = "../foreign.png" }); Assert.False(portraits.Kneeling);
        Assert.Null(portraits.Get(Pilot() with { PortraitAssetId = "../foreign.png" }));
    }
    [Fact]
    public void MissingOrUnpreparedIdentityNeverUsesExampleFace()
    {
        using var portraits = new PrisonerPortraits(AppContext.BaseDirectory);
        Assert.Null(portraits.Get(Pilot() with { PortraitAssetId = "Images/Persons/missing.png" }));
        Assert.False(portraits.Kneeling);
        Assert.Null(portraits.Get(Pilot() with { PortraitAssetId = null }));
    }
}
