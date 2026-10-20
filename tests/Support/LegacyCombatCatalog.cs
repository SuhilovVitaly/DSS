using System.Collections.Concurrent;
using System.Text.Json.Nodes;

namespace DeepSpaceSaga.TestSupport;

/// <summary>Historical legacy characterization only. Default v3 acceptance uses real Settings directly.</summary>
internal static class LegacyCombatCatalog
{
    private static readonly ConcurrentDictionary<string, Lazy<string>> Catalogs = new(StringComparer.OrdinalIgnoreCase);
    internal static string Settings(string original)
    {
        // A caller may deliberately edit an already explicit historical catalog.
        if (JsonNode.Parse(File.ReadAllText(original))!["typeData"]?["fireModes"] is not null) return original;
        return Catalogs.GetOrAdd(Path.GetFullPath(original), path => new Lazy<string>(() => Create(path))).Value;
    }

    internal static string Root(string original) => Path.GetDirectoryName(Settings(original))!;

    private static string Create(string original)
    {
        string source = Path.GetDirectoryName(original)!;
        string fixture = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "tests/Support/LegacyCombatCatalog.json"));
        var snapshot = JsonNode.Parse(File.ReadAllText(fixture))!["files"]!.AsObject();
        string directory = Directory.CreateTempSubdirectory("dss-legacy-characterization-").FullName;
        var settings = JsonNode.Parse(File.ReadAllText(original))!;
        var paths = settings["typeData"]!.AsObject();
        foreach (var pair in paths.ToArray())
            if (pair.Value is not null) paths[pair.Key] = Path.GetFullPath(Path.Combine(source, pair.Value.GetValue<string>()));
        settings["defaultScenario"] = Path.GetFullPath(Path.Combine(source, settings["defaultScenario"]!.GetValue<string>()));
        foreach (string kind in new[] { "Modules", "Commands" })
            foreach (string file in Directory.GetFiles(Path.Combine(source, "Data", kind), "*.json", SearchOption.AllDirectories))
            {
                string destination = Path.Combine(directory, "Data", kind, Path.GetRelativePath(Path.Combine(source, "Data", kind), file));
                Directory.CreateDirectory(Path.GetDirectoryName(destination)!); File.Copy(file, destination);
            }
        foreach (var pair in snapshot)
        {
            string destination = Path.Combine(directory, pair.Key);
            Directory.CreateDirectory(Path.GetDirectoryName(destination)!); File.WriteAllText(destination, pair.Value!.ToJsonString());
        }
        paths["moduleTypes"] = Path.Combine(directory, "Data/module-types.json");
        paths["moduleImplementations"] = Path.Combine(directory, "Data/Modules");
        paths["commandDefinitions"] = Path.Combine(directory, "Data/Commands");
        paths["combatRules"] = Path.Combine(directory, "Data/Combat/combat-rules.json");
        paths["fireModes"] = Path.Combine(directory, "Data/Combat/fire-modes.json");
        string result = Path.Combine(directory, "Settings.json"); File.WriteAllText(result, settings.ToJsonString());
        AppDomain.CurrentDomain.ProcessExit += (_, _) =>
        {
            // The created directory is an explicit child of the OS temporary directory.
            string prefix = Path.GetFullPath(Path.GetTempPath()).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
            if (Path.GetFullPath(directory).StartsWith(prefix, StringComparison.OrdinalIgnoreCase) && Directory.Exists(directory))
                Directory.Delete(directory, true);
        };
        return result;
    }
}
