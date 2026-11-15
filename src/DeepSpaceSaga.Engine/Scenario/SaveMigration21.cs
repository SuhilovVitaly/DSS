namespace DeepSpaceSaga.Engine.Scenario;

/// <summary>
/// Upgrades a world-only Save20 into Save21. The carrier surface (hull + owned wing) is
/// optional: a legacy ship without <c>carrierState</c> simply has no carrier surface — it is
/// never granted starter assets. New torpedo SKUs (<c>item.drone-torpedo.*</c>) are not touched
/// here; the legacy <c>item.ammo.torpedo*</c> filter already ran in SaveMigration20.
/// </summary>
internal static class SaveMigration21
{
    internal const int PreviousVersion = 20;
    internal const int TargetVersion = 21;

    internal static ScenarioFile Upgrade(ScenarioFile scenario, bool allowNonZeroGameTime = true)
    {
        if (scenario.GameState is null) throw new ScenarioException("Missing gameState.");
        if (scenario.SaveFormatVersion == TargetVersion)
            return scenario;
        if (scenario.SaveFormatVersion != PreviousVersion)
            throw new ScenarioException($"SaveMigration21 cannot upgrade version {scenario.SaveFormatVersion}.");
        SaveMigration20.RequireWorldOnly(scenario.GameState);
        return scenario with { SaveFormatVersion = TargetVersion };
    }
}
