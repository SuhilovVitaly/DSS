using DeepSpaceSaga.Client.UI.Screens.TacticalBattle;

namespace DeepSpaceSaga.Client.Tests.TacticalBattle;

[Collection("InterfaceLog")]
public sealed class PilotPortraitCatalogTests
{
    [Fact]
    public void Loads_reference_pilots()
    {
        using var c = PilotPortraitCatalog.Load(AppContext.BaseDirectory);
        foreach (string id in new[] { "vera", "yun", "marta", "oleg" }) { Assert.True(c.TryGet(id, out var image)); Assert.Equal(1024, image.Width); }
        Assert.True(c.IsFemale("vera")); Assert.False(c.IsFemale("oleg")); Assert.False(c.TryGet("unknown", out _));
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
