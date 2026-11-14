using System.Text.Json.Serialization;

namespace DeepSpaceSaga.Engine.Scenario;

/// <summary>
/// Persisted carrier state attached to the world player ship (EP-0029). Schema 1 keeps only
/// the independent runtime hull current; hull max comes from the ship class profile and is
/// never duplicated here. Later stories extend this record with hangar slots, crew roster,
/// owned drones and torpedo magazines without breaking older readers (optional members).
/// </summary>
public sealed record CarrierStateData(
    [property: JsonPropertyName("schemaVersion")] int SchemaVersion,
    [property: JsonPropertyName("revision")] int Revision,
    [property: JsonPropertyName("hullCurrent")] int HullCurrent,
    [property: JsonPropertyName("reservedCabins")] int ReservedCabins = 2)
{
    public const int CurrentSchemaVersion = 1;
}
