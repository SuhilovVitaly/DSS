using DeepSpaceSaga.Engine.Content;

namespace DeepSpaceSaga.Engine.Tests;

/// <summary>
/// Covers ТЗ-05: activationEnergyCellsCost schema/validation prep on command definitions
/// (§56.4). No energy is debited by the Engine yet — this only verifies loading/validation
/// of the new field.
/// </summary>
public class CommandDefinitionLoaderTests
{
    [Fact]
    public void LoadCommandDefinitions_reads_explicit_activation_energy_cells_cost()
    {
        string json = """
        {
          "commandDefinitions": [
            {
              "typeId": "scanner.general-scan",
              "displayName": "General Scan",
              "timeFactor": 0.4,
              "complexityFactor": 1.0,
              "consumptionFactor": 1.0,
              "target": "object",
              "activationEnergyCellsCost": 5
            }
          ]
        }
        """;

        var path = WriteTempFile(json);
        try
        {
            var definitions = EngineContentLoader.LoadCommandDefinitions(path);

            var definition = Assert.Single(definitions);
            Assert.Equal(5, definition.ActivationEnergyCellsCost);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void LoadCommandDefinitions_defaults_missing_activation_energy_cells_cost_to_zero()
    {
        string json = """
        {
          "commandDefinitions": [
            {
              "typeId": "scanner.structural-scan",
              "displayName": "Structural Scan",
              "timeFactor": 3.0,
              "complexityFactor": 1.0,
              "consumptionFactor": 1.0,
              "target": "object"
            }
          ]
        }
        """;

        var path = WriteTempFile(json);
        try
        {
            var definitions = EngineContentLoader.LoadCommandDefinitions(path);

            var definition = Assert.Single(definitions);
            Assert.Equal(0, definition.ActivationEnergyCellsCost);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void LoadCommandDefinitions_rejects_negative_activation_energy_cells_cost()
    {
        string json = """
        {
          "commandDefinitions": [
            {
              "typeId": "scanner.general-scan",
              "displayName": "General Scan",
              "timeFactor": 0.4,
              "complexityFactor": 1.0,
              "consumptionFactor": 1.0,
              "target": "object",
              "activationEnergyCellsCost": -1
            }
          ]
        }
        """;

        var path = WriteTempFile(json);
        try
        {
            Assert.Throws<ContentException>(() => EngineContentLoader.LoadCommandDefinitions(path));
        }
        finally
        {
            File.Delete(path);
        }
    }

    private static string WriteTempFile(string json)
    {
        string path = Path.Combine(Path.GetTempPath(), $"dss-cmddefs-{Guid.NewGuid():N}.json");
        File.WriteAllText(path, json);
        return path;
    }
}
