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

    internal static string Scenario(string original)
    {
        string full = Path.GetFullPath(original);
        string folder = Path.GetDirectoryName(full)!;
        if (!string.Equals(Path.GetFileName(full), "scenario.json", StringComparison.OrdinalIgnoreCase) ||
            !string.Equals(Path.GetFileName(folder), "PlayerShipOnly", StringComparison.OrdinalIgnoreCase)) return original;
        string client = Path.GetFullPath(Path.Combine(folder, "..", ".."));
        return Path.Combine(Root(Path.Combine(client, "Settings.json")), "Scenarios/PlayerShipOnly/scenario.json");
    }

    private static string Create(string original)
    {
        string source = Path.GetDirectoryName(original)!;
        string fixture = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "tests/Support/LegacyCombatCatalog.json"));
        var document = JsonNode.Parse(File.ReadAllText(fixture))!;
        var snapshot = document["files"]!.AsObject();
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
        foreach (var pair in document["scenarios"]!.AsObject())
        {
            string destination = Path.Combine(directory, "Scenarios", pair.Key, "scenario.json");
            Directory.CreateDirectory(Path.GetDirectoryName(destination)!); File.WriteAllText(destination, pair.Value!.ToJsonString());
        }
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
