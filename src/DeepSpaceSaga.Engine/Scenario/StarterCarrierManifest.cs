using System.Text.Json;
using System.Text.Json.Serialization;

namespace DeepSpaceSaga.Engine.Scenario;

/// <summary>
/// One shared starter carrier definition (EP-0029 US-0011). A New Game scenario references this
/// file through <see cref="ScenarioMetadata.StarterCarrierManifest"/> instead of duplicating the
/// hull layout, modules, crew and wing inline; the loader materializes the four fields onto the
/// world player ship before scenario validation. Schema 1 is complete: the materialized carrier is
/// a full snapshot, so a save never references the manifest again.
/// </summary>
public sealed record StarterCarrierManifest(
    [property: JsonPropertyName("schemaVersion")] int SchemaVersion,
    [property: JsonPropertyName("shipClassId")] string? ShipClassId = null,
    [property: JsonPropertyName("hullLayout")] HullLayoutData? HullLayout = null,
    [property: JsonPropertyName("modules")] IReadOnlyList<ShipModuleData>? Modules = null,
    [property: JsonPropertyName("crew")] IReadOnlyList<ShipCrewMemberData>? Crew = null,
    [property: JsonPropertyName("carrierState")] CarrierStateData? CarrierState = null)
{
    public const int CurrentSchemaVersion = 1;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
    };

    /// <summary>Read and validate one manifest file; every failure is a <see cref="ScenarioException"/>.</summary>
    public static StarterCarrierManifest Load(string path)
    {
        StarterCarrierManifest? manifest;
        try
        {
            manifest = JsonSerializer.Deserialize<StarterCarrierManifest>(File.ReadAllText(path), JsonOptions);
        }
        catch (JsonException ex)
        {
            throw new ScenarioException($"Invalid starterCarrierManifest JSON in {path}: {ex.Message}", ex);
        }
        catch (IOException ex)
        {
            throw new ScenarioException($"Failed to read starterCarrierManifest file: {path}", ex);
        }

        if (manifest is null)
            throw new ScenarioException($"starterCarrierManifest JSON deserialized to null: {path}.");

        manifest.Validate(path);
        return manifest;
    }

    private void Validate(string path)
    {
        if (SchemaVersion != CurrentSchemaVersion)
            throw new ScenarioException($"{path}: unsupported starterCarrierManifest schemaVersion {SchemaVersion}.");
        if (ShipClassId is not null && string.IsNullOrWhiteSpace(ShipClassId))
            throw new ScenarioException($"{path}: starterCarrierManifest shipClassId must not be blank.");
        if (HullLayout is null)
            throw new ScenarioException($"{path}: starterCarrierManifest requires hullLayout.");
        if (Modules is not { Count: > 0 })
            throw new ScenarioException($"{path}: starterCarrierManifest requires a non-empty modules array.");
        if (Crew is not { Count: > 0 })
            throw new ScenarioException($"{path}: starterCarrierManifest requires a non-empty crew array.");
        if (CarrierState is null)
            throw new ScenarioException($"{path}: starterCarrierManifest requires carrierState.");

        // EP-0034 US-0001: a derived manifest (e.g. the demo starter with extra quarters) is rejected at the
        // file boundary when a module repeats an id, leaves the hull or overlaps another module.
        var hull = HullLayout.Cells is { } cells ? cells.Where(c => c is not null).Select(c => (c.X, c.Y)).ToHashSet() : [];
        var moduleIds = new HashSet<string>(StringComparer.Ordinal);
        var occupied = new HashSet<(int X, int Y)>();
        foreach (var module in Modules)
        {
            if (module is null || string.IsNullOrWhiteSpace(module.ModuleId) || !moduleIds.Add(module.ModuleId))
                throw new ScenarioException($"{path}: starterCarrierManifest module ids must be present and unique.");
            foreach (var cell in module.OccupiedCells ?? [])
            {
                if (cell is null || !hull.Contains((cell.X, cell.Y)))
                    throw new ScenarioException($"{path}: module '{module.ModuleId}' occupies a cell outside the hull.");
                if (!occupied.Add((cell.X, cell.Y)))
                    throw new ScenarioException($"{path}: module '{module.ModuleId}' overlaps another module at ({cell.X},{cell.Y}).");
            }
        }
    }
}
