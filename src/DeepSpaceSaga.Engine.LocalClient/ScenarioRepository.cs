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
    /// <summary>scenarioId of the hidden Tutorial scenario the client starts directly.</summary>
    public const string TutorialScenarioId = "tutorial";

    /// <summary>
    /// List every "scenario.json" found anywhere under scenariosDirectory, ordered by
    /// scenarioId. Returns an empty array if the directory doesn't exist. A scenario file
    /// that fails to parse/validate is skipped rather than crashing the picker — an
    /// operator error in one scenario file must not take down every other one.
    /// Scenarios whose metadata sets hidden=true are excluded.
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
                if (scenario.Metadata.Hidden == true)
                    continue;
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

    /// <summary>
    /// Full path of the valid scenario file whose scenarioId is <see cref="TutorialScenarioId"/>
    /// (hidden or not), or null when the directory or such a file is missing. When several match,
    /// the ordinally first path wins so the result is deterministic.
    /// </summary>
    public static string? FindTutorialScenarioPath(string scenariosDirectory)
    {
        if (!Directory.Exists(scenariosDirectory))
            return null;

        foreach (var path in Directory.EnumerateFiles(scenariosDirectory, "scenario.json", SearchOption.AllDirectories)
                     .Order(StringComparer.Ordinal))
        {
            try
            {
                if (ScenarioLoader.LoadFromFile(path).Metadata.ScenarioId == TutorialScenarioId)
                    return path;
            }
            catch (ScenarioException)
            {
                // Invalid scenario file — not the tutorial.
            }
        }

        return null;
    }
}
