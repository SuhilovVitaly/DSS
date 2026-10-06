using System.Collections.Immutable;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using DeepSpaceSaga.EconomyBalance;

namespace DeepSpaceSaga.EconomyBalance.Tests;

public sealed class BalanceReportTests : IDisposable
{
    private readonly string _directory = Path.Combine(Path.GetTempPath(), "DSS-balance-tests-" + Guid.NewGuid().ToString("N"));
    private string Output => Path.Combine(_directory, "report.json");
    public BalanceReportTests() => Directory.CreateDirectory(_directory);
    public void Dispose() => Directory.Delete(_directory, true);
    private static BalanceMatrixFile Packaged => Program.ReadMatrix(Program.PackagedMatrixPath, false);
    private string Matrix(Action<JsonObject>? change = null)
    {
        var json = JsonNode.Parse(File.ReadAllText(Program.PackagedMatrixPath))!.AsObject();
        change?.Invoke(json);
        string path = Path.Combine(_directory, "matrix.json");
        File.WriteAllText(path, json.ToJsonString());
        return path;
    }
    private int Execute(string? matrix = null, Func<string, string, BalanceMatrix, ImmutableArray<BalanceCaseEvidence>>? run = null) =>
        Program.Execute(matrix is null ? [BalanceRunTests.Root, Output] : [BalanceRunTests.Root, Output, "--matrix", matrix], new StringWriter(), new StringWriter(), run);

    [Fact]
    public void Packaged_matrix_is_exact_acceptance_corpus()
    {
        var matrix = Packaged;
        Assert.True(matrix.IsAcceptanceCorpus);
        Assert.Equal(24, matrix.Seeds.Length * matrix.ShipConfigurations.Length);
        Assert.Equal(240, matrix.HorizonHours); Assert.Equal(120, matrix.SaveLoadCheckpointHours);
        Assert.Equal(new[] { "cargo-upgrade", "starter" }, matrix.ShipConfigurations.Select(c => c.Id));
        Assert.Equal(File.ReadAllBytes(Path.Combine(BalanceRunTests.Root, "tools/DeepSpaceSaga.EconomyBalance/balance-matrix.json")), File.ReadAllBytes(Program.PackagedMatrixPath));
    }

    [Fact]
    public void Unknown_property_duplicate_seed_or_changed_default_threshold_is_code_two()
    {
        foreach (Action<JsonObject> change in new Action<JsonObject>[]
        {
            j => j["unexpected"] = 1, j => j["SchemaVersion"] = 1, j => j["schemaVersion"] = 2,
            j => j["seeds"] = new JsonArray(1, 1), j => j["zeroStockMaximumPermille"] = 251,
            j => j.Remove("shortMarginMinimumPermille"), j => j["horizonHours"] = "240",
            j => j["shipConfigurations"]![0]!["fuelEfficiencyMultiplierPermille"] = 999,
        })
        {
            bool ran = false;
            Assert.Equal(2, Execute(Matrix(change), (_, _, _) => { ran = true; return []; }));
            Assert.False(ran); Assert.False(File.Exists(Output));
        }
        string duplicate = Matrix();
        File.WriteAllText(duplicate, File.ReadAllText(duplicate).Replace("\"schemaVersion\":1", "\"schemaVersion\":1,\"schemaVersion\":1", StringComparison.Ordinal));
        Assert.Equal(2, Execute(duplicate, (_, _, _) => []));
    }

    [Fact]
    public void Reduced_matrix_requires_explicit_option_and_never_has_release_status()
    {
        string path = Matrix(j => { j["seeds"] = new JsonArray(1); j["shipConfigurations"] = new JsonArray(j["shipConfigurations"]![0]!.DeepClone()); });
        Assert.Throws<BalanceConfigurationException>(() => Program.ReadMatrix(path, false));
        var matrix = Program.ReadMatrix(path, true);
        var c = MarketHealthEvaluatorTests.Healthy();
        var report = Program.BuildReport(matrix, [c], []);
        Assert.Equal("diagnostic-pass", report.Status); Assert.False(report.Summary.IsAcceptanceCorpus);
        Assert.Equal(1, Execute(path, (_, _, _) => [c]));
    }

    [Fact]
    public void Reduced_fixture_writes_schema_complete_diagnostic_report_but_not_release_pass()
    {
        string path = Matrix(j =>
        {
            j["seeds"] = new JsonArray(1); j["shipConfigurations"] = new JsonArray(j["shipConfigurations"]![0]!.DeepClone());
            j["horizonHours"] = 2; j["saveLoadCheckpointHours"] = 1;
        });
        Assert.Equal(1, Execute(path)); // Real settings, real free-flight scenario, quotes, docking and finance owners.
        using var document = JsonDocument.Parse(File.ReadAllBytes(Output));
        var root = document.RootElement;
        Assert.Equal(new[] { "schemaVersion", "status", "matrix", "summary", "cases", "violations" }, root.EnumerateObject().Select(p => p.Name));
        Assert.False(root.GetProperty("summary").GetProperty("isAcceptanceCorpus").GetBoolean());
        var c = Assert.Single(root.GetProperty("cases").EnumerateArray());
        Assert.Equal(c.GetProperty("continuousStateHash").GetString(), c.GetProperty("saveLoadStateHash").GetString());
        Assert.Equal(3, c.GetProperty("hourlySamples").GetArrayLength());
        Assert.Contains(c.GetProperty("strategies").EnumerateArray(), s => s.GetProperty("ledger").ValueKind == JsonValueKind.Object);
        Assert.NotEqual("pass", root.GetProperty("status").GetString());
    }

    [Fact]
    public void Same_evidence_serializes_byte_identically_without_machine_or_time_fields()
    {
        var c = MarketHealthEvaluatorTests.Healthy();
        var reordered = c with
        {
            HourlySamples = c.HourlySamples.Reverse().Select(s => s with
            { Stations = s.Stations.Reverse().Select(st => st with { Stocks = st.Stocks.Reverse().ToImmutableArray() }).ToImmutableArray(), Routes = s.Routes.Reverse().ToImmutableArray() }).ToImmutableArray()
        };
        byte[] first = Program.Bytes(Program.BuildReport(Packaged, [c], Program.Evaluate([c])));
        byte[] second = Program.Bytes(Program.BuildReport(Packaged, [reordered], Program.Evaluate([reordered])));
        Assert.Equal(first, second); Assert.Equal((byte)'\n', first[^1]); Assert.NotEqual((byte)0xEF, first[0]); Assert.DoesNotContain((byte)'\r', first);
        string json = Encoding.UTF8.GetString(first);
        Assert.DoesNotContain("generatedAt", json); Assert.DoesNotContain(BalanceRunTests.Root, json); Assert.DoesNotContain(_directory, json);
    }

    [Fact]
    public void Violation_report_contains_seed_config_route_item_ledger_expected_and_observed()
    {
        var c = MarketHealthEvaluatorTests.Healthy() with { SaveLoadStateHash = "wrong" };
        var violation = new BalanceViolation("test", 1, "starter", "A", "A/B", "ore", 3600000, "expected", "observed");
        using var document = JsonDocument.Parse(Program.Bytes(Program.BuildReport(Packaged, [c], [violation])));
        var v = Assert.Single(document.RootElement.GetProperty("violations").EnumerateArray(), v => v.GetProperty("code").GetString() == "test");
        Assert.Equal(1UL, v.GetProperty("seed").GetUInt64()); Assert.Equal("starter", v.GetProperty("shipConfigurationId").GetString());
        Assert.Equal("A/B", v.GetProperty("routeId").GetString()); Assert.Equal("ore", v.GetProperty("itemTypeId").GetString());
        Assert.Equal("A", v.GetProperty("stationObjectId").GetString()); Assert.Equal(3600000, v.GetProperty("gameTimeMs").GetInt64());
        Assert.Equal("expected", v.GetProperty("expected").GetString()); Assert.Equal("observed", v.GetProperty("observed").GetString());
        var ledger = new BalanceLedgerEvidence("v", "A/B", "ore", 200, 100, 10, 20, 30, 40, 50, 30);
        string fields = JsonSerializer.Serialize(ledger);
        foreach (string field in new[] { "GrossSalesCredits", "CostOfGoodsSoldCredits", "RouteFuelCostCredits", "PortFeesAssessedCredits", "EventCostsCredits", "PassengerPayoutCredits", "PassengerPenaltyCredits", "NetProfitCredits" }) Assert.Contains(field, fields);
    }

    [Fact]
    public void Balance_failure_is_code_one_and_collects_health_and_strategy_violations()
    {
        var c = MarketHealthEvaluatorTests.Healthy() with { SaveLoadStateHash = "bad" };
        Assert.Equal(1, Execute(run: (_, _, _) => [c]));
        using var document = JsonDocument.Parse(File.ReadAllBytes(Output));
        var codes = document.RootElement.GetProperty("violations").EnumerateArray().Select(v => v.GetProperty("code").GetString()).ToArray();
        Assert.Contains("state_hash_mismatch", codes); Assert.Contains("missing_comparable_strategy", codes); Assert.Contains("invalid_evidence", codes);
    }

    [Fact]
    public void Runtime_failure_is_code_two_and_preserves_existing_output()
    {
        File.WriteAllText(Output, "previous good report");
        Assert.Equal(2, Execute(run: (_, _, _) => throw new IOException("injected runtime failure")));
        Assert.Equal("previous good report", File.ReadAllText(Output));
        Assert.Equal(2, Program.Execute([Path.Combine(_directory, "missing"), Output], new StringWriter(), new StringWriter()));
        Assert.Equal("previous good report", File.ReadAllText(Output));
    }

    [Fact]
    public void Atomic_replace_failure_preserves_previous_bytes_and_cleans_temporary_sibling()
    {
        File.WriteAllText(Output, "old");
        using (var locked = new FileStream(Output, FileMode.Open, FileAccess.Read, FileShare.None))
        {
            var exception = Record.Exception(() => Program.WriteAtomic(Output, [1, 2, 3]));
            Assert.True(exception is IOException or UnauthorizedAccessException);
        }
        Assert.Equal("old", File.ReadAllText(Output)); Assert.Empty(Directory.GetFiles(_directory, "*.tmp-*"));
        Program.WriteAtomic(Output, [4, 5]); Assert.Equal(new byte[] { 4, 5 }, File.ReadAllBytes(Output));
    }

    [Fact]
    public void Strict_arguments_and_protected_inputs_fail_before_runner()
    {
        foreach (var args in new string[][] { [], ["a"], ["a", "b", "--other", "c"], ["", "b"] })
            Assert.Equal(2, Program.Execute(args, new StringWriter(), new StringWriter()));
        string input = Path.Combine(BalanceRunTests.Root, "src/DeepSpaceSaga.Client/Settings.json");
        byte[] before = File.ReadAllBytes(input);
        bool ran = false;
        Assert.Equal(2, Program.Execute([BalanceRunTests.Root, input], new StringWriter(), new StringWriter(), (_, _, _) => { ran = true; return []; }));
        Assert.False(ran); Assert.Equal(before, File.ReadAllBytes(input));
    }

    [Fact]
    public void Missing_duplicate_or_extra_matrix_dimensions_never_produce_pass()
    {
        var cases = Packaged.Seeds.SelectMany(seed => Packaged.ShipConfigurations.Select(config => MarketHealthEvaluatorTests.Healthy() with { Seed = seed, ShipConfigurationId = config.Id })).ToImmutableArray();
        Assert.Equal("pass", Program.BuildReport(Packaged, cases, []).Status);
        foreach (var bad in new[] { cases.RemoveAt(0), cases.Add(cases[0]), cases.Add(cases[0] with { Seed = 999 }) })
            Assert.Contains(Program.BuildReport(Packaged, bad, []).Violations, v => v.Code == "invalid_evidence");
    }

    [Fact]
    public void Solution_build_discovers_tool_and_matching_tests()
    {
        string solution = File.ReadAllText(Path.Combine(BalanceRunTests.Root, "DeepSpaceSaga.sln"));
        Assert.Contains("tools\\DeepSpaceSaga.EconomyBalance\\DeepSpaceSaga.EconomyBalance.csproj", solution);
        Assert.Contains("tests\\DeepSpaceSaga.EconomyBalance.Tests\\DeepSpaceSaga.EconomyBalance.Tests.csproj", solution);
        Assert.Contains("{ADCD186C-7AA1-4F5C-B477-F3B39B023399}.Release|x64.Build.0", solution);
        Assert.Contains("{81E122D8-FC42-4ED6-96ED-A630E93CCA5E}.Debug|x86.Build.0", solution);
    }
}
