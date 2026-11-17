using System.Text.Json;
using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Engine.Content;

namespace DeepSpaceSaga.Client.Tests.Demo;

/// <summary>
/// EP-0034-US-0006-TK-0002: shipped job board content (Data/Demo/jobs.json, schema 1) loads through the
/// production loader, resolves its cargo and risk references and fits the demo carrier. Reads the content copied
/// to the test output.
/// </summary>
public sealed class DemoEp0034Us0006Tk0002Tests
{
    private static string BaseDir => AppContext.BaseDirectory;
    private static string SettingsPath => Path.Combine(BaseDir, "Settings.json");
    private static string DemoScenarioPath => Path.Combine(BaseDir, "Scenarios", "Demo", "scenario.json");

    [Fact]
    public void Content_LoadsAndReferencesResolve()
    {
        var definitions = DemoCampaignDefinitions.Load(BaseDir);
        var jobs = Assert.IsType<DemoJobsDefinition>(definitions.Jobs);
        var cluster = definitions.Cluster!;
        Assert.Equal(1, jobs.SchemaVersion);

        // Every role of the frozen demo cluster has a board; filters reference only cluster roles.
        Assert.Equal(cluster.Roles.Order(StringComparer.Ordinal), jobs.RoleFilters.Select(f => f.Role).Order(StringComparer.Ordinal));
        Assert.All(jobs.RoleFilters, f => Assert.True(f.PassengerWeight + f.UrgentPassengerWeight + f.CargoWeight > 0));

        // The production engine validates cargo references against the item catalog while configuring the demo.
        using var engine = EngineContentLoader.CreateEngineFromScenarioFile(SettingsPath, DemoScenarioPath);
        var registry = EngineContentLoader.LoadRegistryFromSettingsFile(SettingsPath, out _, out _);
        Assert.NotEmpty(jobs.CargoItemTypeIds);
        Assert.All(jobs.CargoItemTypeIds, id => Assert.True(registry.ItemTypes.Contains(id), id));

        // Every route risk profile the demo world can generate has an explainable payout factor.
        using var scenario = JsonDocument.Parse(File.ReadAllText(DemoScenarioPath));
        var profiles = scenario.RootElement.GetProperty("gameState").GetProperty("tradingMapGeneration")
            .GetProperty("riskProfiles").EnumerateArray().Select(p => p.GetProperty("riskProfileId").GetString()!).ToArray();
        Assert.NotEmpty(profiles);
        Assert.All(profiles, id => Assert.Contains(jobs.Risk, r => r.RiskProfileId == id));
        Assert.All(jobs.Risk, r => Assert.True(StationJobRiskBands.IsKnown(r.Band), r.Band));
        Assert.True(jobs.Risk.Single(r => r.Band == StationJobRiskBands.Safe).Permille <
            jobs.Risk.Single(r => r.Band == StationJobRiskBands.Elevated).Permille);

        // Invalid content is refused atomically instead of falling back to defaults.
        string broken = Path.Combine("D:/DeepSpaceSaga/artifacts/validation/EP-0034/tk0002-fixtures", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Path.Combine(broken, "Data", "Demo"));
        try
        {
            File.Copy(Path.Combine(BaseDir, "Data", "Demo", "cluster.json"), Path.Combine(broken, "Data", "Demo", "cluster.json"));
            string text = File.ReadAllText(Path.Combine(BaseDir, "Data", "Demo", "jobs.json"));
            File.WriteAllText(Path.Combine(broken, "Data", "Demo", "jobs.json"), text.Replace("\"role\": \"far_trade\"", "\"role\": \"pirate_base\""));
            Assert.ThrowsAny<Exception>(() => DemoCampaignDefinitions.Load(broken));
        }
        finally { Directory.Delete(broken, true); }
    }

    [Fact]
    public void DemoLimits_AreSatisfied()
    {
        var jobs = DemoCampaignDefinitions.Load(BaseDir).Jobs!;
        // A-decisions of the ticket: at most 6 offers, a board day of publication, offers live two calendar days.
        Assert.Equal(6, jobs.Limits.Offers);
        Assert.Equal(GameCalendar.DayMs, jobs.Limits.PublicationGameTimeMs);
        Assert.Equal(2 * GameCalendar.DayMs, jobs.Limits.ExpiryGameTimeMs);

        // Urgency raises the payout and shortens the deadline; nobody is given less than an hour.
        Assert.True(jobs.Rates.UrgencyPermille > 1000);
        Assert.True(jobs.Deadlines.UrgentPermille < jobs.Deadlines.OrdinaryPermille);
        Assert.True(jobs.Deadlines.MinimumGameTimeMs >= GameCalendar.HourMs);

        // Offers fit the demo carrier: passengers in its free cabins, cargo in its hold.
        using var engine = EngineContentLoader.CreateEngineFromScenarioFile(SettingsPath, DemoScenarioPath);
        engine.SetSpeed(SimulationSpeed.Speed0);
        var snapshot = engine.CaptureSnapshot();
        int free = snapshot.DemoCampaign!.Transport!.PassengerCapacity!.FreeCabins;
        Assert.True(jobs.Limits.PassengerMax <= free, $"passengerMax {jobs.Limits.PassengerMax} > free cabins {free}");
        long hold = snapshot.InstalledModules.Sum(m => m.CargoCapacityKg ?? 0);
        Assert.True(jobs.Limits.CargoTonnesMax * 1000L <= hold, $"cargo {jobs.Limits.CargoTonnesMax} t > hold {hold} kg");

        // The guaranteed first order at the start station is an ordinary passenger job for at most 4 people.
        Assert.Equal(StationJobTypes.Passenger, jobs.FirstOffer.Type);
        Assert.InRange(jobs.FirstOffer.Count, 1, Math.Min(4, free));
    }

    [Fact]
    public void LegacyScenario_DoesNotReceiveDemoGrant()
    {
        foreach (var folder in new[] { "Tutorial", "Default" })
        {
            string path = Path.Combine(BaseDir, "Scenarios", folder, "scenario.json");
            using var engine = EngineContentLoader.CreateEngineFromScenarioFile(SettingsPath, path);
            engine.SetSpeed(SimulationSpeed.Speed0);
            Assert.Null(engine.CaptureSnapshot().DemoCampaign?.Transport?.StationJobs);
        }
    }
}
