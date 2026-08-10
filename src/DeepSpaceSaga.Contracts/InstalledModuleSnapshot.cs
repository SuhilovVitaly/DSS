using System.Collections.Immutable;

namespace DeepSpaceSaga.Contracts;

/// <summary>
/// Minimal immutable projection of an installed module for client-side UI rendering.
/// Produced by the Engine in <see cref="AuthoritativeSnapshot"/> and consumed by the
/// Commands Panel — never written to save/scenario schema.
/// </summary>
public sealed record InstalledModuleSnapshot(
    string ModuleId,
    string ModuleTypeId,
    string DisplayName,
    int Position,
    ImmutableArray<string> CommandTypeIds);
