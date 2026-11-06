using System.Text.Json.Nodes;
using DeepSpaceSaga.Client.UI;
using Silk.NET.Input;

namespace DeepSpaceSaga.Client.Tests.TacticalBattle;

public sealed class ManeuverInputSettingsTests
{
    [Fact]
    public void ShippedAxesAndDebugKeyMatchTheWorldDefaults()
    {
        string path = Path.Combine(AppContext.BaseDirectory, ManeuverInputSettings.RelativePath);
        var settings = ManeuverInputSettings.Load(path);
        Assert.Equal(Key.F12, settings.DebugPanel);
        Assert.Equal(new[] { Key.A, Key.Left }, settings.TurnLeft);
        Assert.Equal(new[] { Key.D, Key.Right }, settings.TurnRight);
        Assert.Equal(new[] { Key.W, Key.Up }, settings.Accelerate);
        Assert.Equal(new[] { Key.S, Key.Down }, settings.Brake);
        Assert.Equal(new[] { Key.ShiftLeft, Key.ShiftRight }, settings.FullSpeed);
    }

    [Fact]
    public void RemappedKeysReachPollingAndConflictsAreRejectedWithoutChangingEitherFile()
    {
        string root = Environment.GetEnvironmentVariable("DSS_VALIDATION_ROOT") ?? Path.GetTempPath();
        string directory = Path.Combine(root, "maneuver-input-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        string path = Path.Combine(directory, "maneuver-input.json");
        try
        {
            var json = JsonNode.Parse(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, ManeuverInputSettings.RelativePath)))!.AsObject();
            json["turnLeft"] = new JsonArray("J");
            json["debugPanel"] = "F7";
            File.WriteAllText(path, json.ToJsonString());
            var settings = ManeuverInputSettings.Load(path);
            var tracker = new KeyboardEdgeTracker(maneuver: settings);
            var down = new HashSet<Key> { Key.J, Key.F7 };
            var pressed = new Key[64];
            var released = new Key[64];
            var (count, _) = tracker.PollBoth(down.Contains, pressed, released);
            Assert.Contains(Key.J, pressed[..count]);
            Assert.Contains(Key.F7, pressed[..count]);
            Assert.DoesNotContain(Key.A, settings.TurnLeft);

            json["accelerate"] = new JsonArray("J");
            File.WriteAllText(path, json.ToJsonString());
            Assert.Contains("accelerate", Assert.Throws<InvalidDataException>(() => ManeuverInputSettings.Load(path)).Message);
            json["accelerate"] = new JsonArray("Q");
            File.WriteAllText(path, json.ToJsonString());
            Assert.Contains("Q", Assert.Throws<InvalidDataException>(() => ManeuverInputSettings.Load(path, [Key.Q])).Message);
            json["debugPanel"] = "I";
            File.WriteAllText(path, json.ToJsonString());
            Assert.Contains("reserved", Assert.Throws<InvalidDataException>(() => ManeuverInputSettings.Load(path)).Message);
        }
        finally { File.Delete(path); Directory.Delete(directory); }
    }
}
