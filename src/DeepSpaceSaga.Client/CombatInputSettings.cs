using System.Collections.Immutable;
using System.Text.Json;
using Silk.NET.Input;

namespace DeepSpaceSaga.Client;

/// <summary>Startup-only bindings; v1 files migrate explicitly to the v2 control layout.</summary>
public sealed record CombatInputSettings
{
    public const string RelativePath = "Data/UI/combat-input.json";
    public Key TorpedoAim { get; init; } = Key.Q;
    public Key CountermissileAim { get; init; } = Key.E;
    public ImmutableArray<Key> Profiles { get; init; } = [Key.Number1, Key.Number2, Key.Number3];
    public Key Cancel { get; init; } = Key.Escape;
    public Key DebugPanel { get; init; } = Key.F12;
    public MouseButton LaunchMouse { get; init; } = MouseButton.Left;
    public MouseButton CancelMouse { get; init; } = MouseButton.Right;
    public bool WheelProfiles { get; init; } = true;
    public Key EvadeLeft { get; init; } = Key.Z;
    public Key EvadeRight { get; init; } = Key.C;
    public Key NextThreat { get; init; } = Key.Tab;
    public ImmutableArray<Key> TurnLeft { get; init; } = [Key.A, Key.Left];
    public ImmutableArray<Key> TurnRight { get; init; } = [Key.D, Key.Right];
    public ImmutableArray<Key> Accelerate { get; init; } = [Key.W, Key.Up];
    public ImmutableArray<Key> Brake { get; init; } = [Key.S, Key.Down];
    public ImmutableArray<Key> FullSpeed { get; init; } = [Key.ShiftLeft, Key.ShiftRight];
    public static CombatInputSettings Default { get; } = new();
    internal IEnumerable<Key> MappedKeys => new[] { TorpedoAim, CountermissileAim, Cancel, DebugPanel, EvadeLeft, EvadeRight, NextThreat }
        .Concat(Profiles).Concat(TurnLeft).Concat(TurnRight).Concat(Accelerate).Concat(Brake).Concat(FullSpeed);

    internal CombatInputSettings Validate()
    {
        if (Profiles.IsDefault || Profiles.Length != 3) throw new ArgumentException("profiles must contain three keys");
        var keys = new HashSet<Key>();
        foreach (var (field, key) in new[] { ("torpedoAim", TorpedoAim), ("countermissileAim", CountermissileAim), ("cancel", Cancel),
            ("debugPanel", DebugPanel), ("evadeLeft", EvadeLeft), ("evadeRight", EvadeRight), ("nextThreat", NextThreat) }) Add(field, key);
        foreach (var key in Profiles) Add("profiles", key);
        CheckArray("turnLeft", TurnLeft); CheckArray("turnRight", TurnRight);
        CheckArray("accelerate", Accelerate); CheckArray("brake", Brake); CheckArray("fullSpeed", FullSpeed, allowShift: true);
        if (!Enum.IsDefined(LaunchMouse) || !Enum.IsDefined(CancelMouse) || LaunchMouse == CancelMouse)
            throw new ArgumentException("launchMouse and cancelMouse must be distinct valid buttons");
        return this;
        void Add(string field, Key key, bool allowShift = false)
        {
            string name = key.ToString();
            if (!Enum.IsDefined(key) || name == "Unknown" || key is Key.I or Key.F5 or Key.F9 or Key.F10 ||
                name.StartsWith("Control", StringComparison.Ordinal) || name.StartsWith("Alt", StringComparison.Ordinal) ||
                !allowShift && name.StartsWith("Shift", StringComparison.Ordinal))
                throw new ArgumentException($"{field}: unsupported or reserved combat key '{key}'");
            if (!keys.Add(key)) throw new ArgumentException($"{field}: Duplicate combat key '{key}'");
        }
        void CheckArray(string field, ImmutableArray<Key> values, bool allowShift = false)
        {
            if (values.IsDefaultOrEmpty || values.Length > 2) throw new ArgumentException(field + " must contain one or two keys");
            foreach (var key in values) Add(field, key, allowShift);
        }
    }

    public static CombatInputSettings Load(string path, Action<string>? warning = null)
    {
        try
        {
            using var document = JsonDocument.Parse(File.ReadAllText(path));
            var root = document.RootElement;
            if (root.ValueKind != JsonValueKind.Object) throw Invalid("root", "expected an object");
            if (!root.TryGetProperty("schemaVersion", out var version) || version.ValueKind != JsonValueKind.Number ||
                !version.TryGetInt32(out int schema) || schema is not (1 or 2)) throw Invalid("schemaVersion", "expected 1 or 2");
            string[] required = ["schemaVersion", "torpedoAim", "countermissileAim", "profiles", "cancel", "launchMouse", "cancelMouse", "wheelProfiles"];
            string[] optional = ["evadeLeft", "evadeRight", "nextThreat", "turnLeft", "turnRight", "fullSpeed", "accelerate", "brake", "debugPanel"];
            var seen = new HashSet<string>(StringComparer.Ordinal);
            foreach (var property in root.EnumerateObject())
            {
                if (!seen.Add(property.Name)) throw Invalid(property.Name, "duplicate key");
                if (!required.Contains(property.Name, StringComparer.Ordinal) && !optional.Contains(property.Name, StringComparer.Ordinal)) throw Invalid(property.Name, "unknown key");
                if (schema == 1 && property.Name is "accelerate" or "brake") throw Invalid(property.Name, "requires schemaVersion 2");
            }
            foreach (string key in required) if (!root.TryGetProperty(key, out _)) throw Invalid(key, "required field");
            if (schema == 2) foreach (string key in optional) if (!root.TryGetProperty(key, out _)) throw Invalid(key, "required field");
            var profiles = root.GetProperty("profiles");
            if (profiles.ValueKind != JsonValueKind.Array || profiles.GetArrayLength() != 3) throw Invalid("profiles", "expected three key names");
            var wheel = root.GetProperty("wheelProfiles");
            if (wheel.ValueKind is not (JsonValueKind.True or JsonValueKind.False)) throw Invalid("wheelProfiles", "expected boolean");
            var value = new CombatInputSettings
            {
                TorpedoAim = ReadKey(root.GetProperty("torpedoAim"), "torpedoAim"),
                CountermissileAim = ReadKey(root.GetProperty("countermissileAim"), "countermissileAim"),
                Profiles = profiles.EnumerateArray().Select(p => ReadKey(p, "profiles")).ToImmutableArray(),
                Cancel = ReadKey(root.GetProperty("cancel"), "cancel"),
                DebugPanel = OptionalKey("debugPanel", Key.F12),
                LaunchMouse = Read<MouseButton>(root.GetProperty("launchMouse"), "launchMouse"),
                CancelMouse = Read<MouseButton>(root.GetProperty("cancelMouse"), "cancelMouse"),
                WheelProfiles = wheel.GetBoolean(),
                EvadeLeft = OptionalKey("evadeLeft", schema == 1 ? Key.A : Key.Z),
                EvadeRight = OptionalKey("evadeRight", schema == 1 ? Key.D : Key.C),
                NextThreat = OptionalKey("nextThreat", Key.Tab),
                TurnLeft = Axis("turnLeft", Default.TurnLeft, Key.Left),
                TurnRight = Axis("turnRight", Default.TurnRight, Key.Right),
                Accelerate = Axis("accelerate", Default.Accelerate),
                Brake = Axis("brake", Default.Brake),
                FullSpeed = Axis("fullSpeed", Default.FullSpeed)
            };
            if (schema == 1)
            {
                var maneuver = value.TurnLeft.Concat(value.TurnRight).Concat(value.Accelerate).Concat(value.Brake).ToHashSet();
                Key Migrate(string field, Key key, Key fallback)
                {
                    if (!maneuver.Contains(key)) return key;
                    warning?.Invoke($"Combat input '{path}': schema1 {field} '{key}' conflicts with maneuvering; using '{fallback}'.");
                    return fallback;
                }
                value = value with
                {
                    CountermissileAim = Migrate("countermissileAim", value.CountermissileAim, Key.E),
                    EvadeLeft = Migrate("evadeLeft", value.EvadeLeft, Key.Z),
                    EvadeRight = Migrate("evadeRight", value.EvadeRight, Key.C)
                };
            }
            try { return value.Validate(); }
            catch (ArgumentException error) { throw Invalid("bindings", error.Message); }

            T Read<T>(JsonElement element, string key) where T : struct, Enum =>
                element.ValueKind == JsonValueKind.String && Enum.TryParse<T>(element.GetString(), false, out var result) && Enum.IsDefined(result)
                    ? result : throw Invalid(key, "expected a valid name");
            Key ReadKey(JsonElement element, string key) => Read<Key>(element, key);
            Key OptionalKey(string key, Key fallback) => root.TryGetProperty(key, out var element) ? ReadKey(element, key) : fallback;
            ImmutableArray<Key> Axis(string key, ImmutableArray<Key> fallback, Key? legacyDefault = null)
            {
                if (!root.TryGetProperty(key, out var element)) return fallback;
                if (schema == 1 && legacyDefault is { } defaultKey)
                {
                    Key legacy = ReadKey(element, key);
                    return legacy == defaultKey ? fallback : [legacy];
                }
                if (element.ValueKind != JsonValueKind.Array || element.GetArrayLength() is < 1 or > 2) throw Invalid(key, "expected one or two key names");
                return element.EnumerateArray().Select(e => ReadKey(e, key)).ToImmutableArray();
            }
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException or JsonException)
        { throw new InvalidDataException($"Combat input '{path}': {error.Message}", error); }
        InvalidDataException Invalid(string key, string reason) => new($"Combat input '{path}', key '{key}': {reason}");
    }
}
