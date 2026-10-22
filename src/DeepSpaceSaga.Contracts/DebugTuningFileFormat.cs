using System.Collections.Immutable;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace DeepSpaceSaga.Contracts;

/// <summary>Shared schema1 file syntax. Numeric authority remains in Engine.</summary>
public static class DebugTuningFileFormat
{
    public static string Serialize(ImmutableArray<DebugTuningValue> values, DateTimeOffset writtenAtUtc, bool catalog = false)
    {
        var ships = new JsonObject(); var launchers = new JsonObject();
        foreach (var value in values)
        {
            var section = value.ModuleTypeId.StartsWith("module.engine.", StringComparison.Ordinal) ? ships : launchers;
            var module = (JsonObject)(section[value.ModuleTypeId] ??= new JsonObject());
            var fields = value.ProfileId is not { } profile ? module :
                (JsonObject)(((JsonObject)(module["profiles"] ??= new JsonObject()))[profile] ??= new JsonObject());
            fields[value.Field] = value.Value;
        }
        var root = new JsonObject
        {
            ["schemaVersion"] = 1,
            ["writtenAtUtc"] = writtenAtUtc.UtcDateTime.ToString("O", CultureInfo.InvariantCulture)
        };
        if (catalog) root["source"] = "catalog";
        root["ships"] = ships; root["launchers"] = launchers;
        return root.ToJsonString(new JsonSerializerOptions { WriteIndented = true }) + "\n";
    }

    public static bool TryParse(string json, ImmutableArray<DebugTuningValue> defaults, out DebugTuningPatch patch, out string? reason)
    {
        patch = new([]); reason = null;
        try
        {
            using var document = JsonDocument.Parse(json);
            var values = ImmutableArray.CreateBuilder<DebugTuningValue>();
            bool hasVersion = false;
            foreach (var property in Properties(document.RootElement))
            {
                switch (property.Name)
                {
                    case "schemaVersion":
                        if (property.Value.ValueKind != JsonValueKind.Number || !property.Value.TryGetInt32(out int version) || version != 1)
                            throw new JsonException("schemaVersion must equal 1.");
                        hasVersion = true; break;
                    case "writtenAtUtc":
                        if (property.Value.ValueKind != JsonValueKind.String || !DateTimeOffset.TryParse(property.Value.GetString(),
                            CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out _))
                            throw new JsonException("writtenAtUtc must be a UTC timestamp string.");
                        break;
                    case "source":
                        if (property.Value.ValueKind != JsonValueKind.String || property.Value.GetString() != "catalog")
                            throw new JsonException("source must equal catalog.");
                        break;
                    case "ships": ReadModules(property.Value, true); break;
                    case "launchers": ReadModules(property.Value, false); break;
                    default: throw new JsonException("Unknown root key: " + property.Name);
                }
            }
            if (!hasVersion) throw new JsonException("Missing schemaVersion.");
            patch = new(values.ToImmutable());
            return true;

            void ReadModules(JsonElement section, bool ship)
            {
                foreach (var module in Properties(section))
                {
                    if (module.Name.StartsWith("module.engine.", StringComparison.Ordinal) != ship ||
                        !defaults.Any(v => v.ModuleTypeId == module.Name)) throw new JsonException("Unknown module type: " + module.Name);
                    foreach (var field in Properties(module.Value))
                    {
                        if (field.Name == "profiles" && !ship)
                        {
                            foreach (var profile in Properties(field.Value))
                            {
                                if (!defaults.Any(v => v.ModuleTypeId == module.Name && v.ProfileId == profile.Name))
                                    throw new JsonException("Unknown profile: " + profile.Name);
                                foreach (var item in Properties(profile.Value)) Add(module.Name, item, profile.Name);
                            }
                        }
                        else Add(module.Name, field, null);
                    }
                }
            }
            void Add(string type, JsonProperty field, string? profile)
            {
                if (!defaults.Any(v => v.ModuleTypeId == type && v.Field == field.Name && v.ProfileId == profile))
                    throw new JsonException("Unknown parameter: " + type + "/" + profile + "/" + field.Name);
                if (field.Value.ValueKind != JsonValueKind.Number || !field.Value.TryGetDouble(out double value) || !double.IsFinite(value))
                    throw new JsonException("Parameter must be a finite number: " + field.Name);
                values.Add(new(type, field.Name, value, profile));
            }
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
