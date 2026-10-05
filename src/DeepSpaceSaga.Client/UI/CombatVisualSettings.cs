using System.Globalization;
using System.Text.Json;
using SkiaSharp;

namespace DeepSpaceSaga.Client.UI;

/// <summary>Startup-only presentation palette. Hex strings use RGBA, not Skia's ARGB syntax.</summary>
public sealed record CombatVisualSettings(
    SKColor Torpedo, SKColor Trail, SKColor Prediction, SKColor Intercept,
    SKColor Preview, SKColor HullHp, SKColor Explosion, SKColor Wreck)
{
    public SKColor Countermeasure { get; init; } = new(0, 204, 255);
    public SKColor CountermeasureTrail { get; init; } = new(0, 204, 255);
    public SKColor CountermeasurePrediction { get; init; } = new(0, 204, 255, 136);
    public SKColor CountermeasureIntercept { get; init; } = new(0, 204, 255);
    public SKColor DefenseRange { get; init; } = new(0, 204, 255, 85);
    public SKColor DefenseText { get; init; } = new(153, 238, 255);
    public const string RelativePath = "Data/UI/combat-visuals.json";
    public static CombatVisualSettings Default { get; } = new(
        SKColors.Yellow, SKColors.Yellow, new(255, 255, 0, 102), SKColors.Yellow,
        new(128, 128, 128), SKColors.Lime, SKColors.Red, new(128, 128, 128));

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
                DefenseText = Optional("defenseText", Default.DefenseText)
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
