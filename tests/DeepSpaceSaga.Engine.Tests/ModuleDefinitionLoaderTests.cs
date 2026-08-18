using DeepSpaceSaga.Engine.Content;

namespace DeepSpaceSaga.Engine.Tests;

/// <summary>
/// Covers the split of the module content layer into an abstract module TYPE ("module-types.json",
/// loaded by "LoadModuleCategories") and a concrete module IMPLEMENTATION layer (one or more
/// "moduleImplementations" JSON files, loaded by "LoadModuleImplementations", each entry carrying
/// a "type" field that resolves its owning module type). "LoadModuleImplementations" merges the
/// resolved category's SlotSize/CommandTypeIds into the flat, unchanged "ModuleTypeDefinition"
/// runtime record — story-20260818 module-type/module-implementation split, Batch A.
/// </summary>
public class ModuleDefinitionLoaderTests
{
    [Fact]
    public void LoadModuleCategories_reads_type_id_display_name_slot_size_and_command_type_ids()
    {
        string json = """
        {
          "moduleTypes": [
            {
              "typeId": "module.engine",
              "displayName": "Engine",
              "slotSize": 1,
              "commandTypeIds": [ "engine.accelerate", "engine.brake" ]
            }
          ]
        }
        """;

        var path = WriteTempFile(json);
        try
        {
            var categories = EngineContentLoader.LoadModuleCategories(path);

            var category = Assert.Single(categories);
            Assert.Equal("module.engine", category.TypeId);
            Assert.Equal("Engine", category.DisplayName);
            Assert.Equal(1, category.SlotSize);
            Assert.Equal(["engine.accelerate", "engine.brake"], category.CommandTypeIds.ToArray());
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void LoadModuleCategories_rejects_type_missing_command_type_ids()
    {
        string json = """
        {
          "moduleTypes": [
            { "typeId": "module.engine", "displayName": "Engine", "slotSize": 1 }
          ]
        }
        """;

        var path = WriteTempFile(json);
        try
        {
            var ex = Assert.Throws<ContentException>(() => EngineContentLoader.LoadModuleCategories(path));
            Assert.Contains("is missing commandTypeIds", ex.Message, StringComparison.Ordinal);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void LoadModuleImplementations_resolves_slot_size_and_command_type_ids_from_owning_category()
    {
        var categories = new[]
        {
            new ModuleCategoryDefinition(
                "module.scanner", "Scanner", SlotSize: 2,
                CommandTypeIds: System.Collections.Immutable.ImmutableArray.Create("scanner.generalScan"))
        };

        string json = """
        {
          "moduleImplementations": [
            {
              "typeId": "module.scanner.mk1",
              "displayName": "Scanner MK I",
              "type": "module.scanner",
              "massKg": 1000,
              "structurePointsMax": 50,
              "powerConsumptionW": 100,
              "baseCycleTimeMs": 1000
            }
          ]
        }
        """;

        var path = WriteTempFile(json);
        try
        {
            var implementations = EngineContentLoader.LoadModuleImplementations(path, categories);

            var implementation = Assert.Single(implementations);
            Assert.Equal("module.scanner.mk1", implementation.TypeId);
            Assert.Equal(2, implementation.SlotSize);
            Assert.Equal(["scanner.generalScan"], implementation.CommandTypeIds.ToArray());
            Assert.Equal(1000, implementation.MassKg);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void LoadModuleImplementations_rejects_implementation_referencing_unknown_module_type()
    {
        var categories = new[]
        {
            new ModuleCategoryDefinition(
                "module.scanner", "Scanner", SlotSize: 2,
                CommandTypeIds: System.Collections.Immutable.ImmutableArray.Create("scanner.generalScan"))
        };

        string json = """
        {
          "moduleImplementations": [
            {
              "typeId": "module.scanner.mk1",
              "displayName": "Scanner MK I",
              "type": "module.doesNotExist",
              "massKg": 1000,
              "structurePointsMax": 50,
              "powerConsumptionW": 100,
              "baseCycleTimeMs": 1000
            }
          ]
        }
        """;

        var path = WriteTempFile(json);
        try
        {
            var ex = Assert.Throws<ContentException>(
                () => EngineContentLoader.LoadModuleImplementations(path, categories));

            Assert.Contains(
                "Module implementation 'module.scanner.mk1' references unknown module type 'module.doesNotExist'.",
                ex.Message, StringComparison.Ordinal);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void LoadModuleImplementations_rejects_implementation_missing_type()
    {
        var categories = Array.Empty<ModuleCategoryDefinition>();

        string json = """
        {
          "moduleImplementations": [
            { "typeId": "module.scanner.mk1", "displayName": "Scanner MK I", "massKg": 1000,
              "structurePointsMax": 50, "powerConsumptionW": 100, "baseCycleTimeMs": 1000 }
          ]
        }
        """;

        var path = WriteTempFile(json);
        try
        {
            var ex = Assert.Throws<ContentException>(
                () => EngineContentLoader.LoadModuleImplementations(path, categories));
            Assert.Contains("missing required 'type'", ex.Message, StringComparison.Ordinal);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void LoadModuleImplementations_merges_all_json_files_in_directory()
    {
        var categories = new[]
        {
            new ModuleCategoryDefinition(
                "module.reactor", "Reactor", SlotSize: 1,
                CommandTypeIds: System.Collections.Immutable.ImmutableArray.Create("reactor.activate")),
            new ModuleCategoryDefinition(
                "module.scanner", "Scanner", SlotSize: 2,
                CommandTypeIds: System.Collections.Immutable.ImmutableArray<string>.Empty)
        };

        string directory = CreateTempDirectory();
        try
        {
            WriteFile(directory, "a-reactor.json", """
            {
              "moduleImplementations": [
                { "typeId": "module.reactor.basic", "displayName": "Reactor", "type": "module.reactor",
                  "massKg": 5000, "structurePointsMax": 100, "powerConsumptionW": 0, "baseCycleTimeMs": 1000 }
              ]
            }
            """);
            WriteFile(directory, "b-scanner.json", """
            {
              "moduleImplementations": [
                { "typeId": "module.scanner.mk1", "displayName": "Scanner MK I", "type": "module.scanner",
                  "massKg": 1000, "structurePointsMax": 50, "powerConsumptionW": 100 }
              ]
            }
            """);

            var implementations = EngineContentLoader.LoadModuleImplementations(directory, categories);

            Assert.Equal(2, implementations.Count);
            Assert.Contains(implementations, m => m.TypeId == "module.reactor.basic");
            Assert.Contains(implementations, m => m.TypeId == "module.scanner.mk1");
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public void LoadModuleImplementations_rejects_empty_directory_with_no_json_files()
    {
        var categories = Array.Empty<ModuleCategoryDefinition>();
        string directory = CreateTempDirectory();
        try
        {
            var ex = Assert.Throws<ContentException>(
                () => EngineContentLoader.LoadModuleImplementations(directory, categories));
            Assert.Contains(directory, ex.Message, StringComparison.Ordinal);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public void LoadModuleImplementations_rejects_path_that_is_neither_file_nor_directory()
    {
        var categories = Array.Empty<ModuleCategoryDefinition>();
        string path = Path.Combine(Path.GetTempPath(), $"dss-moduleimpl-missing-{Guid.NewGuid():N}");

        var ex = Assert.Throws<ContentException>(
            () => EngineContentLoader.LoadModuleImplementations(path, categories));
        Assert.Contains(path, ex.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void LoadModuleImplementations_requires_positive_base_cycle_time_for_active_category()
    {
        var categories = new[]
        {
            new ModuleCategoryDefinition(
                "module.scanner", "Scanner", SlotSize: 1,
                CommandTypeIds: System.Collections.Immutable.ImmutableArray.Create("scanner.generalScan"))
        };

        string json = """
        {
          "moduleImplementations": [
            { "typeId": "module.scanner.mk1", "displayName": "Scanner MK I", "type": "module.scanner",
              "massKg": 1000, "structurePointsMax": 50, "powerConsumptionW": 100 }
          ]
        }
        """;

        var path = WriteTempFile(json);
        try
        {
            var ex = Assert.Throws<ContentException>(
                () => EngineContentLoader.LoadModuleImplementations(path, categories));
            Assert.Contains("must specify a positive baseCycleTimeMs", ex.Message, StringComparison.Ordinal);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void LoadModuleImplementations_rejects_module_engine_implementation_missing_max_speed_mps()
    {
        var categories = new[]
        {
            new ModuleCategoryDefinition(
                "module.engine", "Engine", SlotSize: 1,
                CommandTypeIds: System.Collections.Immutable.ImmutableArray.Create("engine.accelerate"))
        };

        string json = """
        {
          "moduleImplementations": [
            { "typeId": "module.engine.basic", "displayName": "Engine", "type": "module.engine",
              "massKg": 5000, "structurePointsMax": 100, "powerConsumptionW": 0, "baseCycleTimeMs": 1000,
              "turnStepDegrees": 1, "linearInertiaMps2": 400, "angularInertiaDegPerSec": 4, "fuelCapacityKg": 2000 }
          ]
        }
        """;

        var path = WriteTempFile(json);
        try
        {
            var ex = Assert.Throws<ContentException>(
                () => EngineContentLoader.LoadModuleImplementations(path, categories));
            Assert.Contains("requires maxSpeedMps greater than zero", ex.Message, StringComparison.Ordinal);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void LoadModuleImplementations_accepts_module_engine_implementation_with_all_engine_parameters()
    {
        var categories = new[]
        {
            new ModuleCategoryDefinition(
                "module.engine", "Engine", SlotSize: 1,
                CommandTypeIds: System.Collections.Immutable.ImmutableArray.Create("engine.accelerate"))
        };

        string json = """
        {
          "moduleImplementations": [
            { "typeId": "module.engine.basic", "displayName": "Engine", "type": "module.engine",
              "massKg": 5000, "structurePointsMax": 100, "powerConsumptionW": 0, "baseCycleTimeMs": 1000,
              "maxSpeedMps": 4000, "turnStepDegrees": 1, "linearInertiaMps2": 400,
              "angularInertiaDegPerSec": 4, "fuelCapacityKg": 2000 }
          ]
        }
        """;

        var path = WriteTempFile(json);
        try
        {
            var implementations = EngineContentLoader.LoadModuleImplementations(path, categories);

            var implementation = Assert.Single(implementations);
            Assert.Equal(4000, implementation.MaxSpeedMps);
        }
        finally
        {
            File.Delete(path);
        }
    }

    private static string CreateTempDirectory()
    {
        string directory = Path.Combine(Path.GetTempPath(), $"dss-moduleimpl-dir-{Guid.NewGuid():N}");
        Directory.CreateDirectory(directory);
        return directory;
    }

    private static void WriteFile(string directory, string fileName, string json) =>
        File.WriteAllText(Path.Combine(directory, fileName), json);

    private static string WriteTempFile(string json)
    {
        string path = Path.Combine(Path.GetTempPath(), $"dss-moduleimpl-{Guid.NewGuid():N}.json");
        File.WriteAllText(path, json);
        return path;
    }
}
