namespace DeepSpaceSaga.Engine.Content;

/// <summary>
/// Factor values are stored as fixed-point integers where 1000 = 1.0.
/// This avoids <see cref="double"/> / <see cref="float"/> as the authoritative
/// runtime representation (§56.3).
/// </summary>
internal sealed record CommandDefinition(
    string TypeId,
    string DisplayName,
    int TimeFactor = FactorDefaults.Default,
    int ComplexityFactor = FactorDefaults.Default,
    int ConsumptionFactor = FactorDefaults.Default,
    /// <summary>
    /// Target requirement: "none" (no target), "point" (world-coordinate target,
    /// e.g. engine.orbit), "object" (target object id required, e.g.
    /// match and scanner commands). Drives client-side command button enablement.
    /// </summary>
    string Target = "none",
    /// <summary>
    /// Activation cost in energy cells to start executing this command (§56.4).
    /// 0 = no activation cost. Schema/validation prep only: the Engine does not
    /// yet debit this value (the energy system is not implemented).
    /// </summary>
    int ActivationEnergyCellsCost = 0) : ITypeDefinition
{
    /// <summary>Fixed-point constant for the neutral factor value 1.0.</summary>
    public const int Neutral = 1000;
}

/// <summary>Well-known fixed-point factor defaults for command definitions.</summary>
internal static class FactorDefaults
{
    /// <summary>1000 = 1.0 — the neutral multiplicative identity.</summary>
    public const int Default = 1000;
}
