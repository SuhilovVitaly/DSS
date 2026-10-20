using System.Globalization;
using System.Text.Json;
using SkiaSharp;

namespace DeepSpaceSaga.Client.UI;

/// <summary>Startup-only presentation palette. Hex strings use RGBA, not Skia's ARGB syntax.</summary>
public sealed record CombatVisualSettings(
    SKColor Torpedo, SKColor Trail, SKColor Prediction, SKColor Intercept,
    SKColor Preview, SKColor HullHp, SKColor Explosion, SKColor Wreck)
{
    public SKColor Countermeasure { get; init; } = TacticalMapPalette.Countermeasure;
    public SKColor CountermeasureTrail { get; init; } = TacticalMapPalette.Countermeasure.WithAlpha(102);
    public SKColor CountermeasurePrediction { get; init; } = TacticalMapPalette.Countermeasure.WithAlpha(140);
    public SKColor CountermeasureIntercept { get; init; } = TacticalMapPalette.Countermeasure;
    public SKColor DefenseRange { get; init; } = TacticalMapPalette.Countermeasure.WithAlpha(46);
    public SKColor DefenseText { get; init; } = TacticalMapPalette.Annotation;
    public SKColor TorpedoRange { get; init; } = TacticalMapPalette.Torpedo.WithAlpha(46);
    public SKColor ManualDefenseRange { get; init; } = TacticalMapPalette.Countermeasure.WithAlpha(71);
    internal SKColor RangeColor(string kind) => kind switch
    {
        "torpedo" => TorpedoRange,
        "manual" => ManualDefenseRange,
        _ => DefenseRange
    };
    public long ForecastHorizonMs { get; init; } = 200_000;
    public const string RelativePath = "Data/UI/combat-visuals.json";
    public static CombatVisualSettings Default { get; } = new(
        TacticalMapPalette.Torpedo, TacticalMapPalette.Torpedo, TacticalMapPalette.Torpedo.WithAlpha(128), TacticalMapPalette.Torpedo,
        TacticalMapPalette.Preview, new(143, 187, 167, 217), TacticalMapPalette.Explosion, TacticalMapPalette.Wreck);

    public static CombatVisualSettings Load(string path)
    {
        try
        {
            using var document = JsonDocument.Parse(File.ReadAllText(path));
            var root = document.RootElement;
            if (root.ValueKind != JsonValueKind.Object)
                throw Invalid("root", "expected an object");
            var keys = new HashSet<string>(StringComparer.Ordinal);
            foreach (var property in root.EnumerateObject())
                if (!keys.Add(property.Name)) throw Invalid(property.Name, "duplicate key");
            if (!root.TryGetProperty("schemaVersion", out var version) ||
                version.ValueKind != JsonValueKind.Number || !version.TryGetInt32(out int schema) || schema != 1)
                throw Invalid("schemaVersion", "expected 1");
            return new(Color("torpedo"), Color("trail"), Color("prediction"), Color("intercept"),
                Color("preview"), Color("hullHp"), Color("explosion"), Color("wreck"))
            {
                Countermeasure = Optional("countermeasure", Default.Countermeasure),
                CountermeasureTrail = Optional("countermeasureTrail", Default.CountermeasureTrail),
                CountermeasurePrediction = Optional("countermeasurePrediction", Default.CountermeasurePrediction),
                CountermeasureIntercept = Optional("countermeasureIntercept", Default.CountermeasureIntercept),
                DefenseRange = Optional("defenseRange", Default.DefenseRange),
                DefenseText = Optional("defenseText", Default.DefenseText),
                TorpedoRange = Optional("torpedoRange", Default.TorpedoRange),
                ManualDefenseRange = Optional("manualDefenseRange", Default.ManualDefenseRange),
                ForecastHorizonMs = root.TryGetProperty("forecastHorizonMs", out var horizon) &&
                    horizon.ValueKind == JsonValueKind.Number && horizon.TryGetInt64(out long ms) && ms is > 0 and <= 200_000 ? ms : 200_000
            };

            // New fields remain compatible with v1 palettes. Invalid optional fields use the documented default.
            SKColor Optional(string key, SKColor fallback)
            {
                try { return Color(key); }
                catch (InvalidDataException) { return fallback; }
            }

            SKColor Color(string key)
            {
                if (!root.TryGetProperty(key, out var element) || element.ValueKind != JsonValueKind.String ||
                    element.GetString() is not { Length: 9 } value || value[0] != '#' ||
                    !uint.TryParse(value.AsSpan(1), NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture, out uint rgba))
                    throw Invalid(key, "expected #RRGGBBAA");
                return new((byte)(rgba >> 24), (byte)(rgba >> 16), (byte)(rgba >> 8), (byte)rgba);
            }
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException)
        {
            throw new InvalidDataException($"Combat palette '{path}': {ex.Message}", ex);
        }

        InvalidDataException Invalid(string key, string reason) =>
            new($"Combat palette '{path}', key '{key}': {reason}.");
    }
}
