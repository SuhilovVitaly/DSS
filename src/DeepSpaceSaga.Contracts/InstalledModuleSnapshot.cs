using System.Collections.Immutable;
using System.Text.Json.Serialization;

namespace DeepSpaceSaga.Contracts;

/// <summary>
/// Immutable projection of an installed module for client-side UI rendering,
/// including runtime status so the Commands Panel can show module state and
/// block commands on busy/broken modules. Produced by the Engine in
/// <see cref="AuthoritativeSnapshot"/> — never written to save/scenario schema.
/// </summary>
public sealed record InstalledModuleSnapshot(
    string ModuleId,
    string ModuleTypeId,
    string DisplayName,
    int Position,
    ImmutableArray<string> CommandTypeIds,
    string PowerState = "Off",
    string OperationalState = "Ready",
    int StructurePoints = 0,
    string? ActiveCommandType = null,
    long? FuelAmountKg = null,
    [property: JsonConverter(typeof(ImmutableArrayDefaultJsonConverter<ModuleCommandSnapshot>))]
    ImmutableArray<ModuleCommandSnapshot> Commands = default);
