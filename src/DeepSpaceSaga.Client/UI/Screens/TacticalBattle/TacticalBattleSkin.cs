using System.Collections.Immutable;
using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;
using SkiaSharp;

namespace DeepSpaceSaga.Client.UI.Screens.TacticalBattle;

internal sealed partial class TacticalBattleSkin
{
    internal const string ClassicId = "classic";
    internal const int MaxManifestBytes = 256 * 1024;

    internal static TacticalBattleSkin Classic { get; } = new(ClassicId, "Classic",
        ImmutableDictionary<string, ImmutableDictionary<string, object>>.Empty);

    private readonly ImmutableDictionary<string, ImmutableDictionary<string, object>> _roles;

    private TacticalBattleSkin(string id, string displayName,
        ImmutableDictionary<string, ImmutableDictionary<string, object>> roles)
    {
        Id = id;
        DisplayName = displayName;
        _roles = roles;
    }

    internal string Id { get; }
    internal string DisplayName { get; }

    internal SKColor Color(string role, string property, SKColor fallback) => Find(role, property, fallback);
    internal float Number(string role, string property, float fallback) => Find(role, property, fallback);
    internal ImmutableArray<float> Dash(string role) => Find(role, "dash", default(ImmutableArray<float>));
    internal string Shape(string role, string fallback = "native") => Find(role, "shape", fallback);
    internal string? Resource(string role, string property) => Find<string?>(role, property, null);

    internal float Metric(string role, string metric, float fallback) => Find(role, $"metrics.{metric}", fallback);

    internal IEnumerable<(string Role, string Property, string Path)> ResourceReferences =>
        from role in _roles
        from property in role.Value
        where property.Key is "image" or "font"
        select (role.Key, property.Key, (string)property.Value);

    private T Find<T>(string role, string property, T fallback)
    {
        while (true)
        {
            if (_roles.TryGetValue(role, out var values) && values.TryGetValue(property, out var value) && value is T typed)
                return typed;

            int parent = role.LastIndexOf('.');
            if (parent < 0)
                return fallback;
            role = role[..parent];
        }
    }

    internal static bool IsValidId(string? id) => id is { Length: > 0 and <= 64 } && KebabCase().IsMatch(id);

    internal static TacticalBattleSkin Parse(string json)
    {
        try
        {
            using var document = JsonDocument.Parse(json);
            var fields = Object(document.RootElement, "$");
            foreach (string name in fields.Keys)
                if (name is not ("schemaVersion" or "id" or "displayName" or "roles"))
                    throw Invalid(name, "unknown manifest field");

            var version = Required(fields, "schemaVersion");
            if (version.ValueKind != JsonValueKind.Number || !version.TryGetInt32(out int schema) || schema != 1)
                throw Invalid("schemaVersion", "expected version 1");

            string id = String(Required(fields, "id"), "id");
            if (!IsValidId(id) || id == ClassicId)
                throw Invalid("id", "expected a non-reserved lower ASCII kebab-case ID");

            string nameValue = String(Required(fields, "displayName"), "displayName");
            string nameTrimmed = nameValue.Trim();
            if (nameTrimmed.Length is 0 or > 80 || nameValue.Any(char.IsControl))
                throw Invalid("displayName", "expected a nonempty name of at most 80 characters without control characters");

            var roles = Object(Required(fields, "roles"), "roles");
            if (roles.Count > 512)
                throw Invalid("roles", "at most 512 roles are allowed");
            var parsed = ImmutableDictionary.CreateBuilder<string, ImmutableDictionary<string, object>>(StringComparer.Ordinal);
            foreach (var (role, value) in roles)
            {
                string field = $"roles.{role}";
                if (role.Length is 0 or > 128 || !RoleName().IsMatch(role))
                    throw Invalid(field, "invalid role name");
                parsed.Add(role, ReadRole(value, field));
            }

            return new TacticalBattleSkin(id, nameTrimmed, parsed.ToImmutable());
        }
        catch (JsonException ex)
        {
            throw new FormatException($"Invalid skin JSON at {ex.Path ?? "$"}, line {ex.LineNumber}, byte {ex.BytePositionInLine}.", ex);
        }
    }

    private static ImmutableDictionary<string, object> ReadRole(JsonElement value, string field)
    {
        var result = ImmutableDictionary.CreateBuilder<string, object>(StringComparer.Ordinal);
        foreach (var (name, property) in Object(value, field))
        {
            string path = $"{field}.{name}";
            if (name == "metrics")
            {
                foreach (var (metric, metricValue) in Object(property, path))
                {
                    string metricPath = $"{path}.{metric}";
                    if (!KebabCase().IsMatch(metric))
                        throw Invalid(metricPath, "invalid metric name");
                    result.Add($"metrics.{metric}", Bounded(metricValue, metricPath, -256, 512));
                }
                continue;
            }

            result.Add(name, name switch
            {
                "fill" or "stroke" or "text" => ReadColor(property, path),
                "opacity" => Bounded(property, path, 0, 1),
                "strokeWidth" => Bounded(property, path, 0, 32),
                "size" => Bounded(property, path, 1, 512),
                "fontSize" => Bounded(property, path, 6, 64),
                "radius" => Bounded(property, path, 0, 128),
                "dash" => ReadDash(property, path),
                "shape" => ReadShape(property, path),
                "image" or "font" => ReadResource(property, path, name),
                _ => throw Invalid(path, "unknown role property"),
            });
        }

        return result.ToImmutable();
    }

    private static SKColor ReadColor(JsonElement value, string field)
    {
        string hex = String(value, field);
        if (hex.Length is not (7 or 9) || hex[0] != '#' ||
            !uint.TryParse(hex.AsSpan(1), NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture, out uint rgba))
            throw Invalid(field, "expected #RRGGBB or #RRGGBBAA");
        if (hex.Length == 7)
            rgba = (rgba << 8) | 255;
        return new SKColor((byte)(rgba >> 24), (byte)(rgba >> 16), (byte)(rgba >> 8), (byte)rgba);
    }

    private static ImmutableArray<float> ReadDash(JsonElement value, string field)
    {
        if (value.ValueKind != JsonValueKind.Array || value.GetArrayLength() is not (0 or 2 or 4 or 6 or 8))
            throw Invalid(field, "expected zero, two, four, six or eight dash lengths");
        return value.EnumerateArray().Select((item, index) => Bounded(item, $"{field}[{index}]", 0.25, 128)).ToImmutableArray();
    }

    private static string ReadShape(JsonElement value, string field)
    {
        string shape = String(value, field);
        return shape is "native" or "circle" or "diamond" or "triangle" or "hexagon" or "square"
            ? shape
            : throw Invalid(field, "unknown marker shape");
    }

    private static string ReadResource(JsonElement value, string field, string kind)
    {
        string path = String(value, field);
        if (path.Length > 240 || !path.StartsWith("assets/", StringComparison.Ordinal) ||
            !AssetPath().IsMatch(path) || path.Split('/').Any(segment => segment is "." or ".." || segment.EndsWith('.')))
            throw Invalid(field, "expected a simple relative path below assets/");

        string ext = Path.GetExtension(path);
        bool validExtension = kind == "image"
            ? ext.Equals(".png", StringComparison.OrdinalIgnoreCase)
            : ext.Equals(".ttf", StringComparison.OrdinalIgnoreCase) || ext.Equals(".otf", StringComparison.OrdinalIgnoreCase);
        return validExtension ? path : throw Invalid(field, "unsupported asset extension");
    }

    private static float Bounded(JsonElement value, string field, double min, double max)
    {
        if (value.ValueKind != JsonValueKind.Number || !value.TryGetDouble(out double number) ||
            !double.IsFinite(number) || number < min || number > max)
            throw Invalid(field, FormattableString.Invariant($"expected a finite number from {min} to {max}"));
        return (float)number;
    }

    private static string String(JsonElement value, string field) =>
        value.ValueKind == JsonValueKind.String ? value.GetString()! : throw Invalid(field, "expected a string");

    private static Dictionary<string, JsonElement> Object(JsonElement value, string field)
    {
        if (value.ValueKind != JsonValueKind.Object)
            throw Invalid(field, "expected an object");
        var entries = new Dictionary<string, JsonElement>(StringComparer.Ordinal);
        foreach (var property in value.EnumerateObject())
            if (!entries.TryAdd(property.Name, property.Value))
                throw Invalid($"{field}.{property.Name}", "duplicate property");
        return entries;
    }

    private static JsonElement Required(Dictionary<string, JsonElement> fields, string name) =>
        fields.TryGetValue(name, out var value) ? value : throw Invalid(name, "required field is missing");

    private static FormatException Invalid(string field, string message) => new($"{field}: {message}.");

    [GeneratedRegex(@"\A[a-z][a-z0-9]*(?:-[a-z0-9]+)*\z", RegexOptions.CultureInvariant)]
    private static partial Regex KebabCase();

    [GeneratedRegex(@"\A[a-z][a-z0-9-]*(?:\.[a-z][a-z0-9-]*)*\z", RegexOptions.CultureInvariant)]
    private static partial Regex RoleName();

    [GeneratedRegex(@"\Aassets/(?:[A-Za-z0-9._-]+/)*[A-Za-z0-9._-]+\z", RegexOptions.CultureInvariant)]
    private static partial Regex AssetPath();
}
