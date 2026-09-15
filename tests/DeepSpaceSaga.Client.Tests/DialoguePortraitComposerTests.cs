using System.Buffers.Binary;
using System.Security.Cryptography;
using System.Text;
using DeepSpaceSaga.Client.Portraits;
using DeepSpaceSaga.Client.UI.Portraits;
using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Engine;
using DeepSpaceSaga.Engine.Scenario;
using SkiaSharp;

namespace DeepSpaceSaga.Client.Tests;

public sealed class DialoguePortraitComposerTests
{
    [Theory]
    [InlineData(CharacterPortraits.DefaultFemale, PersonSex.Female, "station/operator")]
    [InlineData(CharacterPortraits.DefaultMale, PersonSex.Male, "captain")]
    public void Dialogue_matches_the_workshop_composition_at_300_pixels(string path, PersonSex sex, string key)
    {
        using var actual = DialoguePortraitComposer.Compose(path, sex, key);
        Assert.NotNull(actual);
        Assert.Equal(300, actual.Width);
        Assert.Equal(300, actual.Height);
        string pack = sex == PersonSex.Male ? "M4" : "W4";
        var assets = new PortraitAssetRepository(Path.Combine(AppContext.BaseDirectory, "Images", "Persons", pack));
        var head = assets.Parts.Single(p => p.Category == "Portrait" &&
            assets.TexturePath(p.Texture) == Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, path)));
        int seed = BinaryPrimitives.ReadInt32LittleEndian(SHA256.HashData(Encoding.UTF8.GetBytes(key)));
        var appearance = new PortraitGenerator(assets).Generate(seed);
        appearance = appearance with { Parts = appearance.Parts.SetItem("Portrait", head.Id) };
        using var renderer = new PortraitRenderer(assets);
        using var expected = SKBitmap.FromImage(renderer.Render(appearance, 300));
        Assert.Equal(expected.Bytes, actual.Bytes);
        Assert.Equal(255, actual.GetPixel(150, 175).Alpha);
        Assert.Equal(255, actual.GetPixel(150, 240).Alpha);
    }

    [Theory]
    [InlineData("Images/Persons/M/old.png", PersonSex.Male)]
    [InlineData(@"Images\Persons\M4\Portraits\head.png", PersonSex.Male)]
    [InlineData("Images/Persons/W4/Portraits/head.png", PersonSex.Female)]
    public void Identifies_both_current_and_legacy_packs(string path, PersonSex expected) =>
        Assert.Equal(expected, DialoguePortraitComposer.SexFor(path));

    [Fact]
    public void Old_save_male_and_missing_captain_portrait_use_the_mature_man()
    {
        using var legacy = DialoguePortraitComposer.Compose("Images/Persons/M/old.png", PersonSex.Male, "ship");
        using var missing = DialoguePortraitComposer.Compose(null, PersonSex.Male, "ship");
        using var current = DialoguePortraitComposer.Compose(CharacterPortraits.DefaultMale, PersonSex.Male, "ship");
        Assert.NotNull(legacy); Assert.NotNull(missing); Assert.NotNull(current);
        Assert.Equal(current.Bytes, legacy.Bytes);
        Assert.Equal(current.Bytes, missing.Bytes);
        using var woman = DialoguePortraitComposer.Compose("Images/Persons/W/old.png", PersonSex.Female, "operator");
        Assert.NotNull(woman);
        Assert.Equal(255, woman.GetPixel(150, 175).Alpha);
    }

    [Fact]
    public void Missing_explicit_new_file_is_handled_without_throwing()
    {
        using var missing = DialoguePortraitComposer.Compose(CharacterPortraits.FemaleFolder + "/missing.png", PersonSex.Female, "operator");
        Assert.Null(missing);
    }

    [Theory]
    [InlineData("Default")]
    [InlineData("Default_500")]
    [InlineData("Docked")]
    [InlineData("Undocked")]
    public void Shipped_scenarios_resolve_new_portraits_and_preserve_them_on_reload(string name)
    {
        string root = AppContext.BaseDirectory;
        string scenarioPath = Path.Combine(root, "Scenarios", name, "scenario.json");
        var scenario = ScenarioLoader.LoadFromFile(scenarioPath);
        Assert.All(scenario.GameState.SpaceObjects.SelectMany(o => o.StationCrew ?? []), c => Assert.Null(c.PortraitImage));
        using var engine = SimulationEngine.CreateFromScenarioFile(Path.Combine(root, "Settings.json"), scenarioPath);
        var saved = engine.CaptureSaveState();
        var player = saved.GameState.SpaceObjects.Single(o => o.ObjectId == saved.GameState.PlayerShipObjectId);
        Assert.Equal(CharacterPortraits.DefaultMale, player.CaptainPortraitImage);
        var crew = saved.GameState.SpaceObjects.SelectMany(o => o.StationCrew ?? []).ToArray();
        Assert.NotEmpty(crew);
        foreach (var member in crew)
        {
            Assert.StartsWith(CharacterPortraits.FemaleFolder + "/", member.PortraitImage);
            Assert.True(File.Exists(Path.Combine(root, member.PortraitImage!)));
            using var image = DialoguePortraitComposer.Compose(member.PortraitImage, PersonSex.Female, member.CrewId);
            Assert.NotNull(image);
        }
        engine.LoadScenario(saved);
        Assert.Equal(crew.Select(c => c.PortraitImage), engine.CaptureSaveState().GameState.SpaceObjects.SelectMany(o => o.StationCrew ?? []).Select(c => c.PortraitImage));
    }
}

