namespace DeepSpaceSaga.Contracts;

/// <summary>
/// Immutable per-module command metadata for client-side UI rendering —
/// display name and target requirement for one command of an installed module.
/// Produced by the Engine from content definitions; never written to
/// save/scenario schema.
/// </summary>
/// <remarks>
/// Target values: "none" (no target), "point" (world-coordinate target, e.g.
/// engine.navigate-to-point), "object" (target object id required, e.g. match
/// and scanner commands).
/// </remarks>
public sealed record ModuleCommandSnapshot(
    string CommandTypeId,
    string DisplayName,
    string Target = "none");
