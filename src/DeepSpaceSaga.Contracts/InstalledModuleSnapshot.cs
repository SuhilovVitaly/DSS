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
    /// <summary>
    /// Fuel tank capacity in kg. A static module-type attribute (direct pass-through of
    /// <c>ModuleTypeDefinition.FuelCapacityKg</c>, like <see cref="CabinesCount"/>), unlike
    /// <see cref="FuelAmountKg"/> which is runtime state. Null for module types without
    /// fuel capacity.
    /// </summary>
    long? FuelCapacityKg = null,
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
    /// item.unitMassKg)</c>, computed over this module's <see cref="Cargo"/>. Authoritative Engine
    /// runtime state (mirrors the <see cref="FuelAmountKg"/> convention above): computed once when
    /// the module is created and updated whenever its Cargo changes, rather than recomputed at
    /// snapshot/projection time. Null for modules without CargoCapacityKg.
    /// </summary>
    long? AvailableCapacityKg = null,
    /// <summary>
    /// Number of crew cabins provided by this module type (story-20260901-112254). A static
    /// module-type attribute, unlike <see cref="FuelAmountKg"/>/<see cref="AvailableCapacityKg"/>
    /// which depend on runtime state — direct pass-through of
    /// <c>ModuleTypeDefinition.CabinesCount</c>. Null for module types that do not house crew.
    /// </summary>
    int? CabinesCount = null,
    /// <summary>Total cargo capacity of this container; fuel tanks have their own capacity.</summary>
    long? CargoCapacityKg = null,
    /// <summary>
    /// Authoritative launcher parameters and active projectile identity. Null for legacy
    /// and non-launcher modules; absence never grants the ability to fire. Its Operator
    /// is the authoritative assignment, distinct from an assigned zero-skill operator.
    /// </summary>
    LauncherCombatSnapshot? LauncherCombat = null,
    DefenseSnapshot? Defense = null,
    /// <summary>Current authoritative operator for either weapon family; null means unassigned.</summary>
    WeaponOperatorSnapshot? Operator = null,
    /// <summary>Eligibility for this exact module and target; never an aggregate over all defense modules.</summary>
    DefenseLaunchPreview? DefensePreview = null,
    /// <summary>Authoritative combat armor remaining. Populated by the module-health runtime.</summary>
    int ArmorCurrent = 0,
    int ArmorMax = 0,
    /// <summary>Authoritative combat structure, separate from the older operational StructurePoints.</summary>
    int StructureCurrent = 0,
    int StructureMax = 0,
    /// <summary>Absolute physical time when a non-destroyed module becomes available again.</summary>
    double? DisabledUntilMotionTimeMs = null,
    bool IsDestroyed = false,
    string? CombatRole = null,
    /// <summary>Snapshot-only maximum life of a newly launched torpedo, in physical ms.
    /// Null means unavailable; this module projection is never part of save state.</summary>
    [property: JsonPropertyName("torpedoMaxFlightTimeMs"), JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    long? TorpedoMaxFlightTimeMs = null,
    /// <summary>Own module criticality under the authoritative configured destruction rules.</summary>
    [property: JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    bool IsCritical = false);
