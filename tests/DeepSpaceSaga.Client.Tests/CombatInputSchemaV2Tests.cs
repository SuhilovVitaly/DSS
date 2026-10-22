using System.Text.Json.Nodes;
using Silk.NET.Input;

namespace DeepSpaceSaga.Client.Tests;

public sealed class CombatInputSchemaV2Tests : IDisposable
{
    private readonly string _directory = Path.Combine("D:/DeepSpaceSaga/artifacts/validation/ep0013/schema-fixtures", Guid.NewGuid().ToString("N"));
    private string FilePath => Path.Combine(_directory, "combat-input.json");
    private static JsonObject Shipped() => JsonNode.Parse(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, CombatInputSettings.RelativePath)))!.AsObject();
    public CombatInputSchemaV2Tests() => Directory.CreateDirectory(_directory);
    private CombatInputSettings Load(JsonObject json, Action<string>? warning = null)
    { File.WriteAllText(FilePath, json.ToJsonString()); return CombatInputSettings.Load(FilePath, warning); }

    [Fact]
    public void V2_defaults_cover_both_aliases_allow_S_reserve_I_and_validate_every_duplicate()
    {
        var value = Load(Shipped());
        Assert.Equal(new[] { Key.A, Key.Left }, value.TurnLeft.ToArray()); Assert.Equal(new[] { Key.D, Key.Right }, value.TurnRight.ToArray());
        Assert.Equal(new[] { Key.W, Key.Up }, value.Accelerate.ToArray()); Assert.Equal(new[] { Key.S, Key.Down }, value.Brake.ToArray());
        Assert.Equal(Key.E, value.CountermissileAim); Assert.Equal(Key.Z, value.EvadeLeft); Assert.Equal(Key.C, value.EvadeRight); Assert.Equal(Key.F12, value.DebugPanel);
        foreach (string axis in new[] { "turnLeft", "turnRight", "accelerate", "brake" })
        {
            foreach (var invalid in new JsonNode[] { new JsonArray(), new JsonArray("J", "K", "L"), new JsonArray("J", "J"), new JsonArray("I"), JsonValue.Create("J")! })
            {
                var json = Shipped(); json[axis] = invalid.DeepClone();
                Assert.Contains(axis, Assert.Throws<InvalidDataException>(() => Load(json)).Message);
            }
        }
        var duplicate = Shipped(); duplicate["brake"] = new JsonArray("S", "A");
        Assert.Contains("Duplicate", Assert.Throws<InvalidDataException>(() => Load(duplicate)).Message);
        var reserved = Shipped(); reserved["debugPanel"] = "I";
        Assert.Contains("reserved", Assert.Throws<InvalidDataException>(() => Load(reserved)).Message);
        var remapped = Shipped(); remapped["turnLeft"] = new JsonArray("J"); remapped["accelerate"] = new JsonArray("U", "Up");
        value = Load(remapped); Assert.Equal(new[] { Key.J }, value.TurnLeft.ToArray()); Assert.Equal(new[] { Key.U, Key.Up }, value.Accelerate.ToArray());
    }

    [Fact]
    public void V1_conflicts_migrate_with_path_and_field_warnings_and_custom_bindings_survive()
    {
        var json = Shipped(); json["schemaVersion"] = 1; json["countermissileAim"] = "W"; json["evadeLeft"] = "A"; json["evadeRight"] = "D";
        json["turnLeft"] = "Left"; json["turnRight"] = "Right"; json.Remove("accelerate"); json.Remove("brake"); json.Remove("debugPanel");
        var warnings = new List<string>(); var value = Load(json, warnings.Add);
        Assert.Equal(Key.E, value.CountermissileAim); Assert.Equal(Key.Z, value.EvadeLeft); Assert.Equal(Key.C, value.EvadeRight);
        Assert.Equal(3, warnings.Count); Assert.All(warnings, warning => Assert.Contains(FilePath, warning));
        Assert.Equal(new[] { Key.A, Key.Left }, value.TurnLeft.ToArray()); Assert.Equal(new[] { Key.W, Key.Up }, value.Accelerate.ToArray());
        json["countermissileAim"] = "R"; json["evadeLeft"] = "V"; json["evadeRight"] = "B"; json["turnLeft"] = "J";
        warnings.Clear(); value = Load(json, warnings.Add);
        Assert.Empty(warnings); Assert.Equal(Key.R, value.CountermissileAim); Assert.Equal(Key.V, value.EvadeLeft); Assert.Equal(new[] { Key.J }, value.TurnLeft.ToArray());
    }

    [Fact]
    public void V2_is_strict_about_required_keys_unknown_keys_and_duplicate_json_properties()
    {
        var missing = Shipped(); missing.Remove("accelerate"); Assert.Contains("accelerate", Assert.Throws<InvalidDataException>(() => Load(missing)).Message);
        var unknown = Shipped(); unknown["unexpected"] = true; Assert.Contains("unexpected", Assert.Throws<InvalidDataException>(() => Load(unknown)).Message);
        File.WriteAllText(FilePath, Shipped().ToJsonString().Insert(1, "\"schemaVersion\":2,"));
        Assert.Contains("duplicate", Assert.Throws<InvalidDataException>(() => CombatInputSettings.Load(FilePath)).Message);
    }

    public void Dispose() => Directory.Delete(_directory, true);
}
