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
    public PointCombatVisualSettings PointCombat { get; init; } = PointCombatVisualSettings.Default;
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
                PointCombat = ReadPointCombat(),
                ForecastHorizonMs = root.TryGetProperty("forecastHorizonMs", out var horizon) &&
                    horizon.ValueKind == JsonValueKind.Number && horizon.TryGetInt64(out long ms) && ms is > 0 and <= 200_000 ? ms : 200_000
            };

            PointCombatVisualSettings ReadPointCombat()
            {
                if (!root.TryGetProperty("pointCombat", out var group)) return PointCombatVisualSettings.Default;
                if (group.ValueKind != JsonValueKind.Object) throw Invalid("pointCombat", "expected an object");
                var expected = typeof(PointCombatVisualSettings).GetProperties()
                    .Where(p => p.GetMethod?.IsStatic == false).ToDictionary(p => JsonNamingPolicy.CamelCase.ConvertName(p.Name), StringComparer.Ordinal);
                var names = new HashSet<string>(StringComparer.Ordinal);
                foreach (var property in group.EnumerateObject())
                {
                    if (!names.Add(property.Name)) throw Invalid("pointCombat." + property.Name, "duplicate key");
                    if (!expected.ContainsKey(property.Name)) throw Invalid("pointCombat." + property.Name, "unknown key");
                }
                var value = PointCombatVisualSettings.Default with { };
                foreach (var (key, property) in expected)
                {
                    string fullKey = "pointCombat." + key;
                    if (!group.TryGetProperty(key, out var element)) throw Invalid(fullKey, "required field");
                    if (property.PropertyType == typeof(SKColor)) property.SetValue(value, ParseColor(element, fullKey));
                    else
                    {
                        if (element.ValueKind != JsonValueKind.Number || !element.TryGetDouble(out double number) ||
                            !double.IsFinite(number) || number <= 0 || number > 10000)
                            throw Invalid(fullKey, "expected finite presentation value in (0,10000]");
                        property.SetValue(value, number);
                    }
                }
                if (value.MinimumBlastMarkerDiameterPx < 6)
                    throw Invalid("pointCombat.minimumBlastMarkerDiameterPx", "expected at least 6px");
                return value;
            }

            // New fields remain compatible with v1 palettes. Invalid optional fields use the documented default.
            SKColor Optional(string key, SKColor fallback)
            {
                try { return Color(key); }
                catch (InvalidDataException) { return fallback; }
            }

            SKColor Color(string key)
            {
                if (!root.TryGetProperty(key, out var element)) throw Invalid(key, "expected #RRGGBBAA");
                return ParseColor(element, key);
            }

            SKColor ParseColor(JsonElement element, string key)
            {
                if (element.ValueKind != JsonValueKind.String || element.GetString() is not { Length: 9 } value || value[0] != '#' ||
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

/// <summary>Presentation-only styles for point combat; no physical radius, damage or timing rules.</summary>
public sealed record PointCombatVisualSettings
{
    public SKColor BlastFill { get; init; } = new(237, 184, 106, 32);
    public SKColor BlastStroke { get; init; } = new(237, 184, 106, 170);
    public SKColor BlastCoreFill { get; init; } = new(237, 184, 106, 64);
    public SKColor CounterBlastFill { get; init; } = new(118, 213, 220, 40);
    public SKColor CounterBlastStroke { get; init; } = new(118, 213, 220, 179);
    public SKColor ShotDown { get; init; } = TacticalMapPalette.Countermeasure;
    public SKColor OwnVelocity { get; init; } = SpaceMapColorResolver.PlayerShipColor;
    public SKColor OwnPath { get; init; } = SpaceMapColorResolver.PlayerShipColor.WithAlpha(160);
    public SKColor ForeignTrace { get; init; } = TacticalMapPalette.Torpedo.WithAlpha(128);
    public SKColor ForeignProfileText { get; init; } = TacticalMapPalette.Annotation;
    public SKColor TargetShadow { get; init; } = TacticalMapPalette.Preview.WithAlpha(140);
    public SKColor Threat { get; init; } = new(238, 167, 122, 255);
    public SKColor Danger { get; init; } = new(233, 134, 127, 255);
    public SKColor Safe { get; init; } = new(143, 187, 167, 255);
    public SKColor AimReticle { get; init; } = TacticalMapPalette.Preview;
    public double LineWidthPx { get; init; } = 1.5;
    public double TextSizePx { get; init; } = 12;
    public double DashLengthPx { get; init; } = 6;
    public double DashGapPx { get; init; } = 4;
    public double ProjectileMarkerRadiusPx { get; init; } = 3;
    public double MinimumBlastMarkerDiameterPx { get; init; } = 6;
    public double ReticleRadiusPx { get; init; } = 8;
    public double BlastDurationRealMs { get; init; } = 600;
    public double ShotDownDurationRealMs { get; init; } = 350;
    public double ThreatPulsePeriodRealMs { get; init; } = 800;
    public static PointCombatVisualSettings Default { get; } = new();
}
