using System.Collections.Immutable;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using DeepSpaceSaga.Engine.Content;

namespace DeepSpaceSaga.EconomyBalance;

internal sealed record BalanceMatrixFile(int SchemaVersion, ImmutableArray<ulong> Seeds,
    ImmutableArray<BalanceShipConfiguration> ShipConfigurations, long HorizonHours, long SampleIntervalHours,
    long SaveLoadCheckpointHours, int ZeroStockMaximumPermille, int ShortMarginMinimumPermille, int ShortMarginMaximumPermille,
    int MediumMarginMinimumPermille, int MediumMarginMaximumPermille, int MaximumToMedianMultiplierPermille)
{
    internal BalanceMatrix ToMatrix() => new(Seeds, ShipConfigurations, checked(HorizonHours * 3_600_000),
        checked(SampleIntervalHours * 3_600_000), checked(SaveLoadCheckpointHours * 3_600_000));
    internal BalanceMatrixFile Canonical() => this with
    {
        Seeds = Seeds.Order().ToImmutableArray(),
        ShipConfigurations = ShipConfigurations.OrderBy(c => c.Id, StringComparer.Ordinal).ToImmutableArray()
    };
    internal bool IsAcceptanceCorpus => Seeds.Order().SequenceEqual(new ulong[] { 1, 2, 3, 5, 8, 13, 21, 34, 55, 89, 144, 233 }) &&
        ShipConfigurations.OrderBy(c => c.Id, StringComparer.Ordinal).SequenceEqual(new[] { new BalanceShipConfiguration("cargo-upgrade", 2000, 1000), new("starter", 1000, 1000) }) &&
        HorizonHours == 240 && SampleIntervalHours == 1 && SaveLoadCheckpointHours == 120;
}
internal sealed record BalanceCaseSummary(ulong Seed, string ShipConfigurationId, int HourlySampleCount, int StationCount,
    int EventSampleCount, int StrategyCount, int KnownLedgerCount, ImmutableArray<BalanceLeader> Leaders);
internal sealed record BalanceReportSummary(bool IsAcceptanceCorpus, int CaseCount, int SampleCount, int StrategyCount,
    int CompletedStrategyCount, int RejectedStrategyCount, int PartialStrategyCount, int UnknownStrategyCount, int ViolationCount,
    ImmutableArray<BalanceCaseSummary> CaseSummaries);
internal sealed record BalanceReport(int SchemaVersion, string Status, BalanceMatrixFile Matrix, BalanceReportSummary Summary,
    ImmutableArray<BalanceCaseEvidence> Cases, ImmutableArray<BalanceViolation> Violations);

internal static class Program
{
    private static readonly JsonSerializerOptions Json = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = false,
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
        WriteIndented = true,
    };
    internal static string PackagedMatrixPath => Path.Combine(AppContext.BaseDirectory, "balance-matrix.json");
    private static int Main(string[] args) => Execute(args, Console.Out, Console.Error);

    internal static BalanceMatrixFile ReadMatrix(string path, bool explicitCustom)
    {
        string source = File.ReadAllText(path);
        using var document = JsonDocument.Parse(source);
        void Unique(JsonElement element)
        {
            if (element.ValueKind == JsonValueKind.Object)
            {
                var keys = new HashSet<string>(StringComparer.Ordinal);
                foreach (var property in element.EnumerateObject())
                {
                    if (!keys.Add(property.Name)) throw new BalanceConfigurationException("matrix: duplicate property " + property.Name);
                    Unique(property.Value);
                }
            }
            else if (element.ValueKind == JsonValueKind.Array) foreach (var item in element.EnumerateArray()) Unique(item);
        }
        Unique(document.RootElement);
        var matrix = JsonSerializer.Deserialize<BalanceMatrixFile>(source, Json) ?? throw new BalanceConfigurationException("matrix: null document.");
        if (matrix.SchemaVersion != 1 || matrix.ZeroStockMaximumPermille != 250 || matrix.ShortMarginMinimumPermille != 50 || matrix.ShortMarginMaximumPermille != 150 ||
            matrix.MediumMarginMinimumPermille != 150 || matrix.MediumMarginMaximumPermille != 350 || matrix.MaximumToMedianMultiplierPermille != 2000)
            throw new BalanceConfigurationException("matrix: schema1 and exact fixed acceptance thresholds are required.");
        EconomyBalanceRunner.Validate(matrix.ToMatrix());
        if (!explicitCustom && !matrix.IsAcceptanceCorpus) throw new BalanceConfigurationException("packaged matrix: exact24-case acceptance corpus required.");
        return matrix.Canonical();
    }

    internal static ImmutableArray<BalanceViolation> Evaluate(ImmutableArray<BalanceCaseEvidence> cases) => BalanceViolation.Ordered(
        cases.SelectMany(c => MarketHealthEvaluator.Evaluate(c).Concat(StrategyBalanceEvaluator.EvaluateCase(c))).Concat(StrategyBalanceEvaluator.EvaluateCorpus(cases)));

    private static BalanceStrategyEvidence CanonicalStrategy(BalanceStrategyEvidence s) => s with
    {
        DepartureRoute = s.DepartureRoute is { } route ? route with { EventIds = route.EventIds.Order(StringComparer.Ordinal).ToImmutableArray() } : null,
        PostingIds = s.PostingIds.Order(StringComparer.Ordinal).ToImmutableArray(),
        Replays = s.Replays.OrderBy(r => r.CommandId, StringComparer.Ordinal).ToImmutableArray(),
        RoundTripLegs = s.RoundTripLegs.Select(CanonicalStrategy).ToImmutableArray(), // Physical leg sequence is semantic.
    };
    private static BalanceCaseEvidence CanonicalCase(BalanceCaseEvidence c) => c with
    {
        HourlySamples = c.HourlySamples.OrderBy(s => s.GameTimeMs).Select(s => s with
        {
            Stations = s.Stations.OrderBy(st => st.StationId, StringComparer.Ordinal).Select(st => st with
            {
                Stocks = st.Stocks.OrderBy(i => i.ItemTypeId, StringComparer.Ordinal).Select(i => i with { InfluencingEventIds = i.InfluencingEventIds.Order(StringComparer.Ordinal).ToImmutableArray() }).ToImmutableArray(),
            }).ToImmutableArray(),
            Routes = s.Routes.OrderBy(r => r.Origin, StringComparer.Ordinal).ThenBy(r => r.Destination, StringComparer.Ordinal)
                .Select(r => r with { EventIds = r.EventIds.Order(StringComparer.Ordinal).ToImmutableArray() }).ToImmutableArray(),
            CargoFlows = s.CargoFlows.OrderBy(f => f.Origin, StringComparer.Ordinal).ThenBy(f => f.Destination, StringComparer.Ordinal).ThenBy(f => f.ItemTypeId, StringComparer.Ordinal).ToImmutableArray(),
            Events = s.Events.OrderBy(e => e.StationId, StringComparer.Ordinal).ThenBy(e => e.EventId, StringComparer.Ordinal)
                .Select(e => e with { InfluencedItems = e.InfluencedItems.Order(StringComparer.Ordinal).ToImmutableArray() }).ToImmutableArray(),
        }).ToImmutableArray(),
        Strategies = c.Strategies.OrderBy(s => s.StateGameTimeMs).ThenBy(s => s.Origin, StringComparer.Ordinal).ThenBy(s => s.Destination, StringComparer.Ordinal)
            .ThenBy(s => s.ItemTypeId, StringComparer.Ordinal).Select(CanonicalStrategy).ToImmutableArray(),
    };
    internal static BalanceReport BuildReport(BalanceMatrixFile matrix, ImmutableArray<BalanceCaseEvidence> evidence, ImmutableArray<BalanceViolation> violations)
    {
        matrix = matrix.Canonical();
        var cases = evidence.OrderBy(c => c.Seed).ThenBy(c => c.ShipConfigurationId, StringComparer.Ordinal).Select(CanonicalCase).ToImmutableArray();
        var expected = matrix.Seeds.SelectMany(seed => matrix.ShipConfigurations.Select(c => (Seed: seed, Config: c.Id))).ToHashSet();
        var actual = cases.Select(c => (Seed: c.Seed, Config: c.ShipConfigurationId)).ToArray();
        if (actual.Distinct().Count() != actual.Length || !expected.SetEquals(actual))
            violations = violations.Add(new("invalid_evidence", 0, "corpus", null, null, null, null, "complete unique matrix seed/config dimensions", "caseCount=" + cases.Length));
        violations = BalanceViolation.Ordered(violations);
        bool release = matrix.IsAcceptanceCorpus && cases.Length == 24 && violations.Length == 0;
        string status = release ? "pass" : violations.Length == 0 ? "diagnostic-pass" : "violations";
        var strategies = cases.SelectMany(c => c.Strategies).ToArray();
        var summaries = cases.Select(c => new BalanceCaseSummary(c.Seed, c.ShipConfigurationId, c.HourlySamples.Length,
            c.HourlySamples.SelectMany(s => s.Stations).Select(s => s.StationId).Distinct(StringComparer.Ordinal).Count(),
            c.HourlySamples.Count(s => s.Events.Length > 0), c.Strategies.Length, c.Strategies.Count(s => s.Ledger?.NetProfitCredits is not null), StrategyBalanceEvaluator.Leaders(c))).ToImmutableArray();
        return new(1, status, matrix, new(matrix.IsAcceptanceCorpus, cases.Length, cases.Sum(c => c.HourlySamples.Length), strategies.Length,
            strategies.Count(s => s.Outcome == "completed"), strategies.Count(s => s.Outcome == "rejected"), strategies.Count(s => s.Outcome == "partial"),
            strategies.Count(s => s.Outcome == "unknown"), violations.Length, summaries), cases, violations);
    }
    internal static byte[] Bytes(BalanceReport report) => Encoding.UTF8.GetBytes(JsonSerializer.Serialize(report, Json).Replace("\r\n", "\n", StringComparison.Ordinal) + "\n");
    internal static void WriteAtomic(string outputPath, byte[] bytes)
    {
        string output = Path.GetFullPath(outputPath);
        string directory = Path.GetDirectoryName(output)!;
        Directory.CreateDirectory(directory);
        string temp = Path.Combine(directory, Path.GetFileName(output) + ".tmp-" + Guid.NewGuid().ToString("N"));
        try { File.WriteAllBytes(temp, bytes); File.Move(temp, output, true); }
        finally { if (File.Exists(temp)) File.Delete(temp); }
    }
    internal static int Execute(string[] args, TextWriter output, TextWriter error,
        Func<string, string, BalanceMatrix, ImmutableArray<BalanceCaseEvidence>>? run = null)
    {
        try
        {
            if (args.Length is not (2 or 4) || args.Any(string.IsNullOrWhiteSpace) || args.Length == 4 && args[2] != "--matrix")
                throw new BalanceConfigurationException("usage: <DSS-root> <output.json> [--matrix <matrix.json>]");
            string root = Path.GetFullPath(args[0]); string reportPath = Path.GetFullPath(args[1]);
            string matrixPath = args.Length == 4 ? Path.GetFullPath(args[3]) : PackagedMatrixPath;
            var matrix = ReadMatrix(matrixPath, args.Length == 4);
            string client = Path.GetFullPath(Path.Combine(root, "src", "DeepSpaceSaga.Client"));
            string settings = Path.Combine(client, "Settings.json");
            _ = EngineContentLoader.LoadRegistryFromSettingsFile(settings, out string basePath, out var loadedSettings);
            string scenario = Path.GetFullPath(Path.Combine(basePath, loadedSettings.DefaultScenario));
            if (!File.Exists(scenario) || string.Equals(Path.GetFileName(Path.GetDirectoryName(scenario)), "Default_500", StringComparison.OrdinalIgnoreCase))
                throw new BalanceConfigurationException("settings.defaultScenario: a shipped trading scenario is required.");
            // The entire shipped content subtree, selected scenario and both matrix inputs stay read-only.
            bool Same(string path) => reportPath.Equals(Path.GetFullPath(path), StringComparison.OrdinalIgnoreCase);
            if (reportPath.StartsWith(client + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase) || Same(scenario) || Same(matrixPath) || Same(PackagedMatrixPath) ||
                Same(Path.Combine(root, "tools", "DeepSpaceSaga.EconomyBalance", "balance-matrix.json")))
                throw new BalanceConfigurationException("output: may not replace repository content or a matrix input.");
            run ??= new EconomyBalanceRunner().Run;
            var cases = run(settings, scenario, matrix.ToMatrix());
            var report = BuildReport(matrix, cases, Evaluate(cases));
            WriteAtomic(reportPath, Bytes(report));
            output.WriteLine($"status={report.Status};cases={report.Cases.Length};violations={report.Violations.Length};output={reportPath}");
            return report.Status == "pass" ? 0 : 1;
        }
        catch (Exception ex)
        {
            error.WriteLine("configuration/runtime failure: " + ex.Message);
            return 2;
        }
    }
}
