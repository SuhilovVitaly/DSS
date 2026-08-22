using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Engine.Scenario;

namespace DeepSpaceSaga.Engine.LocalClient;

/// <summary>
/// Enumerates playable scenario files on disk for the client's New Game -&gt; scenario
/// picker. Mirrors <see cref="SaveSlotRepository"/>'s role for save slots, but reads each
/// file's own scenarioMetadata (Name/Description) rather than deriving display data purely
/// from the file system.
/// </summary>
public static class ScenarioRepository
{
    /// <summary>
    /// List every "scenario.json" found anywhere under scenariosDirectory, ordered by
    /// scenarioId. Returns an empty array if the directory doesn't exist. A scenario file
    /// that fails to parse/validate is skipped rather than crashing the picker — an
    /// operator error in one scenario file must not take down every other one.
    /// </summary>
    public static ScenarioInfo[] ListScenarios(string scenariosDirectory)
    {
        if (!Directory.Exists(scenariosDirectory))
            return Array.Empty<ScenarioInfo>();

        var result = new List<ScenarioInfo>();
        foreach (var path in Directory.EnumerateFiles(scenariosDirectory, "scenario.json", SearchOption.AllDirectories))
        {
            try
            {
                var scenario = ScenarioLoader.LoadFromFile(path);
                result.Add(new ScenarioInfo(
                    ScenarioPath: path,
                    ScenarioId: scenario.Metadata.ScenarioId,
                    Name: scenario.Metadata.Name,
                    Description: scenario.Metadata.Description ?? string.Empty));
            }
            catch (ScenarioException)
            {
                // Invalid scenario file — skip it, don't break the whole picker.
            }
        }

        return result.OrderBy(s => s.ScenarioId, StringComparer.Ordinal).ToArray();
    }
}
