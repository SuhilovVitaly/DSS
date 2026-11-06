using System.Collections.Immutable;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace DeepSpaceSaga.Contracts;

/// <summary>File syntax for world propulsion tuning. Engine owns parameter bounds.</summary>
public static class DebugTuningFileFormat
{
    public const int SchemaVersion = 2;
    private const int PreviousSchemaVersion = 1;

    public static string Serialize(ImmutableArray<DebugTuningValue> values, DateTimeOffset writtenAtUtc, bool catalog = false)
    {
        var ships = new JsonObject();
        foreach (var value in values)
        {
            if (!value.ModuleTypeId.StartsWith("module.engine.", StringComparison.Ordinal) || value.ProfileId is not null)
                throw new ArgumentException("Only world engine parameters may be written.", nameof(values));
            var module = (JsonObject)(ships[value.ModuleTypeId] ??= new JsonObject());
            module[value.Field] = value.Value;
        }
        var root = new JsonObject
        {
            ["schemaVersion"] = SchemaVersion,
            ["writtenAtUtc"] = writtenAtUtc.UtcDateTime.ToString("O", CultureInfo.InvariantCulture)
        };
        if (catalog) root["source"] = "catalog";
        root["ships"] = ships;
        return root.ToJsonString(new JsonSerializerOptions { WriteIndented = true }) + "\n";
    }

    public static bool TryParse(string json, ImmutableArray<DebugTuningValue> defaults, out DebugTuningPatch patch, out string? reason)
    {
        patch = new([]);
        reason = null;
        try
        {
            using var document = JsonDocument.Parse(json);
            var values = ImmutableArray.CreateBuilder<DebugTuningValue>();
            int version = 0;
            JsonElement? legacyLaunchers = null;
            JsonElement? ships = null;
            foreach (var property in Properties(document.RootElement))
            {
                switch (property.Name)
                {
                    case "schemaVersion":
                        if (property.Value.ValueKind != JsonValueKind.Number || !property.Value.TryGetInt32(out version) ||
                            version is not (PreviousSchemaVersion or SchemaVersion))
                            throw new JsonException($"schemaVersion must equal {PreviousSchemaVersion} or {SchemaVersion}.");
                        break;
                    case "writtenAtUtc":
                        if (property.Value.ValueKind != JsonValueKind.String || !DateTimeOffset.TryParse(property.Value.GetString(),
                            CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out _))
                            throw new JsonException("writtenAtUtc must be a UTC timestamp string.");
                        break;
                    case "source":
                        if (property.Value.ValueKind != JsonValueKind.String || property.Value.GetString() != "catalog")
                            throw new JsonException("source must equal catalog.");
                        break;
                    case "ships": ships = property.Value; break;
                    case "launchers": legacyLaunchers = property.Value; break;
                    default: throw new JsonException("Unknown root key: " + property.Name);
                }
            }
            if (version == 0) throw new JsonException("Missing schemaVersion.");
            if (legacyLaunchers is { } launchers &&
                (version != PreviousSchemaVersion || launchers.ValueKind != JsonValueKind.Object || launchers.EnumerateObject().Any()))
                throw new JsonException("Realtime combat tuning was removed. Delete 'launchers' and keep your engine overrides under 'ships'.");
            if (ships is { } section)
            {
                foreach (var module in Properties(section))
                {
                    if (!module.Name.StartsWith("module.engine.", StringComparison.Ordinal) ||
                        !defaults.Any(v => v.ModuleTypeId == module.Name && v.ProfileId is null))
                        throw new JsonException("Unknown engine module type: " + module.Name);
                    foreach (var field in Properties(module.Value))
                    {
                        if (!defaults.Any(v => v.ModuleTypeId == module.Name && v.Field == field.Name && v.ProfileId is null))
                            throw new JsonException("Unknown parameter: " + module.Name + "/" + field.Name);
                        if (field.Value.ValueKind != JsonValueKind.Number || !field.Value.TryGetDouble(out double value) || !double.IsFinite(value))
                            throw new JsonException("Parameter must be a finite number: " + field.Name);
                        values.Add(new(module.Name, field.Name, value));
                    }
                }
            }
            patch = new(values.ToImmutable());
            return true;
        }
        catch (JsonException error) { reason = error.Message; return false; }
    }

    private static IEnumerable<JsonProperty> Properties(JsonElement element)
    {
        if (element.ValueKind != JsonValueKind.Object) throw new JsonException("Expected an object.");
        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (var property in element.EnumerateObject())
        {
            if (!seen.Add(property.Name)) throw new JsonException("Duplicate key: " + property.Name);
            yield return property;
        }
    }
}
