using System.Text.Json.Nodes;
using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Engine.Content;
using DeepSpaceSaga.Engine.LocalClient;
using DeepSpaceSaga.Engine.Scenario;

namespace DeepSpaceSaga.Engine.Tests.Demo;

public sealed class DemoEp0032ContentReviewTests
{
    private const string Root = DemoEp0032Us0001Tk0002Tests.ContentRoot;
    private static string Temp()
    {
        var path = "D:/DeepSpaceSaga/artifacts/validation/ep-0032-review/fixtures/" + Guid.NewGuid().ToString("N");
        Directory.CreateDirectory(path);
        return path;
    }

    [Fact]
    public void OptionalContentAndWhitespaceDoNotInvalidateClusterIdentity()
    {
        string directory = Temp();
        try
        {
            Directory.CreateDirectory(directory + "/Data/Demo");
            string cluster = File.ReadAllText(Root + "/Data/Demo/cluster.json");
            File.WriteAllText(directory + "/Data/Demo/cluster.json", cluster);
            var before = DemoCampaignDefinitions.Load(directory);
            File.WriteAllText(directory + "/Data/Demo/cluster.json", JsonNode.Parse(cluster)!.ToJsonString());
            File.WriteAllText(directory + "/Data/Demo/jobs.json", """{"schemaVersion":1,"limits":{"offers":1,"publicationGameTimeMs":1000,"expiryGameTimeMs":2000,"passengerMax":1},"rates":{"base":1,"perPassenger":1,"perCargoUnit":1,"perCalendarDay":1,"urgencyMultiplier":1},"roleFilters":[]}""");
            var after = DemoCampaignDefinitions.Load(directory);
            Assert.Equal(before.Fingerprint, after.Fingerprint);
            using var engine = EngineContentLoader.CreateEngineFromSettingsFile(Root + "/Settings.json");
            var save = engine.CaptureSaveState();
            after.ValidateWorld(save);
            after.ValidateWorld(save with
            {
                GameState = save.GameState with
                { DemoCampaign = save.GameState.DemoCampaign! with { CatalogFingerprint = before.LegacyClusterFingerprint } }
            });
        }
        finally { Directory.Delete(directory, true); }
    }

    [Fact]
    public void BrokenDemoContentDoesNotDisableDevelopmentScenario()
    {
        string directory = Temp();
        try
        {
            var settings = JsonNode.Parse(File.ReadAllText(Root + "/Settings.json"))!;
            foreach (var item in settings["typeData"]!.AsObject().ToArray())
                settings["typeData"]![item.Key] = Path.Combine(Root, item.Value!.GetValue<string>());
            settings["defaultScenario"] = Root + "/Scenarios/Default/scenario.json";
            File.WriteAllText(directory + "/Settings.json", settings.ToJsonString());
            Directory.CreateDirectory(directory + "/Data/Demo");
            File.WriteAllText(directory + "/Data/Demo/cluster.json", "{bad");
            Directory.CreateDirectory(directory + "/Data/Ships");
            File.Copy(Root + "/Data/Ships/starter-carrier.json", directory + "/Data/Ships/starter-carrier.json");
            using var engine = EngineContentLoader.CreateEngineFromScenarioFile(directory + "/Settings.json", Root + "/Scenarios/Default/scenario.json");
            Assert.NotEmpty(engine.CaptureSnapshot().Objects);
        }
        finally { Directory.Delete(directory, true); }
    }

}
