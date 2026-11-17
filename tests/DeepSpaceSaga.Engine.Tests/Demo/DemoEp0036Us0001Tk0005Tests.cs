using System.Text.Json.Nodes;
using DeepSpaceSaga.Engine.Content;
using DeepSpaceSaga.Engine.Scenario;

namespace DeepSpaceSaga.Engine.Tests.Demo;

public sealed class DemoEp0036Us0001Tk0005Tests
{
    private static DemoCampaignDefinitions Load(params JsonObject[] bounties)
    {
        string root = "D:/DeepSpaceSaga/artifacts/validation/ep-0036/fixtures/" + Guid.NewGuid().ToString("N");
        Directory.CreateDirectory(root + "/Data/Demo");
        try
        {
            string source = DemoEp0032Us0001Tk0002Tests.ContentRoot + "/Data/Demo/";
            foreach (string name in new[] { "cluster", "encounters" })
                File.WriteAllText(root + "/Data/Demo/" + name + ".json", File.ReadAllText(source + name + ".json"));
            var jobs = JsonNode.Parse(File.ReadAllText(source + "jobs.json"))!.AsObject();
            jobs.Remove("bounties");
            if (bounties.Length > 0) jobs["bounties"] = new JsonArray(bounties.Select(b => (JsonNode)b).ToArray());
            File.WriteAllText(root + "/Data/Demo/jobs.json", jobs.ToJsonString());
            return DemoCampaignDefinitions.Load(root);
        }
        finally { Directory.Delete(root, true); }
    }

    private static JsonObject Order(string id = "bounty:pirate:01", string issuer = "SPC-0002",
        string target = "pirate:01", long reward = 1500) => new()
        { ["id"] = id, ["issuerStationId"] = issuer, ["targetGroupId"] = target, ["rewardTokens"] = reward };

    [Fact]
    public void MissingBounties_BackwardCompatible()
    {
        var legacy = Load();
        Assert.Empty(legacy.Jobs!.Bounties);
        Assert.Null(legacy.BountyFingerprint);
        Assert.Equal(legacy.Fingerprint, Load(Order()).Fingerprint);
    }

    [Fact]
    public void DuplicateTargetOrId_Rejected()
    {
        Assert.Throws<ScenarioException>(() => Load(Order(), Order(id: "second")));
        Assert.Throws<ScenarioException>(() => Load(Order(), Order(target: "pirate:02")));
    }

    [Theory]
    [InlineData("unknown", "pirate:01")]
    [InlineData("SPC-0002", "pirate:05")]
    [InlineData("SPC-0002", "PIRATE:01")]
    public void UnknownIssuerOrTarget_Rejected(string issuer, string target) =>
        Assert.Throws<ScenarioException>(() => Load(Order(issuer: issuer, target: target)));

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void InvalidReward_Rejected(long reward) =>
        Assert.Throws<ScenarioException>(() => Load(Order(reward: reward)));

    [Fact]
    public void BountyChange_ChangesFingerprint()
    {
        var first = Load(Order());
        var second = Load(Order(reward: 2000));
        Assert.NotNull(first.BountyFingerprint);
        Assert.NotEqual(first.BountyFingerprint, second.BountyFingerprint);
        Assert.Equal(first.Fingerprint, second.Fingerprint);
    }
}
