using System.Collections.Immutable;
using System.Text.Json;
using System.Text.Json.Serialization;
using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Engine;
using DeepSpaceSaga.Engine.Content;
using DeepSpaceSaga.Engine.Scenario;

namespace DeepSpaceSaga.EconomyBalance;

internal sealed record ClusterMatrixFile(int SchemaVersion, ImmutableArray<ulong> Seeds,
    ImmutableArray<BalanceShipConfiguration> ShipConfigurations, int HorizonDays = 100,
    int SampleIntervalHours = 1, int MaxHorizonDays = 1000, int LocalCycles = 3)
{
    internal static ClusterMatrixFile Default => new(1, [1, 2, 42], [new("starter", 1000, 1000), new("cargo-upgrade", 2000, 1000)]);
    internal void Validate()
    {
        if (SchemaVersion != 1 || Seeds.IsDefaultOrEmpty || Seeds.Distinct().Count() != Seeds.Length ||
            ShipConfigurations.IsDefaultOrEmpty || ShipConfigurations.Any(c => c is null ||
                c.Id is not ("starter" or "cargo-upgrade") || c.CargoCapacityMultiplierPermille != (c.Id == "starter" ? 1000 : 2000) || c.FuelEfficiencyMultiplierPermille != 1000) ||
            ShipConfigurations.Select(c => c.Id).Distinct().Count() != ShipConfigurations.Length || HorizonDays < 100 ||
            MaxHorizonDays < HorizonDays || MaxHorizonDays > 1000 || SampleIntervalHours != 1 || LocalCycles < 3 || LocalCycles > 10)
            throw new BalanceConfigurationException("cluster matrix: schema1, unique seeds, supported ship configurations, hourly sampling, 100..1000 days and 3..10 local cycles required.");
    }
}

internal sealed record ClusterInputShortage(long GameTimeMs, string StationId, string ItemTypeId, long Stock, long RequiredHourlyQuantity);
internal sealed record ClusterCaseEvidence(ulong Seed, string ShipConfigurationId, string ClusterScenario,
    double HorizonReachedDays, int CompletedLocalCycles, int CompletedRoundTrips, string Outcome, string? StopReason,
    long GeometrySampleSimulationTimeMs, long InitialCredits, long FinalCredits, long ActualCargoCapacityKg,
    BalanceCaseEvidence Economy, ImmutableArray<ClusterInputShortage> ProductionInputShortages)
{
    public ClusterEconomyAssessment? Assessment { get; init; }
    public string ScenarioPolicy => "Unit batches; three local cycles; outbound in-transit hold until horizon before Approach; return follows actual docking/trade commands";
}
internal sealed record ClusterBalanceReport(int SchemaVersion, string Status, string Commit, ClusterMatrixFile Matrix,
    ImmutableArray<ClusterCaseEvidence> Cases);

internal sealed class ClusterBalanceRunner
{
    internal ImmutableArray<ClusterCaseEvidence> Run(string settings, string scenarioPath, ClusterMatrixFile matrix)
    {
        matrix.Validate();
        var original = EngineContentLoader.LoadRegistryFromSettingsFile(settings, out _, out _);
        var source = ScenarioLoader.LoadFromFile(scenarioPath);
        var generation = EngineContentLoader.LoadSolarSystemGenerationConfig(settings) ?? throw new BalanceConfigurationException("cluster generation configuration missing");
        var fields = JsonSerializer.Deserialize<StationResourceFieldConfig>(File.ReadAllText(Path.Combine(Path.GetDirectoryName(settings)!, "Data/World/station-resource-fields.json")))!;
        var cases = ImmutableArray.CreateBuilder<ClusterCaseEvidence>();
        foreach (ulong seed in matrix.Seeds.Order()) foreach (var ship in matrix.ShipConfigurations.OrderBy(c => c.Id, StringComparer.Ordinal))
        {
            var registry = ConfigureCargo(original, ship);
            using var bootstrap = new SimulationEngine(registry);
            bootstrap.ConfigureStationResourceFields(fields);
            bootstrap.LoadScenario(source with { GameState = source.GameState with { MasterSeed = seed, CurrentSpeed = "Speed0" } }, generation: generation);
            var save = bootstrap.CaptureSaveStateForTests(0, SimulationSpeed.Speed0, 0);
            var map = save.GameState.ClusterMap ?? throw new BalanceConfigurationException("clusterMap missing");
            var originalStationIds = source.GameState.SpaceObjects.Where(o => o.ObjectType == SpaceObjectType.Station).Select(o => o.ObjectId).ToHashSet(StringComparer.Ordinal);
            string origin = map.Stations.Where(s => s.ClusterId == map.StartClusterId && s.MarketProfileId == "market.mining")
                .OrderBy(s => originalStationIds.Contains(s.ObjectId) ? 0 : 1).ThenBy(s => s.ObjectId, StringComparer.Ordinal).First().ObjectId;
            var initialGeography = bootstrap.CaptureClusterVoyageMapForTools()!;
            string Destination(bool remote) => map.Stations.Where(s => s.MarketProfileId == "market.industrial" && (s.ClusterId != map.StartClusterId) == remote &&
                map.Links.Any(l => l.FromStationId == origin && l.ToStationId == s.ObjectId))
                .OrderBy(s => initialGeography.Edges.Single(e => e.FromStationObjectId == origin && e.ToStationObjectId == s.ObjectId || e.ToStationObjectId == origin && e.FromStationObjectId == s.ObjectId).DistanceKm)
                .ThenBy(s => s.ObjectId, StringComparer.Ordinal).First().ObjectId;
            var samples = ImmutableArray.CreateBuilder<BalanceHourlySample>();
            var shortages = ImmutableArray.CreateBuilder<ClusterInputShortage>();
            void Sample(SimulationEngine engine, ScenarioFile state)
            {
                var sample = EconomyBalanceRunner.Sample(engine, registry, state, engine.CaptureClusterVoyageMapForTools());
                string localDestination = Destination(false), remoteDestination = Destination(true);
                sample = sample with
                {
                    Routes = sample.Routes.Where(r => (r.Origin == origin && (r.Destination == localDestination || r.Destination == remoteDestination)) ||
                    (r.Destination == origin && (r.Origin == localDestination || r.Origin == remoteDestination))).ToImmutableArray(),
                    CargoFlows = state.GameState.GameTimeMs == 0 ? sample.CargoFlows : []
                };
                samples.Add(sample);
                foreach (var station in sample.Stations)
                {
                    var member = map.Stations.Single(s => s.ObjectId == station.StationId);
                    var profile = registry.StationMarketProfiles.GetDefinition(registry.StationMarketProfiles.GetIndex(member.MarketProfileId));
                    foreach (var input in profile.Economy?.HourlyInputs ?? [])
                    {
                        long stock = station.Stocks.Single(s => s.ItemTypeId == input.ItemTypeId).Stock;
                        if (stock < input.Quantity) shortages.Add(new(sample.GameTimeMs, station.StationId, input.ItemTypeId, stock, input.Quantity));
                    }
                }
            }
            Sample(bootstrap, save);
            using var driver = new BalanceDriver(registry, save, true, Sample, matrix.MaxHorizonDays * GameCalendar.DayMs);
            var legs = ImmutableArray.CreateBuilder<BalanceStrategyEvidence>(); int local = 0, remoteTrips = 0; string? stopped = null;
            bool Leg(string a, string b, string item)
            {
                var sample = driver.CurrentSample;
                var route = sample.Routes.Single(r => r.Origin == a && r.Destination == b);
                var leg = driver.Run(sample, route, item, ship); legs.Add(leg);
                if (leg.Outcome != "completed") { stopped = leg.Reason ?? leg.Outcome; return false; }
                return true;
            }
            try
            {
                string destination = Destination(false);
                for (int i = 0; i < matrix.LocalCycles; i++)
                {
                    if (!Leg(origin, destination, "item.iron-ore") || !Leg(destination, origin, "item.energy-cells")) break;
                    local++;
                }
                if (local == matrix.LocalCycles)
                {
                    // The explicit long-voyage scenario waits in transit before its outbound
                    // Approach. It exercises 100-day economy without accruing invented port fees.
                    driver.MinimumDepartureHoldGameTimeMs = matrix.HorizonDays * GameCalendar.DayMs;
                    destination = Destination(true);
                    if (Leg(origin, destination, "item.iron-ore"))
                    {
                        driver.MinimumDepartureHoldGameTimeMs = 0;
                        if (Leg(destination, origin, "item.energy-cells")) remoteTrips++;
                    }
                }
            }
            catch (BalanceConfigurationException e) { stopped = e.Message; }
            var final = driver.CurrentSave;
            using var reloaded = new SimulationEngine(registry); reloaded.LoadScenario(ScenarioLoader.LoadFromJson(ScenarioLoader.Serialize(final), true), true);
            string originalHash = BalanceCanonical.State(final);
            string loadedHash = BalanceCanonical.State(reloaded.CaptureSaveStateForTests(final.GameState.GameTimeMs, SimulationSpeed.Speed0, final.GameState.MotionTimeMs));
            var evidence = new BalanceCaseEvidence(seed, ship.Id, samples.ToImmutable(), legs.ToImmutable(), originalHash, loadedHash);
            long capacity = driver.CurrentSnapshot.InstalledModules.Where(m => m.AvailableCapacityKg is not null).Sum(m => m.AvailableCapacityKg!.Value + m.Cargo.Sum(c => c.Quantity * registry.ItemTypes.GetDefinition(registry.ItemTypes.GetIndex(c.ItemTypeId)).UnitMassKg));
            cases.Add(new(seed, ship.Id, Path.GetFileName(Path.GetDirectoryName(scenarioPath))!, final.GameState.GameTimeMs / (double)GameCalendar.DayMs,
                local, remoteTrips, stopped is null && remoteTrips == 1 ? "completed" : "incomplete", stopped,
                final.GameState.MotionTimeMs, save.GameState.PlayerTokens ?? throw new BalanceConfigurationException("initial credits missing"), driver.CurrentSnapshot.PlayerCredits, capacity, evidence, shortages.ToImmutable()));
        }
        return cases.ToImmutable();
    }

    internal static GameDataRegistry ConfigureCargo(GameDataRegistry r, BalanceShipConfiguration c)
    {
        static IEnumerable<T> All<T>(TypeRegistry<T> types) where T : ITypeDefinition => Enumerable.Range(0, types.Count).Select(types.GetDefinition);
        return GameDataRegistry.Create(All(r.ModuleCategories), All(r.ModuleTypes).Select(m => m.CargoCapacityKg is { } kg
            ? m with { CargoCapacityKg = checked((long)((Int128)kg * c.CargoCapacityMultiplierPermille / 1000)) } : m), All(r.ItemTypes), All(r.CommandDefinitions),
            All(r.FactoryTypes), All(r.Recipes), All(r.Dialogues), All(r.Quests), r.CatalogVersion, r.LegacyCatalogFingerprint,
            All(r.StationMarketProfiles), All(r.ShipClasses), All(r.StationMarketEvents));
    }
}

internal static class ClusterBalanceCli
{
    private static readonly JsonSerializerOptions Json = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase, UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow, WriteIndented = true };
    internal static int Execute(string[] args, TextWriter output, TextWriter error)
    {
        try
        {
            string root = Path.GetFullPath(args[0]), target = Path.GetFullPath(args[1]), matrixPath = Path.GetFullPath(args[3]);
            string client = Path.GetFullPath(Path.Combine(root, "src/DeepSpaceSaga.Client")), settings = Path.Combine(client, "Settings.json");
            if (target.StartsWith(client + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase) || target.Equals(matrixPath, StringComparison.OrdinalIgnoreCase)) throw new BalanceConfigurationException("output may not replace content or matrix input");
            var matrix = JsonSerializer.Deserialize<ClusterMatrixFile>(File.ReadAllText(matrixPath), Json) ?? throw new BalanceConfigurationException("null matrix");
            matrix.Validate();
            _ = EngineContentLoader.LoadRegistryFromSettingsFile(settings, out string basePath, out var loaded);
            var cases = new ClusterBalanceRunner().Run(settings, Path.GetFullPath(Path.Combine(basePath, loaded.DefaultScenario)), matrix);
            using var git = System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo("git") { WorkingDirectory = root, RedirectStandardOutput = true, UseShellExecute = false, CreateNoWindow = true, ArgumentList = { "rev-parse", "HEAD" } })!;
            string commit = git.StandardOutput.ReadToEnd().Trim(); git.WaitForExit();
            cases = cases.Select(c => c with { Assessment = ClusterEconomyEvaluator.Evaluate(c.Economy) }).ToImmutableArray();
            var report = new ClusterBalanceReport(1, cases.All(c => c.Outcome == "completed" && c.Assessment!.Correctness == "passed") ? "correctness-completed-balance-not-assessed" : "incomplete", commit, matrix, cases);
            string temp = target + ".tmp-" + Guid.NewGuid().ToString("N");
            Directory.CreateDirectory(Path.GetDirectoryName(target)!);
            try
            {
                using (var stream = File.Create(temp)) JsonSerializer.Serialize(stream, report, new JsonSerializerOptions(Json) { WriteIndented = false });
                File.Move(temp, target, true);
            }
            finally { if (File.Exists(temp)) File.Delete(temp); }
            output.WriteLine($"status={report.Status};cases={cases.Length};output={target}");
            return report.Status == "correctness-completed-balance-not-assessed" ? 0 : 1;
        }
        catch (Exception e) { error.WriteLine("configuration/runtime failure: " + e.Message); return 2; }
    }
}
