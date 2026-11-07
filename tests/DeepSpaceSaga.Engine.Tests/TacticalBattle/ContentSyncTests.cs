using System.Diagnostics;
using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace DeepSpaceSaga.Engine.Tests.TacticalBattle;

public sealed class ContentSyncTests
{
    private static string Repository
    {
        get
        {
            for (var dir = new DirectoryInfo(AppContext.BaseDirectory); dir is not null; dir = dir.Parent)
                if (File.Exists(Path.Combine(dir.FullName, "DeepSpaceSaga.sln"))) return dir.FullName;
            throw new DirectoryNotFoundException("DSS repository not found.");
        }
    }
    private static string Source => Path.Combine(Repository, "VData/Planning/V4.0/Data");
    private static string Content => Path.Combine(Repository, "src/DeepSpaceSaga.Client/Data/TacticalBattle");

    [Fact]
    public void EveryMarkdownValueAndOrderedCatalogMatchesGameJson()
    {
        var files = new List<string>();
        foreach (string name in new[] { "rules", "ai" })
        {
            using var doc = Open(name + ".json");
            var expectedLeaves = new List<string>();
            foreach (var row in Table("README.md", name))
            {
                string path = row["поле"];
                var node = doc.RootElement;
                foreach (string key in path.Split('.')) node = node.GetProperty(key);
                if (path == "systems.table")
                {
                    var effects = Regex.Matches(row["значение"], @"effect: (\w+), weight: (\d+)");
                    Assert.Equal(effects.Count, node.GetArrayLength());
                    for (int i = 0; i < effects.Count; i++)
                    {
                        Cell(effects[i].Groups[1].Value, node[i].GetProperty("effect"));
                        Cell(effects[i].Groups[2].Value, node[i].GetProperty("weight"));
                    }
                }
                else Cell(row["значение"], node, path == "version");
                expectedLeaves.AddRange(Leaves(node, path));
            }
            Assert.Equal(expectedLeaves, Leaves(doc.RootElement, ""));
        }
        foreach (var (folder, table) in new[] { ("Frames", "frames"), ("Modules/Weapons", "weapons"),
            ("Modules/Engines", "engines"), ("Modules/Shields", "shields"), ("Modules/Armor", "armor") })
            foreach (var row in Table(folder + "/README.md", table))
            {
                using var doc = Open(folder + "/" + row["id"] + ".json");
                Fields(row, doc.RootElement, [], false);
            }
        foreach (string name in new[] { "pilots", "layouts" })
        {
            using var doc = Open(name + ".json");
            var rows = Table("README.md", name);
            Assert.Equal(rows.Select(r => r["id"]), doc.RootElement.EnumerateObject().Select(p => p.Name));
            foreach (var row in rows)
            {
                if (name == "pilots") row.Add("initiative", Initiative(row["id"]));
                Fields(row, doc.RootElement.GetProperty(row["id"]), ["id"], name == "layouts");
            }
        }
        using (var doc = Open("scenarios.json"))
        {
            var goals = Table("README.md", "goals");
            var units = Table("README.md", "scenarios");
            Assert.Equal(goals.Select(r => r["сценарий"]), doc.RootElement.EnumerateObject().Select(p => p.Name));
            foreach (var goal in goals)
            {
                string id = goal["сценарий"];
                var scenario = doc.RootElement.GetProperty(id);
                Cell(goal["name"], scenario.GetProperty("name"));
                Cell(goal["name"], scenario.GetProperty("description"));
                Fields(goal, scenario.GetProperty("goal"), ["сценарий", "name"], false);
                Assert.Equal(new[] { "name", "description", "goal", "sides" }, scenario.EnumerateObject().Select(p => p.Name));
                var groups = units.Where(r => r["сценарий"] == id).GroupBy(r => r["сторона"]).ToArray();
                var sides = scenario.GetProperty("sides");
                Assert.Equal(groups.Length, sides.GetArrayLength());
                for (int i = 0; i < groups.Length; i++)
                {
                    var side = sides[i];
                    Cell(groups[i].Key, side.GetProperty("id"));
                    Cell(groups[i].First()["тактика"], side.GetProperty("tactic"));
                    Assert.All(groups[i], r => Assert.Equal(groups[i].First()["тактика"], r["тактика"]));
                    Assert.Equal(new[] { "id", "tactic", "carrier", "drones" }, side.EnumerateObject().Select(p => p.Name));
                    var carrier = Assert.Single(groups[i], r => r["что"] == "носитель");
                    Fields(carrier, side.GetProperty("carrier"), ["сценарий", "сторона", "тактика", "что"], false);
                    var drones = groups[i].Where(r => r["что"] == "дрон").ToArray();
                    Assert.Equal(drones.Length, side.GetProperty("drones").GetArrayLength());
                    for (int j = 0; j < drones.Length; j++)
                        Fields(drones[j], side.GetProperty("drones")[j], ["сценарий", "сторона", "тактика", "что"], false);
                }
            }
        }
        Assert.Equal(files.Order(StringComparer.Ordinal), Directory.GetFiles(Content, "*.json", SearchOption.AllDirectories)
            .Select(p => Path.GetRelativePath(Content, p).Replace('\\', '/')).Order(StringComparer.Ordinal));

        JsonDocument Open(string file)
        {
            files.Add(file);
            return JsonDocument.Parse(File.ReadAllText(Path.Combine(Content, file)));
        }
    }

    private static string Initiative(string id) => id switch
    {
        "vera" => "55",
        "yun" => "53",
        "marta" => "59",
        "oleg" => "50",
        "thug1" or "thug2" or "thug3" or "thug4" or "thug5" or "thug6" => "[40,55]",
        _ => throw new InvalidOperationException("Unapproved initiative pilot: " + id)
    };

    [Fact]
    public void V4BaseContentAndExplicitInitiativeExtensionAreBothValidated()
    {
        using var pilots = JsonDocument.Parse(File.ReadAllText(Path.Combine(Content, "pilots.json")));
        foreach (var pilot in pilots.RootElement.EnumerateObject())
        {
            Assert.Equal(new[] { "name", "accuracy", "maneuver", "initiative" }, pilot.Value.EnumerateObject().Select(p => p.Name));
            Cell(Initiative(pilot.Name), pilot.Value.GetProperty("initiative"));
        }
        Assert.Equal(10, pilots.RootElement.EnumerateObject().Count());
    }

    [Fact]
    public void ExportCheckRejectsCorruptionWithoutOverwritingFixture()
    {
        string validation = Path.GetFullPath(Environment.GetEnvironmentVariable("DSS_VALIDATION_ROOT")
            ?? (OperatingSystem.IsWindows() ? "D:/DeepSpaceSaga/artifacts/validation/ep-0015-tactical-carrier-battle"
                : Path.Combine(Path.GetTempPath(), "dss-artifacts/validation/ep-0015-tactical-carrier-battle")));
        string root = Path.Combine(validation, "sync-fixture-" + Guid.NewGuid().ToString("N"));
        try
        {
            Assert.Equal(0, Export(root, false));
            Assert.Equal(0, Export(root, true));
            string path = Path.Combine(root, "rules.json");
            const string corrupted = "{\"version\":\"corrupted\"}";
            File.WriteAllText(path, corrupted);
            Assert.NotEqual(0, Export(root, true));
            Assert.Equal(corrupted, File.ReadAllText(path));
            Assert.Equal(0, Export(Content, true));
        }
        finally
        {
            if (!Path.GetFullPath(root).StartsWith(validation + Path.DirectorySeparatorChar, StringComparison.Ordinal))
                throw new InvalidOperationException("Fixture cleanup escaped validation root.");
            if (Directory.Exists(root)) Directory.Delete(root, true);
        }
    }

    private static int Export(string output, bool check)
    {
        var start = new ProcessStartInfo("pwsh") { RedirectStandardOutput = true, RedirectStandardError = true, UseShellExecute = false };
        foreach (string arg in new[] { "-NoProfile", "-File", Path.Combine(Repository, "tools/tactical-battle/Export-Content.ps1"), "-OutputDirectory", output }) start.ArgumentList.Add(arg);
        if (check) start.ArgumentList.Add("-Check");
        using var process = Process.Start(start)!;
        var stdout = process.StandardOutput.ReadToEndAsync();
        var stderr = process.StandardError.ReadToEndAsync();
        if (!process.WaitForExit(60000)) { process.Kill(true); throw new TimeoutException("Content exporter exceeded 60 seconds."); }
        Task.WaitAll(stdout, stderr);
        return process.ExitCode;
    }

    private static void Fields(Dictionary<string, string> row, JsonElement actual, string[] skip, bool layout)
    {
        var expected = new List<string>();
        foreach (var (key, value) in row)
        {
            if (skip.Contains(key, StringComparer.Ordinal)) continue;
            if (value == "—")
            {
                if (layout && key is "weapon" or "shield" or "engines") continue;
                Assert.False(actual.TryGetProperty(key, out _), key);
                continue;
            }
            expected.Add(key);
            if (key == "engines") Assert.Equal(value.Split(',').Select(s => s.Trim()), actual.GetProperty(key).EnumerateArray().Select(e => e.GetString()));
            else Cell(value, actual.GetProperty(key));
        }
        if (layout)
            foreach (string key in new[] { "weapon", "shield", "engines" })
                if (row[key] == "—")
                {
                    expected.Add(key);
                    if (key == "engines") Assert.Empty(actual.GetProperty(key).EnumerateArray());
                    else Assert.Equal(JsonValueKind.Null, actual.GetProperty(key).ValueKind);
                }
        Assert.Equal(expected, actual.EnumerateObject().Select(p => p.Name));
    }

    private static void Cell(string text, JsonElement actual, bool forceString = false)
    {
        if (forceString) { Assert.Equal(text, actual.GetString()); return; }
        if (text.StartsWith('['))
        {
            using var expected = JsonDocument.Parse(text);
            Assert.Equal(expected.RootElement.EnumerateArray().Select(e => e.GetInt32()), actual.EnumerateArray().Select(e => e.GetInt32()));
        }
        else if (text is "true" or "false") Assert.Equal(bool.Parse(text), actual.GetBoolean());
        else if (double.TryParse(text.Replace('−', '-'), NumberStyles.Float, CultureInfo.InvariantCulture, out double number)) Assert.Equal(number, actual.GetDouble());
        else Assert.Equal(text, actual.GetString());
    }

    private static IEnumerable<string> Leaves(JsonElement element, string prefix)
    {
        if (element.ValueKind == JsonValueKind.Object)
            return element.EnumerateObject().SelectMany(p => Leaves(p.Value, prefix.Length == 0 ? p.Name : prefix + "." + p.Name));
        if (element.ValueKind == JsonValueKind.Array)
            return element.EnumerateArray().SelectMany((e, i) => Leaves(e, prefix + "[" + i + "]"));
        return [prefix];
    }

    private static Dictionary<string, string>[] Table(string file, string name)
    {
        string text = File.ReadAllText(Path.Combine(Source, file));
        string start = "<!-- table:" + name + " -->";
        int begin = text.IndexOf(start, StringComparison.Ordinal);
        Assert.True(begin >= 0, start);
        int end = text.IndexOf("<!-- /table -->", begin, StringComparison.Ordinal);
        string[][] rows = text[(begin + start.Length)..end].Split('\n').Where(l => l.StartsWith('|'))
            .Select(l => l.Trim().Trim('|').Split('|').Select(c => c.Trim().Replace("`", "").Replace(" (±)", "")).ToArray()).ToArray();
        return rows.Skip(2).Select(cells =>
        {
            Assert.Equal(rows[0].Length, cells.Length);
            return rows[0].Zip(cells).ToDictionary(p => p.First, p => p.Second, StringComparer.Ordinal);
        }).ToArray();
    }
}
