namespace DeepSpaceSaga.Contracts;

/// <summary>
/// One stack of cargo carried inside a specific installed module (see
/// <see cref="InstalledModuleSnapshot.Cargo"/>). Mirrors the save-schema shape of
/// <c>CargoStackData</c> — <see cref="Quantity"/> is a unit count, not a mass;
/// total mass is Quantity × the item type's unitMassKg (see item-types.json), checked
/// against the module's CargoCapacityKg authoritatively by the engine.
/// </summary>
public sealed record CargoStackSnapshot(
    string ItemTypeId,
    long Quantity);
