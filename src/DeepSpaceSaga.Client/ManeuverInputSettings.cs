using System.Collections.Immutable;
using System.Text.Json;
using Silk.NET.Input;

namespace DeepSpaceSaga.Client;

/// <summary>Startup bindings for ordinary ship movement and the world tuning panel.</summary>
public sealed record ManeuverInputSettings
{
    public const string RelativePath = "Data/UI/maneuver-input.json";
    public Key DebugPanel { get; init; } = Key.F12;
    public ImmutableArray<Key> TurnLeft { get; init; } = [Key.A, Key.Left];
    public ImmutableArray<Key> TurnRight { get; init; } = [Key.D, Key.Right];
    public ImmutableArray<Key> Accelerate { get; init; } = [Key.W, Key.Up];
    public ImmutableArray<Key> Brake { get; init; } = [Key.S, Key.Down];
    public ImmutableArray<Key> FullSpeed { get; init; } = [Key.ShiftLeft, Key.ShiftRight];
    public static ManeuverInputSettings Default { get; } = new();

    internal IEnumerable<Key> MappedKeys => new[] { DebugPanel }.Concat(TurnLeft).Concat(TurnRight)
        .Concat(Accelerate).Concat(Brake).Concat(FullSpeed);

    internal ManeuverInputSettings Validate(IEnumerable<Key>? conflicts = null)
    {
        var used = new HashSet<Key>(conflicts ?? []);
        Check("debugPanel", [DebugPanel]);
        Check("turnLeft", TurnLeft); Check("turnRight", TurnRight);
        Check("accelerate", Accelerate); Check("brake", Brake); Check("fullSpeed", FullSpeed, allowShift: true);
        return this;

        void Check(string field, ImmutableArray<Key> keys, bool allowShift = false)
        {
            if (keys.IsDefaultOrEmpty || keys.Length > 2) throw new ArgumentException(field + ": expected one or two keys");
            foreach (var key in keys)
            {
                string name = key.ToString();
                if (!Enum.IsDefined(key) || key == Key.Unknown || key is Key.I or Key.F or Key.F5 or Key.F9 or Key.F10 or
                    Key.Escape or Key.Enter or Key.KeypadEnter or Key.Tab or Key.Space or Key.Number0 or Key.Keypad0 or
                    Key.Number1 or Key.Number2 or Key.Number3 or Key.Number4 or Key.Number5 or Key.Home or Key.End or
                    Key.Equal or Key.Minus or Key.KeypadAdd or Key.KeypadSubtract ||
                    name.StartsWith("Control", StringComparison.Ordinal) || name.StartsWith("Alt", StringComparison.Ordinal) ||
                    !allowShift && name.StartsWith("Shift", StringComparison.Ordinal))
                    throw new ArgumentException($"{field}: reserved maneuver key '{key}'");
                if (!used.Add(key)) throw new ArgumentException($"{field}: Duplicate or conflicting key '{key}'");
            }
        }
    }

    public static ManeuverInputSettings Load(string path, IEnumerable<Key>? conflicts = null)
    {
        try
        {
            using var document = JsonDocument.Parse(File.ReadAllText(path));
            var root = document.RootElement;
            string[] fields = ["schemaVersion", "debugPanel", "turnLeft", "turnRight", "accelerate", "brake", "fullSpeed"];
            if (root.ValueKind != JsonValueKind.Object) throw new ArgumentException("expected an object");
            var seen = new HashSet<string>(StringComparer.Ordinal);
            foreach (var property in root.EnumerateObject())
            {
                if (!seen.Add(property.Name)) throw new ArgumentException(property.Name + ": duplicate field");
                if (!fields.Contains(property.Name, StringComparer.Ordinal)) throw new ArgumentException(property.Name + ": unknown field");
            }
            foreach (string field in fields)
                if (!seen.Contains(field)) throw new ArgumentException(field + ": required field");
            var version = root.GetProperty("schemaVersion");
            if (version.ValueKind != JsonValueKind.Number || !version.TryGetInt32(out int schema) || schema != 1)
                throw new ArgumentException("schemaVersion: expected 1");
            return new ManeuverInputSettings
            {
                DebugPanel = Read(root.GetProperty("debugPanel"), "debugPanel"),
                TurnLeft = Axis("turnLeft"),
                TurnRight = Axis("turnRight"),
                Accelerate = Axis("accelerate"),
                Brake = Axis("brake"),
                FullSpeed = Axis("fullSpeed")
            }.Validate(conflicts);

            ImmutableArray<Key> Axis(string field)
            {
                var value = root.GetProperty(field);
                if (value.ValueKind != JsonValueKind.Array || value.GetArrayLength() is < 1 or > 2)
                    throw new ArgumentException(field + ": expected one or two key names");
                return value.EnumerateArray().Select(e => Read(e, field)).ToImmutableArray();
            }
            static Key Read(JsonElement value, string field)
            {
                string? name = value.ValueKind == JsonValueKind.String ? value.GetString() : null;
                if (name is null || !Enum.TryParse<Key>(name, out var key) || !Enum.IsDefined(key) || key.ToString() != name)
                    throw new ArgumentException(field + ": expected a key name");
                return key;
            }
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException or JsonException or ArgumentException)
        {
            throw new InvalidDataException($"Maneuver input '{path}': {error.Message}", error);
        }
    }
}
