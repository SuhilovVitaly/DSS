using System.Text.Json;

namespace DeepSpaceSaga.Client.Tests.TacticalBattle;

public sealed class ContentDataTests
{
    [Fact]
    public void ShippedPilotsHaveApprovedInitiativeValues()
    {
        string path = Path.Combine(FindRepository(), "src", "DeepSpaceSaga.Client", "Data", "TacticalBattle", "pilots.json");
        using var pilots = JsonDocument.Parse(File.ReadAllText(path));
        Assert.Equal(10, pilots.RootElement.EnumerateObject().Count());
        string[] ids = ["vera", "yun", "marta", "oleg"];
        int[] expected = [55, 53, 59, 50];
        for (int i = 0; i < ids.Length; i++)
            Assert.Equal(expected[i], pilots.RootElement.GetProperty(ids[i]).GetProperty("initiative").GetInt32());
        for (int i = 1; i <= 6; i++)
            Assert.Equal(new[] { 40, 55 }, pilots.RootElement.GetProperty($"thug{i}").GetProperty("initiative")
                .EnumerateArray().Select(value => value.GetInt32()));
    }

    [Fact]
    public void InitiativeTextsHaveMatchingLocaleKeysAndPlaceholders()
    {
        string locale = Path.Combine(FindRepository(), "src", "DeepSpaceSaga.Client", "Data", "Locale");
        using var russian = JsonDocument.Parse(File.ReadAllText(Path.Combine(locale, "Russian.json")));
        using var english = JsonDocument.Parse(File.ReadAllText(Path.Combine(locale, "English.json")));
        string[] keys = ["Initiative", "FireQueue", "FireConditional", "Log.FireSkipped", "Skip.NoTarget",
            "Skip.Withheld", "Skip.ShooterDestroyed", "Skip.ShooterDisabled", "Skip.WeaponJammed",
            "Skip.TargetUnavailable", "Skip.BattleEnded"];
        foreach (string suffix in keys)
        {
            string key = "TacticalBattle." + suffix;
            string ru = russian.RootElement.GetProperty(key).GetString()!;
            string en = english.RootElement.GetProperty(key).GetString()!;
            Assert.False(string.IsNullOrWhiteSpace(ru));
            Assert.False(string.IsNullOrWhiteSpace(en));
            Assert.DoesNotContain("TacticalBattle.", ru);
            Assert.DoesNotContain("TacticalBattle.", en);
            string[] expected = suffix == "Initiative" ? ["{0}"] : suffix == "Log.FireSkipped" ? ["{0}", "{1}"] : [];
            Assert.Equal(expected, System.Text.RegularExpressions.Regex.Matches(ru, @"\{\d+\}").Select(match => match.Value));
            Assert.Equal(expected, System.Text.RegularExpressions.Regex.Matches(en, @"\{\d+\}").Select(match => match.Value));
        }
        Assert.Equal("acc {0} · man {1}", english.RootElement.GetProperty("TacticalBattle.SetupSkills").GetString());
    }

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
