using System.Text.Json.Nodes;
using DeepSpaceSaga.Client.UI.Screens.TacticalBattle;

namespace DeepSpaceSaga.Client.Tests.TacticalBattle;

public sealed class SkinPreferenceTests
{
    [Theory]
    [InlineData(null, "tabletop")]
    [InlineData("classic", "classic")]
    [InlineData("contrast", "contrast")]
    [InlineData("unavailable-skin", "classic")]
    public void Client_default_uses_tabletop_and_preserves_explicit_choices(string? savedId, string expected)
    {
        using var catalog = TacticalBattleSkinCatalog.Load(Path.Combine(AppContext.BaseDirectory, "Skins"));
        Assert.Equal(expected, catalog.GetPreferred(savedId).Id);
    }

    [Fact]
    public void Selected_id_round_trips_without_rewriting_other_settings()
    {
        using var fixture = new SettingsFile();
        const string before = """
            {"typeData":{"example":"assets"},"display":{"monitor":2},"gameSettings":{"language":"Russian","unknown":[1,true,null]}}
            """;
        File.WriteAllText(fixture.Path, before);
        Assert.Null(TacticalBattleSkinPreference.Read(fixture.Path));
        Assert.True(TacticalBattleSkinPreference.TryWrite(fixture.Path, "contrast"));
        Assert.Equal("contrast", TacticalBattleSkinPreference.Read(fixture.Path));
        string serialized = File.ReadAllText(fixture.Path);
        var actual = JsonNode.Parse(serialized)!;
        actual["gameSettings"]!.AsObject().Remove("tacticalBattleSkinId");
        Assert.True(JsonNode.DeepEquals(JsonNode.Parse(before), actual));
        DateTime timestamp = File.GetLastWriteTimeUtc(fixture.Path);
        Assert.True(TacticalBattleSkinPreference.TryWrite(fixture.Path, "contrast"));
        Assert.Equal(timestamp, File.GetLastWriteTimeUtc(fixture.Path));
        Assert.Equal(serialized, File.ReadAllText(fixture.Path));
        Assert.True(TacticalBattleSkinPreference.TryWrite(fixture.Path, "classic"));
        Assert.Equal("classic", TacticalBattleSkinPreference.Read(fixture.Path));
    }

    [Fact]
    public void Missing_or_invalid_value_uses_classic_and_failed_write_preserves_file()
    {
        using var fixture = new SettingsFile();
        using var catalog = TacticalBattleSkinCatalog.Load(Path.Combine(fixture.Root, "Skins"));
        Assert.Same(TacticalBattleSkin.Classic, catalog.GetOrClassic(TacticalBattleSkinPreference.Read(fixture.Path)));
        Assert.True(TacticalBattleSkinPreference.TryWrite(fixture.Path, "contrast"));
        Assert.Same(TacticalBattleSkin.Classic, catalog.GetOrClassic(TacticalBattleSkinPreference.Read(fixture.Path)));
        List<string> warnings = [];
        foreach (string before in new[] { "{", "[]", """{"gameSettings":null}""", """{"gameSettings":42}""" })
        {
            File.WriteAllText(fixture.Path, before);
            Assert.Same(TacticalBattleSkin.Classic, catalog.GetOrClassic(TacticalBattleSkinPreference.Read(fixture.Path, warnings.Add)));
            Assert.False(TacticalBattleSkinPreference.TryWrite(fixture.Path, "contrast", warnings.Add));
            Assert.Equal(before, File.ReadAllText(fixture.Path));
            Assert.Single(Directory.EnumerateFiles(fixture.Root));
        }
        Assert.Equal(8, warnings.Count);
        File.WriteAllText(fixture.Path, """{"gameSettings":{"tacticalBattleSkinId":false}}""");
        Assert.Null(TacticalBattleSkinPreference.Read(fixture.Path));
        Assert.False(TacticalBattleSkinPreference.TryWrite(fixture.Path, "INVALID"));
        Assert.True(TacticalBattleSkinPreference.TryWrite(fixture.Path, "contrast"));
        Assert.Equal("contrast", TacticalBattleSkinPreference.Read(fixture.Path));
    }

    [Fact]
    public void Failed_atomic_replace_removes_the_staging_file()
    {
        if (!OperatingSystem.IsWindows())
            return;
        using var fixture = new SettingsFile();
        const string original = """{"gameSettings":{"tacticalBattleSkinId":"classic"}}""";
        File.WriteAllText(fixture.Path, original);
        using var denyDelete = File.Open(fixture.Path, FileMode.Open, FileAccess.Read, FileShare.Read);
        Assert.False(TacticalBattleSkinPreference.TryWrite(fixture.Path, "contrast"));
        Assert.Equal(original, File.ReadAllText(fixture.Path));
        Assert.Single(Directory.EnumerateFiles(fixture.Root));
    }

    private sealed class SettingsFile : IDisposable
    {
        internal SettingsFile()
        {
            Root = System.IO.Path.Combine(Environment.GetEnvironmentVariable("DSS_VALIDATION_ROOT") ?? System.IO.Path.GetTempPath(),
                "skin-settings-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(Root);
            Path = System.IO.Path.Combine(Root, "Settings.json");
        }

        internal string Root { get; }
        internal string Path { get; }

        public void Dispose() => Directory.Delete(Root, recursive: true);
    }
}
