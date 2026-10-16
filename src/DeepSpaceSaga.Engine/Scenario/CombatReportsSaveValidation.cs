namespace DeepSpaceSaga.Engine.Scenario;

internal static class CombatReportsSaveValidation
{
    internal static void Validate(CombatReportsSaveData? state)
    {
        if (state is null) return; // Pre-feature Save17 and New Game.
        if (state.Version != 1 || state.Sequence < 0 || state.ReportedRangeTargetIds is null)
            throw new ScenarioException("Invalid combatReportsState version, sequence or range IDs.");
        var ids = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var id in state.ReportedRangeTargetIds)
            if (string.IsNullOrWhiteSpace(id) || id != id.Trim() || id.Any(char.IsControl) || !ids.Add(id))
                throw new ScenarioException("Invalid or duplicate combatReportsState target ID.");
    }
}
