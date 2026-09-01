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
    ImmutableArray<ModuleCommandSnapshot> Commands = default,
    /// <summary>
    /// Cargo carried inside this specific module. Empty for modules without
    /// CargoCapacityKg. Cargo of this module-container only — aggregation across
    /// multiple modules is a Client concern, not an Engine one.
    /// </summary>
    [property: JsonConverter(typeof(ImmutableArrayDefaultJsonConverter<CargoStackSnapshot>))]
    ImmutableArray<CargoStackSnapshot> Cargo = default,
    /// <summary>
    /// Remaining cargo capacity in kg: <c>cargoCapacityKg - sum(item_in_cargo.quantity *
    /// item.unitMassKg)</c>, computed over this module's <see cref="Cargo"/>. Null for modules
    /// without CargoCapacityKg (mirrors the <see cref="FuelAmountKg"/> convention above).
    /// </summary>
    long? AvailableCapacityKg = null);
