using System.Globalization;
using System.Text.Json;
using DeepSpaceSaga.Client.UI;
using Silk.NET.Input;

namespace DeepSpaceSaga.Client;

/// <summary>Startup-only tactical UI bindings. Invalid files fall back without changing game bindings.</summary>
public sealed record TacticalBattleInputSettings
{
    public const string RelativePath = "Data/UI/tactical-battle-input.json";
    public Key TacticalBattle { get; init; } = Key.F8;
    public int RepeatDelayMs { get; init; } = 400;
    public int RepeatPeriodMs { get; init; } = 80;
    public bool ShortcutAvailable { get; init; } = true;
    public static TacticalBattleInputSettings Default { get; } = new();

    public static TacticalBattleInputSettings Load(string path, ManeuverInputSettings? maneuverBindings = null, Action<string>? warning = null)
    {
        warning ??= InterfaceLog.Write;
        var maneuver = maneuverBindings ?? ManeuverInputSettings.Default;
        try
        {
            if (maneuverBindings is null)
            {
                string maneuverPath = Path.Combine(Path.GetDirectoryName(path) ?? "", Path.GetFileName(ManeuverInputSettings.RelativePath));
                if (File.Exists(maneuverPath)) maneuver = ManeuverInputSettings.Load(maneuverPath);
            }
            using var document = JsonDocument.Parse(File.ReadAllText(path));
            var root = document.RootElement;
            if (root.ValueKind != JsonValueKind.Object) throw Invalid("root", "expected object");
            string[] fields = ["schemaVersion", "tacticalBattle", "repeatDelayMs", "repeatPeriodMs"];
            var seen = new HashSet<string>(StringComparer.Ordinal);
            foreach (var property in root.EnumerateObject())
            {
                if (!seen.Add(property.Name)) throw Invalid(property.Name, "duplicate field");
                if (!fields.Contains(property.Name, StringComparer.Ordinal)) throw Invalid(property.Name, "unknown field");
            }
            foreach (string field in fields) if (!seen.Contains(field)) throw Invalid(field, "required field");
            if (Integer("schemaVersion") != 1) throw Invalid("schemaVersion", "expected 1");
            var keyValue = root.GetProperty("tacticalBattle");
            if (keyValue.ValueKind != JsonValueKind.String || !Enum.TryParse<Key>(keyValue.GetString(), out var key)
                || !Enum.IsDefined(key) || key.ToString() != keyValue.GetString() || key.ToString() == "Unknown")
                throw Invalid("tacticalBattle", "unknown key name");
            if (Conflicts(key, maneuver)) throw Invalid("tacticalBattle", "reserved key or game binding conflict: " + key);
            int delay = Integer("repeatDelayMs"), period = Integer("repeatPeriodMs");
            if (delay < 0) throw Invalid("repeatDelayMs", "expected non-negative milliseconds");
            if (period <= 0) throw Invalid("repeatPeriodMs", "expected positive milliseconds");
            return new() { TacticalBattle = key, RepeatDelayMs = delay, RepeatPeriodMs = period };

            int Integer(string field) => root.GetProperty(field).ValueKind == JsonValueKind.Number
                && root.GetProperty(field).TryGetInt32(out int value) ? value : throw Invalid(field, "expected integer");
        }
        catch (Exception error) when (error is IOException or InvalidDataException or UnauthorizedAccessException or JsonException or ArgumentException)
        {
            warning(string.Format(CultureInfo.InvariantCulture, Localization.Get("TacticalBattle.InputDefaults"), path, error.Message));
            bool available = !Conflicts(Default.TacticalBattle, maneuver);
            if (!available) warning(Localization.Get("TacticalBattle.InputDefaultConflict"));
            return Default with { ShortcutAvailable = available };
        }
        InvalidDataException Invalid(string field, string reason) => new(field + ": " + reason);
    }

    private static bool Conflicts(Key key, ManeuverInputSettings maneuver) =>
        key is Key.I or Key.F5 or Key.F9 or Key.F10 or Key.F12 or Key.Escape or Key.Enter or Key.KeypadEnter
            or Key.Tab or Key.Number0 or Key.Keypad0 or Key.Number1 or Key.Number2 or Key.Number3 or Key.Number4 or Key.Number5
            or Key.Space or Key.C or Key.F or Key.S or Key.Equal or Key.Minus or Key.KeypadAdd or Key.KeypadSubtract or Key.Home or Key.End
        || key.ToString().StartsWith("Control", StringComparison.Ordinal)
        || key.ToString().StartsWith("Shift", StringComparison.Ordinal)
        || key.ToString().StartsWith("Alt", StringComparison.Ordinal)
        || maneuver.MappedKeys.Contains(key);
}
