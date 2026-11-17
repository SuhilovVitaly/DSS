using DeepSpaceSaga.Client.UI.Demo;
using DeepSpaceSaga.Client.UI.Screens.MainMenu;
using DeepSpaceSaga.Engine.LocalClient;

namespace DeepSpaceSaga.Client.Tests.Demo;

public sealed class DemoEp0032Us0004Tk0004Tests
{
    [Fact]
    public void MissingOrUnknownProfileFallsBackWithoutCrashing()
    {
        var directory = "D:/DeepSpaceSaga/artifacts/validation/ep-0032-review/fixtures/" + Guid.NewGuid().ToString("N");
        Directory.CreateDirectory(directory);
        try
        {
            string path = directory + "/Settings.json";
            Assert.False(DemoStartPresentation.IsDemoProfile(path));
            File.WriteAllText(path, """{"gameSettings":{"campaignProfile":"future-profile"}}""");
            Assert.False(DemoStartPresentation.IsDemoProfile(path));
            File.WriteAllText(path, "{broken");
            Assert.False(DemoStartPresentation.IsDemoProfile(path));
        }
        finally { Directory.Delete(directory, true); }
    }

    [Fact]
    public void PublicProfileUsesDemoForTutorialAndHidesDevelopmentScenarios()
    {
        string root = DemoCheckoutPaths.ClientRoot;
        Assert.True(DemoStartPresentation.IsDemoProfile(root + "/Settings.json"));
        var demo = Assert.Single(ScenarioRepository.ListScenarios(root + "/Scenarios", demoProfile: true));
        Assert.Equal("demo", demo.ScenarioId);
        Assert.Equal(demo.ScenarioPath, ScenarioRepository.FindTutorialScenarioPath(root + "/Scenarios", demoProfile: true));
        Assert.True(ScenarioRepository.ListScenarios(root + "/Scenarios").Length > 1);
        Assert.Contains("Tutorial", ScenarioRepository.FindTutorialScenarioPath(root + "/Scenarios")!);
        using var locale = Localization.UseLocaleForTests("English");
        Assert.Contains("demo", new MainMenuScreen(demoProfile: true).NewGameLabel);
    }
}
