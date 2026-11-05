using System.Collections.Immutable;
using System.Text.Json;
using Silk.NET.Input;

namespace DeepSpaceSaga.Client;

/// <summary>Temporary real-time combat bindings; world maneuver input has a separate file.</summary>
public sealed record CombatInputSettings
{
    public const string RelativePath = "Data/UI/combat-input.json";
    public Key TorpedoAim { get; init; } = Key.Q;
    public Key CountermissileAim { get; init; } = Key.E;
    public ImmutableArray<Key> Profiles { get; init; } = [Key.Number1, Key.Number2, Key.Number3];
    public Key Cancel { get; init; } = Key.Escape;
    public MouseButton LaunchMouse { get; init; } = MouseButton.Left;
    public MouseButton CancelMouse { get; init; } = MouseButton.Right;
    public bool WheelProfiles { get; init; } = true;
    public Key EvadeLeft { get; init; } = Key.Z;
    public Key EvadeRight { get; init; } = Key.C;
    public Key NextThreat { get; init; } = Key.Tab;
    public static CombatInputSettings Default { get; } = new();
    internal IEnumerable<Key> MappedKeys => new[] { TorpedoAim, CountermissileAim, Cancel, EvadeLeft, EvadeRight, NextThreat }.Concat(Profiles);

    internal CombatInputSettings Validate()
    {
        if (Profiles.IsDefault || Profiles.Length != 3) throw new ArgumentException("profiles must contain three keys");
        var keys = new HashSet<Key>();
        foreach (var key in MappedKeys)
        {
            string name = key.ToString();
            if (!Enum.IsDefined(key) || key == Key.Unknown || key is Key.I or Key.F5 or Key.F9 or Key.F10 ||
                name.StartsWith("Control", StringComparison.Ordinal) || name.StartsWith("Alt", StringComparison.Ordinal) ||
                name.StartsWith("Shift", StringComparison.Ordinal))
                throw new ArgumentException($"unsupported or reserved combat key '{key}'");
            if (!keys.Add(key)) throw new ArgumentException($"Duplicate combat key '{key}'");
        }
        if (!Enum.IsDefined(LaunchMouse) || !Enum.IsDefined(CancelMouse) || LaunchMouse == CancelMouse)
            throw new ArgumentException("launchMouse and cancelMouse must be distinct valid buttons");
        return this;
    }

    public static CombatInputSettings Load(string path, Action<string>? warning = null)
    {
        try
        {
            using var document = JsonDocument.Parse(File.ReadAllText(path));
            var root = document.RootElement;
            if (root.ValueKind != JsonValueKind.Object) throw Invalid("root", "expected an object");
            if (!root.TryGetProperty("schemaVersion", out var version) || version.ValueKind != JsonValueKind.Number ||
                !version.TryGetInt32(out int schema) || schema is < 1 or > 3) throw Invalid("schemaVersion", "expected 1, 2 or 3");
            string[] required = ["schemaVersion", "torpedoAim", "countermissileAim", "profiles", "cancel", "launchMouse", "cancelMouse", "wheelProfiles"];
            string[] optional = ["evadeLeft", "evadeRight", "nextThreat"];
            string[] obsolete = ["turnLeft", "turnRight", "fullSpeed", "accelerate", "brake", "debugPanel"];
            var seen = new HashSet<string>(StringComparer.Ordinal);
            foreach (var property in root.EnumerateObject())
            {
                if (!seen.Add(property.Name)) throw Invalid(property.Name, "duplicate key");
                if (!required.Contains(property.Name) && !optional.Contains(property.Name) &&
                    !(schema < 3 && obsolete.Contains(property.Name))) throw Invalid(property.Name, "unknown key");
            }
            foreach (string key in required) if (!seen.Contains(key)) throw Invalid(key, "required field");
            if (schema >= 2) foreach (string key in optional) if (!seen.Contains(key)) throw Invalid(key, "required field");
            var profiles = root.GetProperty("profiles");
            if (profiles.ValueKind != JsonValueKind.Array || profiles.GetArrayLength() != 3) throw Invalid("profiles", "expected three key names");
            var wheel = root.GetProperty("wheelProfiles");
            if (wheel.ValueKind is not (JsonValueKind.True or JsonValueKind.False)) throw Invalid("wheelProfiles", "expected boolean");
            var value = new CombatInputSettings
            {
                TorpedoAim = Read<Key>(root.GetProperty("torpedoAim"), "torpedoAim"),
                CountermissileAim = Read<Key>(root.GetProperty("countermissileAim"), "countermissileAim"),
                Profiles = profiles.EnumerateArray().Select(p => Read<Key>(p, "profiles")).ToImmutableArray(),
                Cancel = Read<Key>(root.GetProperty("cancel"), "cancel"),
                LaunchMouse = Read<MouseButton>(root.GetProperty("launchMouse"), "launchMouse"),
                CancelMouse = Read<MouseButton>(root.GetProperty("cancelMouse"), "cancelMouse"),
                WheelProfiles = wheel.GetBoolean(),
                EvadeLeft = OptionalKey("evadeLeft", Key.Z),
                EvadeRight = OptionalKey("evadeRight", Key.C),
                NextThreat = OptionalKey("nextThreat", Key.Tab)
            };
            if (schema == 1)
            {
                value = value with
                {
                    CountermissileAim = Migrate("countermissileAim", value.CountermissileAim, Key.E),
                    EvadeLeft = Migrate("evadeLeft", value.EvadeLeft, Key.Z),
                    EvadeRight = Migrate("evadeRight", value.EvadeRight, Key.C)
                };
            }
            try { return value.Validate(); }
            catch (ArgumentException error) { throw Invalid("bindings", error.Message); }

            Key Migrate(string field, Key key, Key fallback)
            {
                if (!ManeuverInputSettings.Default.MappedKeys.Contains(key)) return key;
                warning?.Invoke($"Combat input '{path}': schema1 {field} '{key}' conflicts with maneuvering; using '{fallback}'.");
                return fallback;
            }
            T Read<T>(JsonElement element, string key) where T : struct, Enum =>
                element.ValueKind == JsonValueKind.String && Enum.TryParse<T>(element.GetString(), false, out var result) &&
                Enum.IsDefined(result) && result.ToString() == element.GetString() ? result : throw Invalid(key, "expected a valid name");
            Key OptionalKey(string key, Key fallback) => root.TryGetProperty(key, out var element) ? Read<Key>(element, key) : fallback;
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException or JsonException)
        { throw new InvalidDataException($"Combat input '{path}': {error.Message}", error); }
        InvalidDataException Invalid(string key, string reason) => new($"Combat input '{path}', {key}: {reason}");
    }
}
