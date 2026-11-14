using System.Text.Json.Nodes;
using DeepSpaceSaga.Client.UI.Screens.Tutorial;

namespace DeepSpaceSaga.Client.Tests.Tutorial;

public sealed class TutorialHintsPreferenceTests
{
    [Fact]
    public void MissingSettingDefaultsToEnabled()
    {
        using var fixture = new SettingsFile();
        List<string> warnings = [];
        Assert.True(TutorialHintsPreference.Read(fixture.Path, warnings.Add)); // no file
        File.WriteAllText(fixture.Path, "{}");
        Assert.True(TutorialHintsPreference.Read(fixture.Path, warnings.Add)); // no group
        File.WriteAllText(fixture.Path, """{"gameSettings":{"language":"Russian"}}""");
        Assert.True(TutorialHintsPreference.Read(fixture.Path, warnings.Add)); // no property
        Assert.Empty(warnings);
        File.WriteAllText(fixture.Path, """{"gameSettings":{"tutorialHints":false}}""");
        Assert.False(TutorialHintsPreference.Read(fixture.Path, warnings.Add));
        File.WriteAllText(fixture.Path, """{"gameSettings":{"tutorialHints":true}}""");
        Assert.True(TutorialHintsPreference.Read(fixture.Path, warnings.Add));
        Assert.Empty(warnings);
        Assert.Equal("tutorialHints", TutorialHintsPreference.Property);
    }

    [Fact]
    public void WritePreservesUnrelatedSettingsAndIsReadBack()
    {
        using var fixture = new SettingsFile();
        const string before = """
            {"typeData":{"example":"assets"},"display":{"monitor":2},"gameSettings":{"language":"Russian","uiScale":1.25,"tacticalBattleSkinId":"classic","tacticalMap":{"zoom":2},"unknown":[1,true,null]}}
            """;
        File.WriteAllText(fixture.Path, before);
        Assert.True(TutorialHintsPreference.TryWrite(fixture.Path, false));
        Assert.False(TutorialHintsPreference.Read(fixture.Path));
        string serialized = File.ReadAllText(fixture.Path);
        var actual = JsonNode.Parse(serialized)!;
        Assert.False(actual["gameSettings"]!["tutorialHints"]!.GetValue<bool>());
        actual["gameSettings"]!.AsObject().Remove("tutorialHints");
        Assert.True(JsonNode.DeepEquals(JsonNode.Parse(before), actual));

        DateTime timestamp = File.GetLastWriteTimeUtc(fixture.Path);
        Assert.True(TutorialHintsPreference.TryWrite(fixture.Path, false)); // same value: no rewrite
        Assert.Equal(timestamp, File.GetLastWriteTimeUtc(fixture.Path));
        Assert.Equal(serialized, File.ReadAllText(fixture.Path));

        Assert.True(TutorialHintsPreference.TryWrite(fixture.Path, true));
        Assert.True(TutorialHintsPreference.Read(fixture.Path));
        Assert.Single(Directory.EnumerateFiles(fixture.Root));

        // A missing file is created with just the group.
        File.Delete(fixture.Path);
        Assert.True(TutorialHintsPreference.TryWrite(fixture.Path, false));
        Assert.True(JsonNode.DeepEquals(JsonNode.Parse("""{"gameSettings":{"tutorialHints":false}}"""), JsonNode.Parse(File.ReadAllText(fixture.Path))));
    }

    [Fact]
    public void InvalidValueReadsAsEnabledWithWarning()
    {
        using var fixture = new SettingsFile();
        foreach (string value in new[] { "\"yes\"", "0", "1", "null", "[]", "{}" })
        {
            List<string> warnings = [];
            File.WriteAllText(fixture.Path, $$$"""{"gameSettings":{"tutorialHints":{{{value}}},"language":"English"}}""");
            Assert.True(TutorialHintsPreference.Read(fixture.Path, warnings.Add));
            Assert.Single(warnings);
            Assert.True(TutorialHintsPreference.TryWrite(fixture.Path, false)); // overwritten as a bool
            Assert.False(TutorialHintsPreference.Read(fixture.Path));
            Assert.Equal("English", JsonNode.Parse(File.ReadAllText(fixture.Path))!["gameSettings"]!["language"]!.GetValue<string>());
        }

        foreach (string broken in new[] { "{", "[]", """{"gameSettings":null}""", """{"gameSettings":42}""" })
        {
            List<string> warnings = [];
            File.WriteAllText(fixture.Path, broken);
            Assert.True(TutorialHintsPreference.Read(fixture.Path, warnings.Add));
            Assert.Single(warnings);
        }
    }

    [Fact]
    public void WriteFailureKeepsOriginalFileAndLeavesNoTemp()
    {
        using var fixture = new SettingsFile();
        List<string> warnings = [];
        foreach (string before in new[] { "{", "[]", """{"gameSettings":null}""", """{"gameSettings":42}""" })
        {
            File.WriteAllText(fixture.Path, before);
            Assert.False(TutorialHintsPreference.TryWrite(fixture.Path, false, warnings.Add));
            Assert.Equal(before, File.ReadAllText(fixture.Path));
            Assert.Single(Directory.EnumerateFiles(fixture.Root));
        }
        Assert.Equal(4, warnings.Count);

        if (!OperatingSystem.IsWindows()) return;
        const string original = """{"gameSettings":{"tutorialHints":true}}""";
        File.WriteAllText(fixture.Path, original);
        warnings.Clear();
        using (File.Open(fixture.Path, FileMode.Open, FileAccess.Read, FileShare.Read)) // the atomic replace cannot win
            Assert.False(TutorialHintsPreference.TryWrite(fixture.Path, false, warnings.Add));
        Assert.Equal(original, File.ReadAllText(fixture.Path));
        Assert.Single(Directory.EnumerateFiles(fixture.Root));
        Assert.NotEmpty(warnings);
        Assert.True(TutorialHintsPreference.Read(fixture.Path));
    }

    private sealed class SettingsFile : IDisposable
    {
        internal SettingsFile()
        {
            Root = System.IO.Path.Combine(Environment.GetEnvironmentVariable("DSS_VALIDATION_ROOT") ?? System.IO.Path.GetTempPath(),
                "tutorial-hints-settings-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(Root);
            Path = System.IO.Path.Combine(Root, "Settings.json");
        }

        internal string Root { get; }
        internal string Path { get; }

        public void Dispose() => Directory.Delete(Root, recursive: true);
    }
}
