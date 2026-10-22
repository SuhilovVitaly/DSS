using System.Collections.Immutable;
using System.Text.Json;
using Silk.NET.Input;

namespace DeepSpaceSaga.Client;

/// <summary>Startup-only input bindings. No simulation state or pending commands.</summary>
public sealed record CombatInputSettings
{
    public const string RelativePath = "Data/UI/combat-input.json";
    public Key TorpedoAim { get; init; } = Key.Q;
    public Key CountermissileAim { get; init; } = Key.W;
    public ImmutableArray<Key> Profiles { get; init; } = [Key.Number1, Key.Number2, Key.Number3];
    public Key Cancel { get; init; } = Key.Escape;
    public Key DebugPanel { get; init; } = Key.F12;
    public MouseButton LaunchMouse { get; init; } = MouseButton.Left;
    public MouseButton CancelMouse { get; init; } = MouseButton.Right;
    public bool WheelProfiles { get; init; } = true;
    public Key EvadeLeft { get; init; } = Key.A;
    public Key EvadeRight { get; init; } = Key.D;
    public Key NextThreat { get; init; } = Key.Tab;
    public Key TurnLeft { get; init; } = Key.Left;
    public Key TurnRight { get; init; } = Key.Right;
    public ImmutableArray<Key> FullSpeed { get; init; } = [Key.ShiftLeft, Key.ShiftRight];
    public static CombatInputSettings Default { get; } = new();
    internal IEnumerable<Key> MappedKeys => new[] { TorpedoAim, CountermissileAim, Cancel, DebugPanel, EvadeLeft, EvadeRight, NextThreat, TurnLeft, TurnRight }.Concat(Profiles).Concat(FullSpeed);

    internal CombatInputSettings Validate()
    {
        if (Profiles.IsDefault || Profiles.Length != 3) throw new ArgumentException("profiles must contain three keys");
        var keys = new HashSet<Key>();
        var namedKeys = new[] { ("torpedoAim", TorpedoAim), ("countermissileAim", CountermissileAim), ("cancel", Cancel),
            ("debugPanel", DebugPanel), ("evadeLeft", EvadeLeft), ("evadeRight", EvadeRight), ("nextThreat", NextThreat), ("turnLeft", TurnLeft), ("turnRight", TurnRight) }
            .Concat(Profiles.Select((key, index) => ($"profiles[{index}]", key)));
        foreach (var (field, key) in namedKeys)
        {
            string name = key.ToString();
            if (!Enum.IsDefined(key) || name == "Unknown" || key is Key.I or Key.S or Key.F5 or Key.F9 or Key.F10 ||
                name.StartsWith("Control", StringComparison.Ordinal) || name.StartsWith("Shift", StringComparison.Ordinal) || name.StartsWith("Alt", StringComparison.Ordinal))
                throw new ArgumentException($"{field}: unsupported or reserved combat key '{key}'");
            if (!keys.Add(key)) throw new ArgumentException($"{field}: Duplicate combat key '{key}'");
        }
        if (FullSpeed.IsDefaultOrEmpty || FullSpeed.Length > 2) throw new ArgumentException("fullSpeed must contain one or two keys");
        foreach (var key in FullSpeed)
        {
            string name = key.ToString();
            if (!Enum.IsDefined(key) || name == "Unknown" || key is Key.I or Key.S or Key.F5 or Key.F9 or Key.F10 ||
                name.StartsWith("Control", StringComparison.Ordinal) || name.StartsWith("Alt", StringComparison.Ordinal))
                throw new ArgumentException($"fullSpeed: unsupported or reserved combat key '{key}'");
            if (!keys.Add(key)) throw new ArgumentException($"fullSpeed: Duplicate combat key '{key}'");
        }
        if (!Enum.IsDefined(LaunchMouse) || !Enum.IsDefined(CancelMouse) || LaunchMouse == CancelMouse)
            throw new ArgumentException("launchMouse and cancelMouse must be distinct valid buttons");
        return this;
    }

    public static CombatInputSettings Load(string path)
    {
        try
        {
            using var document = JsonDocument.Parse(File.ReadAllText(path));
            var root = document.RootElement;
            if (root.ValueKind != JsonValueKind.Object) throw Invalid("root", "expected an object");
            string[] expected = ["schemaVersion", "torpedoAim", "countermissileAim", "profiles", "cancel", "launchMouse", "cancelMouse", "wheelProfiles"];
            string[] optional = ["evadeLeft", "evadeRight", "nextThreat", "turnLeft", "turnRight", "fullSpeed", "debugPanel"];
            var seen = new HashSet<string>(StringComparer.Ordinal);
            foreach (var property in root.EnumerateObject())
            {
                if (!seen.Add(property.Name)) throw Invalid(property.Name, "duplicate key");
                if (!expected.Contains(property.Name, StringComparer.Ordinal) && !optional.Contains(property.Name, StringComparer.Ordinal)) throw Invalid(property.Name, "unknown key");
            }
            foreach (string key in expected) if (!root.TryGetProperty(key, out _)) throw Invalid(key, "required field");
            if (root.GetProperty("schemaVersion").ValueKind != JsonValueKind.Number ||
                !root.GetProperty("schemaVersion").TryGetInt32(out int schema) || schema != 1) throw Invalid("schemaVersion", "expected 1");
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
                DebugPanel = OptionalKey("debugPanel", Key.F12),
                LaunchMouse = Read<MouseButton>(root.GetProperty("launchMouse"), "launchMouse"),
                CancelMouse = Read<MouseButton>(root.GetProperty("cancelMouse"), "cancelMouse"),
                WheelProfiles = wheel.GetBoolean(),
                EvadeLeft = OptionalKey("evadeLeft", Key.A),
                EvadeRight = OptionalKey("evadeRight", Key.D),
                NextThreat = OptionalKey("nextThreat", Key.Tab),
                TurnLeft = OptionalKey("turnLeft", Key.Left),
                TurnRight = OptionalKey("turnRight", Key.Right),
                FullSpeed = ReadFullSpeed()
            };
            try { return value.Validate(); }
            catch (ArgumentException error) { throw Invalid("bindings", error.Message); }

            T Read<T>(JsonElement element, string key) where T : struct, Enum =>
                element.ValueKind == JsonValueKind.String && Enum.TryParse<T>(element.GetString(), false, out var result) && Enum.IsDefined(result)
                    ? result : throw Invalid(key, "expected a valid name");
            Key OptionalKey(string key, Key fallback) => root.TryGetProperty(key, out var element) ? Read<Key>(element, key) : fallback;
            ImmutableArray<Key> ReadFullSpeed()
            {
                if (!root.TryGetProperty("fullSpeed", out var element)) return [Key.ShiftLeft, Key.ShiftRight];
                if (element.ValueKind != JsonValueKind.Array || element.GetArrayLength() is < 1 or > 2) throw Invalid("fullSpeed", "expected one or two key names");
                return element.EnumerateArray().Select(e => Read<Key>(e, "fullSpeed")).ToImmutableArray();
            }
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException or JsonException)
        { throw new InvalidDataException($"Combat input '{path}': {error.Message}", error); }
        InvalidDataException Invalid(string key, string reason) => new($"Combat input '{path}', key '{key}': {reason}");
    }
}
