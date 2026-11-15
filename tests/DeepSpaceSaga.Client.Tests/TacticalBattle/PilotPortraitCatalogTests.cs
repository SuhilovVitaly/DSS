using DeepSpaceSaga.Client.UI.Screens.TacticalBattle;
using DeepSpaceSaga.Client.UI.Portraits;
using SkiaSharp;

namespace DeepSpaceSaga.Client.Tests.TacticalBattle;

[Collection("InterfaceLog")]
public sealed class PilotPortraitCatalogTests
{
    [Fact]
    public void Loads_reference_pilots()
    {
        using var c = PilotPortraitCatalog.Load(AppContext.BaseDirectory);
        foreach (var (id, head, suit) in new[] { ("vera", "violet", "eva"),
                     ("yun", "crew-f12", "pilot"), ("marta", "crew-f05", "engineer") })
        {
            Assert.True(c.TryGet(id, out var image));
            Assert.Equal(new SKRect(33, 0, 267, 234), c.SourceRect(id));
            using var expected = ModularCrewPortraits.Compose(AppContext.BaseDirectory, head, suit);
            Assert.NotNull(expected);
            Assert.Equal(expected.Bytes, image.Bytes);
        }
        Assert.True(c.TryGet("oleg", out var legacy));
        Assert.Equal(1024, legacy.Width);
        Assert.Equal(new SKRect(170, 70, 854, 754), c.SourceRect("oleg"));
        Assert.True(c.IsFemale("vera"));
        Assert.False(c.TryGet("unknown", out _));
    }
    [Fact]
    public void Missing_or_broken_file_falls_back()
    {
        string root = Path.Combine(Environment.GetEnvironmentVariable("DSS_VALIDATION_ROOT")!, "broken");
        Directory.CreateDirectory(Path.Combine(root, "Data", "UI"));
        using (var c = PilotPortraitCatalog.Load(root)) Assert.False(c.TryGet("vera", out _));
        File.WriteAllText(Path.Combine(root, "Data", "UI", "tactical-battle-pilots.json"), "{");
        using (var c = PilotPortraitCatalog.Load(root)) Assert.False(c.TryGet("vera", out _));
        Directory.Delete(root, true);
    }
}
