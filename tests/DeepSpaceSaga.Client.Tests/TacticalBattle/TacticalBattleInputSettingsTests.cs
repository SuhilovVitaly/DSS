using System.Text.Json;
using DeepSpaceSaga.Client.UI;
using Silk.NET.Input;

namespace DeepSpaceSaga.Client.Tests.TacticalBattle;

public sealed class TacticalBattleInputSettingsTests
{
    private const string Valid = """{"schemaVersion":1,"tacticalBattle":"F8","repeatDelayMs":400,"repeatPeriodMs":80}""";

    [Fact]
    public void PackagedDefaultsAndCustomBindingsReachTrackerWithoutRoundingTimings()
    {
        var messages = new List<string>();
        var defaults = TacticalBattleInputSettings.Load(Path.Combine(AppContext.BaseDirectory, TacticalBattleInputSettings.RelativePath), warning: messages.Add);
        Assert.Equal(TacticalBattleInputSettings.Default, defaults);
        Assert.Empty(messages);
        using var fixture = new FileFixture(Valid.Replace("F8", "F7").Replace("400", "123").Replace("80", "27"));
        var custom = TacticalBattleInputSettings.Load(fixture.Path, warning: messages.Add);
        Assert.Equal(Key.F7, custom.TacticalBattle);
        Assert.Equal(123, custom.RepeatDelayMs);
        Assert.Equal(27, custom.RepeatPeriodMs);
        var tracker = new KeyboardEdgeTracker(tactical: custom);
        var down = new HashSet<Key> { Key.F7, Key.Left };
        var pressed = new Key[64]; var released = new Key[64]; var repeated = new Key[4];
        var result = tracker.PollBoth(down.Contains, pressed, released, 0);
        Assert.Contains(Key.F7, pressed.Take(result.pressedCount));
        Assert.Equal(0, tracker.PollRepeats(122, repeated));
        Assert.Equal(1, tracker.PollRepeats(123, repeated));
        Assert.Equal(0, tracker.PollRepeats(149, repeated));
        Assert.Equal(1, tracker.PollRepeats(150, repeated));
        Assert.Empty(messages);
    }

    [Theory]
    [InlineData("UnknownKey")]
    [InlineData("Unknown")]
    [InlineData("65")]
    [InlineData("f8")]
    [InlineData("I")]
    [InlineData("F5")]
    [InlineData("F9")]
    [InlineData("F10")]
    [InlineData("F12")]
    [InlineData("ShiftLeft")]
    [InlineData("ControlRight")]
    [InlineData("AltLeft")]
    [InlineData("Tab")]
    [InlineData("Enter")]
    [InlineData("KeypadEnter")]
    [InlineData("Number0")]
    [InlineData("Left")]
    [InlineData("W")]
    public void UnknownAndGameControlKeysAreLoggedAndFallBack(string key)
    {
        using var fixture = new FileFixture(Valid.Replace("F8", key));
        var messages = new List<string>();
        var value = TacticalBattleInputSettings.Load(fixture.Path, warning: messages.Add);
        Assert.Equal(TacticalBattleInputSettings.Default, value);
        string message = Assert.Single(messages);
        Assert.Contains(fixture.Path, message);
        Assert.Contains("tacticalBattle", message);
    }

    [Theory]
    [InlineData("{}", "schemaVersion")]
    [InlineData("[]", "root")]
    [InlineData("{", "")]
    [InlineData("{\"schemaVersion\":1,\"schemaVersion\":1}", "schemaVersion")]
    [InlineData("{\"schemaVersion\":1,\"extra\":0}", "extra")]
    public void MalformedRootDuplicateAndUnknownFieldsFallBack(string json, string field)
    {
        using var fixture = new FileFixture(json);
        var messages = new List<string>();
        Assert.Equal(TacticalBattleInputSettings.Default, TacticalBattleInputSettings.Load(fixture.Path, warning: messages.Add));
        Assert.Contains(field, Assert.Single(messages));
    }

    [Theory]
    [InlineData("schemaVersion", "2")]
    [InlineData("repeatDelayMs", "-1")]
    [InlineData("repeatDelayMs", "1.5")]
    [InlineData("repeatDelayMs", "null")]
    [InlineData("repeatPeriodMs", "0")]
    [InlineData("repeatPeriodMs", "-1")]
    [InlineData("repeatPeriodMs", "2147483648")]
    [InlineData("tacticalBattle", "123")]
    public void InvalidSchemaTypesAndTimingsFallBack(string field, string jsonValue)
    {
        var root = System.Text.Json.Nodes.JsonNode.Parse(Valid)!;
        root[field] = System.Text.Json.Nodes.JsonNode.Parse(jsonValue);
        using var fixture = new FileFixture(root.ToJsonString());
        var messages = new List<string>();
        Assert.Equal(TacticalBattleInputSettings.Default, TacticalBattleInputSettings.Load(fixture.Path, warning: messages.Add));
        Assert.Contains(field, Assert.Single(messages));
    }

    [Fact]
    public void ConflictUsesActualCustomGameBindingsIncludingSiblingFile()
    {
        using var fixture = new FileFixture(Valid.Replace("F8", "F7"));
        var messages = new List<string>();
        var game = ManeuverInputSettings.Default with { DebugPanel = Key.F7 };
        Assert.Equal(TacticalBattleInputSettings.Default, TacticalBattleInputSettings.Load(fixture.Path, warning: messages.Add, maneuverBindings: game));
        Assert.Contains("F7", Assert.Single(messages));
        var root = JsonDocument.Parse(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, ManeuverInputSettings.RelativePath)));
        using (root)
        {
            string gameJson = root.RootElement.GetRawText().Replace("F12", "F7");
            File.WriteAllText(System.IO.Path.Combine(System.IO.Path.GetDirectoryName(fixture.Path)!, "maneuver-input.json"), gameJson);
        }
        messages.Clear();
        Assert.Equal(TacticalBattleInputSettings.Default, TacticalBattleInputSettings.Load(fixture.Path, warning: messages.Add));
        Assert.Contains("F7", Assert.Single(messages));
    }

    [Fact]
    public void DefaultCollisionDisablesOnlyShortcutWithoutRewritingGameBindings()
    {
        using var fixture = new FileFixture("{}");
        var messages = new List<string>();
        var game = ManeuverInputSettings.Default with { DebugPanel = Key.F8 };
        var result = TacticalBattleInputSettings.Load(fixture.Path, game, messages.Add);
        Assert.Equal(Key.F8, result.TacticalBattle);
        Assert.Equal(400, result.RepeatDelayMs);
        Assert.Equal(80, result.RepeatPeriodMs);
        Assert.False(result.ShortcutAvailable);
        Assert.Equal(Key.F8, game.DebugPanel);
        Assert.Equal(2, messages.Count);
        Assert.Contains("F8", messages[1]);
    }

    [Fact]
    public void MissingFileLogsExactPathAndRetainsDefaults()
    {
        using var fixture = new FileFixture(Valid);
        File.Delete(fixture.Path);
        var messages = new List<string>();
        Assert.Equal(TacticalBattleInputSettings.Default, TacticalBattleInputSettings.Load(fixture.Path, warning: messages.Add));
        Assert.Contains(fixture.Path, Assert.Single(messages));
    }

    private sealed class FileFixture : IDisposable
    {
        private readonly string _directory = System.IO.Path.Combine(Environment.GetEnvironmentVariable("DSS_VALIDATION_ROOT") ?? System.IO.Path.GetTempPath(), "input-" + Guid.NewGuid().ToString("N"));
        public string Path => System.IO.Path.Combine(_directory, "tactical-battle-input.json");
        public FileFixture(string content) { Directory.CreateDirectory(_directory); File.WriteAllText(Path, content); }
        public void Dispose() => Directory.Delete(_directory, true);
    }
}
