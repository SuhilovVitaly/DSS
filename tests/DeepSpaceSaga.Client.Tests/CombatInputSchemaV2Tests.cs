using System.Text.Json.Nodes;
using Silk.NET.Input;

namespace DeepSpaceSaga.Client.Tests;

public sealed class CombatInputSchemaV2Tests : IDisposable
{
    private readonly string _directory = Path.Combine(Environment.GetEnvironmentVariable("DSS_VALIDATION_ROOT") ?? Path.GetTempPath(),
        "combat-input-" + Guid.NewGuid().ToString("N"));
    private string FilePath => Path.Combine(_directory, "combat-input.json");
    private static JsonObject Shipped() => JsonNode.Parse(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, CombatInputSettings.RelativePath)))!.AsObject();
    public CombatInputSchemaV2Tests() => Directory.CreateDirectory(_directory);
    private CombatInputSettings Load(JsonObject json, Action<string>? warning = null)
    { File.WriteAllText(FilePath, json.ToJsonString()); return CombatInputSettings.Load(FilePath, warning); }

    [Fact]
    public void V3ContainsOnlyCombatBindingsAndV2IgnoresOldMovementProperties()
    {
        var json = Shipped();
        var value = Load(json);
        Assert.Equal(Key.E, value.CountermissileAim);
        Assert.Equal(Key.Z, value.EvadeLeft);
        Assert.Equal(Key.C, value.EvadeRight);
        Assert.False(json.ContainsKey("debugPanel"));
        json["turnLeft"] = "ignored legacy field";
        Assert.Contains("turnLeft", Assert.Throws<InvalidDataException>(() => Load(json)).Message);
        json["schemaVersion"] = 2;
        Assert.Equal(Key.Z, Load(json).EvadeLeft);
    }

    [Fact]
    public void V1ConflictsMigrateWithPathAndFieldWarningsAndCustomCombatBindingsSurvive()
    {
        var json = Shipped(); json["schemaVersion"] = 1; json["countermissileAim"] = "W"; json["evadeLeft"] = "A"; json["evadeRight"] = "D";
        var warnings = new List<string>(); var value = Load(json, warnings.Add);
        Assert.Equal(Key.E, value.CountermissileAim); Assert.Equal(Key.Z, value.EvadeLeft); Assert.Equal(Key.C, value.EvadeRight);
        Assert.Equal(3, warnings.Count); Assert.All(warnings, warning => Assert.Contains(FilePath, warning));
        json["countermissileAim"] = "R"; json["evadeLeft"] = "V"; json["evadeRight"] = "B";
        warnings.Clear(); value = Load(json, warnings.Add);
        Assert.Empty(warnings); Assert.Equal(Key.R, value.CountermissileAim); Assert.Equal(Key.V, value.EvadeLeft);
    }

    [Fact]
    public void V3ValidatesRequiredFieldsUnknownFieldsAndDuplicateKeys()
    {
        var missing = Shipped(); missing.Remove("nextThreat"); Assert.Contains("nextThreat", Assert.Throws<InvalidDataException>(() => Load(missing)).Message);
        var unknown = Shipped(); unknown["unexpected"] = true; Assert.Contains("unexpected", Assert.Throws<InvalidDataException>(() => Load(unknown)).Message);
        var duplicate = Shipped(); duplicate["evadeRight"] = "Z"; Assert.Contains("Duplicate", Assert.Throws<InvalidDataException>(() => Load(duplicate)).Message);
        File.WriteAllText(FilePath, Shipped().ToJsonString().Insert(1, "\"schemaVersion\":3,"));
        Assert.Contains("duplicate", Assert.Throws<InvalidDataException>(() => CombatInputSettings.Load(FilePath)).Message);
    }

    public void Dispose() { File.Delete(FilePath); Directory.Delete(_directory); }
}
