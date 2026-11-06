using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Engine.LocalClient;
using static DeepSpaceSaga.Engine.Tests.PointCombatUS0003TK0002Tests;

namespace DeepSpaceSaga.Engine.Tests;

public sealed class DebugTuningStartupTests : IDisposable
{
    private readonly string _directory = Path.Combine("D:/DeepSpaceSaga/artifacts/validation/ep0013/startup-fixtures", Guid.NewGuid().ToString("N"));
    private string PathName => Path.Combine(_directory, "debug-tuning.json");
    public DebugTuningStartupTests() => Directory.CreateDirectory(_directory);
    private void Write(string json) => File.WriteAllText(PathName, json);

    [Fact]
    public void Partial_file_fills_catalog_defaults_and_file_edits_are_not_live()
    {
        using var engine = PointCombatUS0018TK0002Tests.Create();
        var defaults = engine.CaptureSnapshotForTests().DebugTuning!.Defaults;
        Write("{\"schemaVersion\":1,\"ships\":{\"module.engine.strizh\":{\"maxSpeedKmS\":12}}}");
        engine.ApplyStartupDebugTuningFile(PathName);
        var state = engine.CaptureSnapshotForTests().DebugTuning!;
        Assert.Null(state.StartupError); Assert.Equal(1, state.Revision);
        Assert.Equal(12, state.Values.Single(v => v.ModuleTypeId == "module.engine.strizh" && v.Field == "maxSpeedKmS").Value);
        Assert.All(state.Values.Where(v => v.ModuleTypeId != "module.engine.strizh" || v.Field != "maxSpeedKmS"),
            v => Assert.Contains(v, defaults));
        Write("{\"schemaVersion\":1,\"ships\":{\"module.engine.strizh\":{\"maxSpeedKmS\":20}}}");
        Assert.Equal(12, engine.CaptureSnapshotForTests().DebugTuning!.Values.Single(v =>
            v.ModuleTypeId == "module.engine.strizh" && v.Field == "maxSpeedKmS").Value);
    }

    [Theory]
    [InlineData("{")]
    [InlineData("null")]
    [InlineData("{\"schemaVersion\":2}")]
    [InlineData("{\"schemaVersion\":\"1\"}")]
    [InlineData("{\"schemaVersion\":1,\"unknown\":0}")]
    [InlineData("{\"schemaVersion\":1,\"schemaVersion\":1}")]
    [InlineData("{\"schemaVersion\":1,\"ships\":{\"unknown\":{}}}")]
    [InlineData("{\"schemaVersion\":1,\"ships\":{\"module.engine.strizh\":{\"maxSpeedKmS\":-1}}}")]
    [InlineData("{\"schemaVersion\":1,\"ships\":{\"module.engine.strizh\":{\"maxSpeedKmS\":1e999}}}")]
    [InlineData("{\"schemaVersion\":1,\"ships\":{\"module.engine.strizh\":{\"unknown\":12}}}")]
    [InlineData("{\"schemaVersion\":1,\"ships\":{\"module.engine.strizh\":{\"maxSpeedKmS\":\"12\"}}}")]
    [InlineData("{\"schemaVersion\":1,\"launchers\":{\"module.torpedo.launcher.basic\":{\"reloadMs\":999.5}}}")]
    [InlineData("{\"schemaVersion\":1,\"launchers\":{\"module.torpedo.launcher.basic\":{\"profiles\":{\"wrong\":{}}}}}")]
    [InlineData("{\"schemaVersion\":1,\"writtenAtUtc\":0}")]
    [InlineData("{\"schemaVersion\":1,\"source\":\"overlay\"}")]
    public void Rejected_file_is_atomic_logs_path_and_exposes_a_panel_error(string json)
    {
        using var engine = PointCombatUS0018TK0002Tests.Create();
        Write(json);
        var warnings = new List<string>();
        engine.ApplyStartupDebugTuningFile(PathName, warnings.Add);
        var state = engine.CaptureSnapshotForTests().DebugTuning!;
        Assert.Equal(state.Defaults.ToArray(), state.Values.ToArray());
        Assert.Equal(0, state.Revision);
        Assert.NotNull(state.StartupError); Assert.Contains(PathName, state.StartupError);
        Assert.Contains(PathName, Assert.Single(warnings));
    }

    [Fact]
    public void Catalog_reference_file_can_be_copied_to_current_file()
    {
        using var engine = PointCombatUS0018TK0002Tests.Create();
        var defaults = engine.CaptureSnapshotForTests().DebugTuning!.Defaults;
        Write(DebugTuningFileFormat.Serialize(defaults, DateTimeOffset.UtcNow, catalog: true));
        engine.ApplyStartupDebugTuningFile(PathName);
        var state = engine.CaptureSnapshotForTests().DebugTuning!;
        Assert.Null(state.StartupError);
        Assert.Equal(defaults.ToArray(), state.Values.ToArray());
    }

    [Fact]
    public async Task Local_session_factory_applies_file_before_first_transport_snapshot()
    {
        Write("{\"schemaVersion\":1,\"ships\":{\"module.engine.strizh\":{\"maxSpeedKmS\":12}}}");
        await using var connection = LocalGameSessionConnection.CreateFromScenarioFile(
            Path.Combine(TorpedoImpactTests.ClientRoot, "Settings.json"),
            Path.Combine(TorpedoImpactTests.ClientRoot, "Scenarios/PlayerShipOnly/scenario.json"), debugTuningPath: PathName);
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        await using var snapshots = connection.ReadSnapshotsAsync(timeout.Token).GetAsyncEnumerator();
        Assert.True(await snapshots.MoveNextAsync());
        Assert.Equal(12, snapshots.Current.DebugTuning!.Values.Single(v =>
            v.ModuleTypeId == "module.engine.strizh" && v.Field == "maxSpeedKmS").Value);
    }

    public void Dispose() => Directory.Delete(_directory, recursive: true);
}
