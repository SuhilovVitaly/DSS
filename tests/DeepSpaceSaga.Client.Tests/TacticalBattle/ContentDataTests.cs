using System.Text.Json;

namespace DeepSpaceSaga.Client.Tests.TacticalBattle;

public sealed class ContentDataTests
{
    [Fact]
    public void SettingsDeclaresBattleContentAndClientBuildShipsEveryJson()
    {
        string repo = FindRepository();
        string client = Path.Combine(repo, "src", "DeepSpaceSaga.Client");
        using var settings = JsonDocument.Parse(File.ReadAllText(Path.Combine(client, "Settings.json")));
        string relative = settings.RootElement.GetProperty("typeData").GetProperty("tacticalBattle").GetString()!;
        Assert.Equal("Data/TacticalBattle", relative);
        string source = Path.Combine(client, relative);
        string configuration = new DirectoryInfo(AppContext.BaseDirectory).Parent!.Name;
        string output = Path.Combine(client, "bin", configuration, "net8.0", relative);
        var files = Directory.GetFiles(source, "*.json", SearchOption.AllDirectories);
        Assert.Equal(29, files.Length);
        foreach (string file in files)
        {
            string deployed = Path.Combine(output, Path.GetRelativePath(source, file));
            Assert.True(File.Exists(deployed), $"Client build did not copy {deployed}");
            Assert.Equal(File.ReadAllText(file), File.ReadAllText(deployed));
        }
    }

    [Fact]
    public void InitialScenarioPreservesTableOrderAndEquipmentReferences()
    {
        string source = Path.Combine(FindRepository(), "src", "DeepSpaceSaga.Client", "Data", "TacticalBattle");
        using var scenarios = Read("scenarios.json");
        using var layouts = Read("layouts.json");
        using var ai = Read("ai.json");
        using var rules = Read("rules.json");
        var scenario = scenarios.RootElement.EnumerateObject().First();
        Assert.Equal("initial", scenario.Name);
        Assert.Equal("4.0", rules.RootElement.GetProperty("version").GetString());
        Assert.Equal(new[] { "raid", "defence", "dogfight", "balanced", "commander" },
            ai.RootElement.GetProperty("tactics").EnumerateObject().Select(p => p.Name));
        var sides = scenario.Value.GetProperty("sides");
        Assert.Equal(new[] { "A", "B" }, sides.EnumerateArray().Select(s => s.GetProperty("id").GetString()));
        Assert.Equal(new[] { "Вера", "Юн", "Марта", "Олег" },
            sides[0].GetProperty("drones").EnumerateArray().Select(d => d.GetProperty("id").GetString()));
        Assert.Equal(6, sides[1].GetProperty("drones").GetArrayLength());
        Assert.Equal(-180, sides[1].GetProperty("carrier").GetProperty("course").GetInt32());
        foreach (var layout in layouts.RootElement.EnumerateObject())
        {
            AssertCatalog("Frames", layout.Value.GetProperty("class").GetString()!);
            AssertCatalog("Modules/Armor", layout.Value.GetProperty("armor").GetString()!);
            if (layout.Value.GetProperty("weapon").GetString() is { } weapon) AssertCatalog("Modules/Weapons", weapon);
            if (layout.Value.GetProperty("shield").GetString() is { } shield) AssertCatalog("Modules/Shields", shield);
            foreach (var engine in layout.Value.GetProperty("engines").EnumerateArray()) AssertCatalog("Modules/Engines", engine.GetString()!);
        }

        JsonDocument Read(string file) => JsonDocument.Parse(File.ReadAllText(Path.Combine(source, file)));
        void AssertCatalog(string folder, string id)
        {
            using var module = Read($"{folder}/{id}.json");
            Assert.Equal(id, module.RootElement.GetProperty("id").GetString());
        }
    }

    private static string FindRepository()
    {
        for (var dir = new DirectoryInfo(AppContext.BaseDirectory); dir is not null; dir = dir.Parent)
            if (File.Exists(Path.Combine(dir.FullName, "DeepSpaceSaga.sln"))) return dir.FullName;
        throw new DirectoryNotFoundException("DSS repository not found.");
    }
}
