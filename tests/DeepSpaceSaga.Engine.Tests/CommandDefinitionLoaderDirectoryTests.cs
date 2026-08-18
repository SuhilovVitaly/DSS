using DeepSpaceSaga.Engine.Content;

namespace DeepSpaceSaga.Engine.Tests;

/// <summary>
/// Covers the dual-mode "LoadCommandDefinitions" directory layout (Data/Commands/&lt;module
/// type&gt;/commands.json) introduced alongside the "type" (owning module type id) field on
/// "CommandDefinition" — story-20260818-085340, Batch 1, U2/U5.
/// </summary>
public class CommandDefinitionLoaderDirectoryTests
{
    [Fact]
    public void LoadCommandDefinitions_merges_all_json_files_in_directory()
    {
        string directory = CreateTempDirectory();
        try
        {
            WriteFile(directory, "a-engine.json", """
            {
              "commandDefinitions": [
                { "typeId": "engine.accelerate", "displayName": "Accelerate", "type": "module.engine.basic" },
                { "typeId": "engine.brake", "displayName": "Brake", "type": "module.engine.basic" }
              ]
            }
            """);
            WriteFile(directory, "b-scanner.json", """
            {
              "commandDefinitions": [
                { "typeId": "scanner.general-scan", "displayName": "General Scan", "type": "module.scanner.mk1" }
              ]
            }
            """);

            var definitions = EngineContentLoader.LoadCommandDefinitions(directory);

            Assert.Equal(3, definitions.Count);
            Assert.Contains(definitions, d => d.TypeId == "engine.accelerate");
            Assert.Contains(definitions, d => d.TypeId == "engine.brake");
            Assert.Contains(definitions, d => d.TypeId == "scanner.general-scan");
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public void LoadCommandDefinitions_merges_directory_in_deterministic_ordinal_file_order()
    {
        string directory = CreateTempDirectory();
        try
        {
            // File names deliberately chosen so ordinal sort order differs from creation order.
            WriteFile(directory, "z-last.json", """
            { "commandDefinitions": [ { "typeId": "engine.accelerate", "displayName": "A", "type": "module.engine.basic" } ] }
            """);
            WriteFile(directory, "a-first.json", """
            { "commandDefinitions": [ { "typeId": "engine.brake", "displayName": "B", "type": "module.engine.basic" } ] }
            """);

            var definitions = EngineContentLoader.LoadCommandDefinitions(directory);

            // "a-first.json" sorts before "z-last.json" (StringComparer.Ordinal) regardless of
            // on-disk enumeration/creation order, so its command must appear first in the merge.
            Assert.Equal(2, definitions.Count);
            Assert.Equal("engine.brake", definitions[0].TypeId);
            Assert.Equal("engine.accelerate", definitions[1].TypeId);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public void LoadCommandDefinitions_merges_all_files_into_a_single_set_so_duplicate_typeId_across_files_is_caught()
    {
        // LoadCommandDefinitions itself just merges DTOs from every file into one sequence;
        // duplicate-typeId detection happens once that merged sequence reaches
        // TypeRegistry<CommandDefinition>.Create (via GameDataRegistry.Create) — proving the
        // merge is a single IEnumerable, not per-file registries that would miss cross-file dupes.
        string directory = CreateTempDirectory();
        try
        {
            WriteFile(directory, "a.json", """
            { "commandDefinitions": [ { "typeId": "engine.accelerate", "displayName": "A", "type": "module.engine.basic" } ] }
            """);
            WriteFile(directory, "b.json", """
            { "commandDefinitions": [ { "typeId": "engine.accelerate", "displayName": "A duplicate", "type": "module.engine.basic" } ] }
            """);

            var definitions = EngineContentLoader.LoadCommandDefinitions(directory);
            Assert.Equal(2, definitions.Count); // no dedup/merge-conflict detection at this layer

            var ex = Assert.Throws<ContentException>(
                () => TypeRegistry<CommandDefinition>.Create(definitions, "command definitions"));
            Assert.Contains("duplicate typeId 'engine.accelerate'", ex.Message, StringComparison.Ordinal);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public void LoadCommandDefinitions_rejects_command_missing_type_in_single_file_mode()
    {
        string json = """
        { "commandDefinitions": [ { "typeId": "engine.accelerate", "displayName": "A" } ] }
        """;
        string path = WriteTempFile(json);
        try
        {
            var ex = Assert.Throws<ContentException>(() => EngineContentLoader.LoadCommandDefinitions(path));
            Assert.Contains("missing required 'type'", ex.Message, StringComparison.Ordinal);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void LoadCommandDefinitions_rejects_command_with_empty_type_in_directory_mode()
    {
        string directory = CreateTempDirectory();
        try
        {
            WriteFile(directory, "a.json", """
            { "commandDefinitions": [ { "typeId": "engine.accelerate", "displayName": "A", "type": "" } ] }
            """);

            var ex = Assert.Throws<ContentException>(() => EngineContentLoader.LoadCommandDefinitions(directory));
            Assert.Contains("missing required 'type'", ex.Message, StringComparison.Ordinal);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public void LoadCommandDefinitions_rejects_empty_directory_with_no_json_files()
    {
        string directory = CreateTempDirectory();
        try
        {
            var ex = Assert.Throws<ContentException>(() => EngineContentLoader.LoadCommandDefinitions(directory));
            Assert.Contains(directory, ex.Message, StringComparison.Ordinal);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public void LoadCommandDefinitions_rejects_path_that_is_neither_file_nor_directory()
    {
        string path = Path.Combine(Path.GetTempPath(), $"dss-cmddefs-missing-{Guid.NewGuid():N}");

        var ex = Assert.Throws<ContentException>(() => EngineContentLoader.LoadCommandDefinitions(path));
        Assert.Contains(path, ex.Message, StringComparison.Ordinal);
    }

    private static string CreateTempDirectory()
    {
        string directory = Path.Combine(Path.GetTempPath(), $"dss-cmddefs-dir-{Guid.NewGuid():N}");
        Directory.CreateDirectory(directory);
        return directory;
    }

    private static void WriteFile(string directory, string fileName, string json) =>
        File.WriteAllText(Path.Combine(directory, fileName), json);

    private static string WriteTempFile(string json)
    {
        string path = Path.Combine(Path.GetTempPath(), $"dss-cmddefs-{Guid.NewGuid():N}.json");
        File.WriteAllText(path, json);
        return path;
    }
}
