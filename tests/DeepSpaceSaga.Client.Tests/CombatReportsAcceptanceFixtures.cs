using System.Runtime.CompilerServices;
using DeepSpaceSaga.Client.UI.Screens.GameSession;
using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Engine;
using DeepSpaceSaga.Engine.Content;
using DeepSpaceSaga.Engine.LocalClient;
using DeepSpaceSaga.Engine.Scenario;
using DeepSpaceSaga.Motion;

namespace DeepSpaceSaga.Client.Tests;

internal sealed class CombatReportsAcceptanceFixtures : IAsyncDisposable
{
    internal GameSessionHandle Handle { get; }
    internal GameSessionScreen Screen { get; private set; } = null!;
    internal BattleReportSnapshot[] Expected { get; }

    private CombatReportsAcceptanceFixtures(ReplayConnection connection, BattleReportSnapshot[] expected)
    { Handle = new(connection); Expected = expected; }

    internal static async Task<CombatReportsAcceptanceFixtures> Create()
    {
        string root = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "../../../../../src/DeepSpaceSaga.Client"));
        var registry = EngineContentLoader.LoadRegistryFromSettingsFile(global::DeepSpaceSaga.TestSupport.LegacyCombatCatalog.Settings(Path.Combine(root, "Settings.json")), out _, out _);
        var scenario = ScenarioLoader.LoadFromFile(Path.Combine(root, "Scenarios/CombatMukha/scenario.json"));
        using var engine = new SimulationEngine(registry);
        engine.LoadScenario(scenario with { GameState = scenario.GameState with { MasterSeed = 1 } });
        var connection = new ReplayConnection(); var expected = new List<BattleReportSnapshot>();
        for (int time = 0; time <= 20000; time += 1000)
        {
            var snapshot = engine.CaptureSnapshotForTests(time, SimulationSpeed.Speed0, time);
            expected.AddRange(snapshot.CombatReports!.Reports); connection.Mailbox.Publish(snapshot);
        }
        var fixture = new CombatReportsAcceptanceFixtures(connection, expected.ToArray());
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        while (fixture.Handle.Buffer.Latest is null) await Task.Delay(1, timeout.Token);
        fixture.Screen = new(fixture.Handle.Buffer, new LinearMotionPredictor(), fixture.Handle);
        return fixture;
    }

    public async ValueTask DisposeAsync()
    { Screen.DisposeCombatReports(); await Handle.DisposeAsync(); }

    private sealed class ReplayConnection : IGameSessionConnection
    {
        internal SnapshotMailbox Mailbox { get; } = new();
        public ValueTask SendCommandAsync(PlayerCommand command, CancellationToken cancellationToken = default) => ValueTask.CompletedTask;
        public ValueTask SendDialogueCommandAsync(DialogueCommand command, CancellationToken cancellationToken = default) => ValueTask.CompletedTask;
        public ValueTask SetSimulationSpeedAsync(SimulationSpeed speed, CancellationToken cancellationToken = default) => ValueTask.CompletedTask;
        public ValueTask SetObjectInteractionStateAsync(string? activeObjectId, string? selectedObjectId, CancellationToken cancellationToken = default) => ValueTask.CompletedTask;
        public ValueTask SaveAsync(string slotId, CancellationToken cancellationToken = default) => ValueTask.CompletedTask;
        public async IAsyncEnumerable<AuthoritativeSnapshot> ReadSnapshotsAsync([EnumeratorCancellation] CancellationToken cancellationToken = default)
        { await foreach (var snapshot in Mailbox.ReadAllAsync(cancellationToken)) yield return snapshot; }
        public ValueTask DisposeAsync() { Mailbox.Complete(); return ValueTask.CompletedTask; }
    }
}
