namespace DeepSpaceSaga.Contracts;

/// <summary>
/// Engine-owned manual launch eligibility for one module and the explicitly identified
/// selected torpedo. A missing chance means no eligible torpedo; zero chance is a valid
/// value and does not itself forbid a manual launch. Geometry is evaluated by shared
/// Motion from confirmed snapshots, without changing these authoritative facts.
/// </summary>
public sealed record DefenseLaunchPreview(
    string OwnerObjectId,
    string ModuleId,
    string? TargetTorpedoId,
    int? ChanceTenths,
    bool CanFire,
    string? Reason = null,
    decimal? Accuracy = null,
    decimal? TargetManeuverability = null);
