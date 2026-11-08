using System.Runtime.CompilerServices;
using System.Threading.Channels;
using DeepSpaceSaga.Client.UI;
using DeepSpaceSaga.Client.UI.Screens;
using DeepSpaceSaga.Client.UI.Screens.TacticalBattle;
using DeepSpaceSaga.Contracts;

namespace DeepSpaceSaga.Client.Tests.TacticalBattle;

[Collection("InterfaceLog")]
public sealed class SessionHandleBattleTests
{
    [Fact]
    public async Task ReplayAndDuplicateResponseDoNotDuplicateJournal()
    {
        await using var fixture = await BattleNavigationTests.Fixture.Create();
        var screen = await ReachFire(fixture);
        var before = screen.Flow.State!;
        fixture.ManualAnimation = true;
        await fixture.Route(ScreenEvent.FireTacticalBattle);
        var accepted = screen.Flow.State!;
        fixture.AdvanceAnimation(500);
        var first = screen.CaptureFrame(1280, 720)!;
        var duplicate = new TacticalBattleResponse("duplicate", true, TacticalBattleRejectionCode.None, accepted.Phase, accepted, []);
        screen.OnBattleUpdated(before, duplicate);
        Assert.True(screen.HasSalvoPresentation);
        var after = screen.CaptureFrame(1280, 720)!;
        Assert.Equal(first.Salvo!.Flights.Select(f => f.ElapsedMs), after.Salvo!.Flights.Select(f => f.ElapsedMs));
        Assert.Equal(first.Battle.Log.AsEnumerable(), after.Battle.Log);
        screen.OnBattleUpdated(before, duplicate with { Accepted = false, Code = TacticalBattleRejectionCode.StaleRevision, State = before });
        Assert.True(screen.HasSalvoPresentation);
        screen.OnKeyDown(Silk.NET.Input.Key.Enter);
        Assert.Equal(accepted.Log.AsEnumerable(), screen.CaptureFrame(1280, 720)!.Battle.Log);
        using var bitmap = new SkiaSharp.SKBitmap(1280, 720);
        using var canvas = new SkiaSharp.SKCanvas(bitmap);
        screen.Render(canvas, 1280, 720);
        string log = ReadLog();
        var replay = screen.ReplaySalvoBounds;
        screen.OnMouseDown(replay.MidX, replay.MidY, Silk.NET.Input.MouseButton.Left);
        Assert.Equal(before.Log.AsEnumerable(), screen.CaptureFrame(1280, 720)!.Battle.Log);
        Assert.Equal(log, ReadLog());
        Assert.Equal(1, fixture.Proxy.Fires);
        Assert.Same(accepted, screen.Flow.State);
    }

    [Fact]
    public async Task NewBattleDoesNotReuseOldQueueOrLogPrefix()
    {
        await using var fixture = await BattleNavigationTests.Fixture.Create();
        var screen = await ReachFire(fixture);
        fixture.ManualAnimation = true;
        await fixture.Route(ScreenEvent.FireTacticalBattle);
        var old = screen.Flow.State!;
        Assert.True(screen.HasSalvoPresentation);
        // Recover a live battle that replaced the stale client's battle at the actual owner boundary.
        Assert.True(fixture.Engine.ExitTacticalBattle(new("external-exit", old.BattleId)).Accepted);
        var live = fixture.Engine.StartTacticalBattle(new("live-battle", 2)).State!;
        var refused = await screen.Flow.FireAsync(new("stale-address", old.BattleId, old.Revision));
        Assert.Equal(TacticalBattleRejectionCode.WrongBattle, refused!.Code);
        screen.OnBattleUpdated(old, refused);
        var frame = screen.CaptureFrame(1280, 720)!;
        Assert.Equal(live.BattleId, frame.Battle.BattleId);
        Assert.False(screen.HasSalvoPresentation);
        Assert.Empty(frame.Battle.FireQueue);
        Assert.Empty(frame.Battle.FireSteps);
        Assert.Equal(live.Log.AsEnumerable(), TacticalBattleLogTab.Visible(frame));
        Assert.NotEqual(old.BattleId, frame.Battle.BattleId);
    }

    private static async Task<TacticalBattleScreen> ReachFire(BattleNavigationTests.Fixture fixture)
    {
        await fixture.Route(ScreenEvent.OpenTacticalBattle);
        var setup = Assert.IsType<TacticalBattleSetupScreen>(fixture.Screens.Current);
        await setup.Flow.OptionsTask!;
        await fixture.Route(ScreenEvent.StartTacticalBattle);
        var screen = Assert.IsType<TacticalBattleScreen>(fixture.Screens.Current);
        for (int turn = 0; turn < 30 && screen.Flow.State?.Phase == TacticalBattlePhase.Move; turn++)
            await fixture.Route(ScreenEvent.MoveTacticalBattle);
        fixture.AdvanceAnimation(TacticalBattleMovementAnimation.FireTransitionDurationMs);
        return screen;
    }

    [Fact]
    public async Task AllMethodsForwardOriginalPayloadsAndLogCommandsAndRefusals()
    {
        var connection = new Connection();
        await using var handle = new GameSessionHandle(connection, deferDebugTuningWrites: true);
        Assert.Same(connection.Options, await handle.GetTacticalBattleOptionsAsync());
        Assert.Null(await handle.GetTacticalBattleStateAsync());
        string id = Guid.NewGuid().ToString("N");
        var start = new StartTacticalBattleRequest(id + "-start", -1, "initial", "raid", "defence",
            [new("Vera", "gun", "plate", null, 0)]);
        var move = new MoveTacticalBattleRequest(id + "-move", "battle", 3, [new("Vera", 12.34567890123456, -7.125)]);
        var fire = new FireTacticalBattleRequest(id + "-fire", "battle", 4, [new("Vera", null)]);
        var exit = new ExitTacticalBattleRequest(id + "-exit", "battle");
        foreach (var response in new[]
        {
            await handle.StartTacticalBattleAsync(start), await handle.MoveTacticalBattleAsync(move),
            await handle.FireTacticalBattleAsync(fire), await handle.ExitTacticalBattleAsync(exit)
        })
        {
            Assert.False(response.Accepted);
            Assert.Equal(TacticalBattleRejectionCode.WrongPhase, response.Code);
            Assert.Equal(TacticalBattlePhase.Fire, response.CurrentPhase);
            Assert.Empty(response.NewEvents);
        }
        Assert.Equal(new object[] { start, move, fire, exit }, connection.Requests);
        string log = ReadLog();
        foreach (string operation in new[] { "start", "move", "fire", "exit" })
        {
            Assert.Contains("TACTICAL send " + operation + ":", log);
            Assert.Contains($"command={id}-{operation}, code=WrongPhase, phase=Fire", log);
        }
        Assert.Contains("12.34567890123456", log);
        Assert.Contains("\"TargetId\":null", log);
        Assert.Null(handle.Failure);
    }

    [Fact]
    public async Task BrokenTransportProducesTypedUnavailableAndDiagnosticInsteadOfUiException()
    {
        var connection = new Connection { Throw = true };
        await using var handle = new GameSessionHandle(connection, deferDebugTuningWrites: true);
        Assert.False((await handle.GetTacticalBattleOptionsAsync()).Availability.Available);
        Assert.Null(await handle.GetTacticalBattleStateAsync());
        string id = Guid.NewGuid().ToString("N");
        var response = await handle.StartTacticalBattleAsync(new(id, 1));
        Assert.Equal(id, response.CommandId);
        Assert.Equal(TacticalBattleRejectionCode.Unavailable, response.Code);
        Assert.False(response.Accepted);
        Assert.Contains($"command={id}, code=Unavailable, phase=none", ReadLog());
        Assert.Null(handle.Failure);
    }

    [Fact]
    public async Task CallerCancellationIsLoggedAndDoesNotBecomeGameplayRejection()
    {
        var connection = new Connection();
        await using var handle = new GameSessionHandle(connection, deferDebugTuningWrites: true);
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        string id = Guid.NewGuid().ToString("N");
        var task = handle.StartTacticalBattleAsync(new(id, 1), cancellation.Token);
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => task.AsTask());
        Assert.Contains($"TACTICAL canceled start: command={id}", ReadLog());
        Assert.Empty(connection.Requests);
    }

    [Theory]
    [InlineData("Russian")]
    [InlineData("English")]
    public void EveryRefusalHasRealLocalizedTextAndUnknownCodesUseGenericMessage(string language)
    {
        using var scope = Localization.UseLocaleForTests(language);
        foreach (var code in Enum.GetValues<TacticalBattleRejectionCode>())
        {
            string text = GameSessionHandle.TacticalBattleRefusalText(code);
            if (code == TacticalBattleRejectionCode.None) Assert.Empty(text);
            else
            {
                Assert.False(string.IsNullOrWhiteSpace(text));
                Assert.DoesNotContain("TacticalBattle.", text);
                Assert.NotEqual(code.ToString(), text);
            }
        }
        Assert.Equal(GameSessionHandle.TacticalBattleRefusalText(TacticalBattleRejectionCode.Unavailable),
            GameSessionHandle.TacticalBattleRefusalText((TacticalBattleRejectionCode)999));
    }

    private static string ReadLog()
    {
        using var stream = new FileStream(InterfaceLog.FileName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }

    private sealed class Connection : IGameSessionConnection
    {
        private readonly Channel<AuthoritativeSnapshot> _snapshots = Channel.CreateUnbounded<AuthoritativeSnapshot>();
        public TacticalBattleOptions Options { get; } = new(new(true), [], [], [], [], []);
        public List<object> Requests { get; } = [];
        public bool Throw { get; init; }
        public ValueTask<TacticalBattleOptions> GetTacticalBattleOptionsAsync(CancellationToken cancellationToken = default) =>
            Throw ? throw new IOException("test transport") : ValueTask.FromResult(Options);
        public ValueTask<TacticalBattleSnapshot?> GetTacticalBattleStateAsync(CancellationToken cancellationToken = default) =>
            Throw ? throw new IOException("test transport") : ValueTask.FromResult<TacticalBattleSnapshot?>(null);
        private ValueTask<TacticalBattleResponse> Reply(object request, string id, CancellationToken token)
        {
            if (token.IsCancellationRequested) return ValueTask.FromCanceled<TacticalBattleResponse>(token);
            if (Throw) throw new IOException("test transport");
            Requests.Add(request);
            return ValueTask.FromResult(new TacticalBattleResponse(id, false, TacticalBattleRejectionCode.WrongPhase, TacticalBattlePhase.Fire, null, []));
        }
        public ValueTask<TacticalBattleResponse> StartTacticalBattleAsync(StartTacticalBattleRequest request, CancellationToken cancellationToken = default) => Reply(request, request.CommandId, cancellationToken);
        public ValueTask<TacticalBattleResponse> MoveTacticalBattleAsync(MoveTacticalBattleRequest request, CancellationToken cancellationToken = default) => Reply(request, request.CommandId, cancellationToken);
        public ValueTask<TacticalBattleResponse> FireTacticalBattleAsync(FireTacticalBattleRequest request, CancellationToken cancellationToken = default) => Reply(request, request.CommandId, cancellationToken);
        public ValueTask<TacticalBattleResponse> ExitTacticalBattleAsync(ExitTacticalBattleRequest request, CancellationToken cancellationToken = default) => Reply(request, request.CommandId, cancellationToken);
        public ValueTask SendCommandAsync(PlayerCommand command, CancellationToken cancellationToken = default) => ValueTask.CompletedTask;
        public ValueTask SendDialogueCommandAsync(DialogueCommand command, CancellationToken cancellationToken = default) => ValueTask.CompletedTask;
        public ValueTask SetSimulationSpeedAsync(SimulationSpeed speed, CancellationToken cancellationToken = default) => ValueTask.CompletedTask;
        public ValueTask SetObjectInteractionStateAsync(string? activeObjectId, string? selectedObjectId, CancellationToken cancellationToken = default) => ValueTask.CompletedTask;
        public async IAsyncEnumerable<AuthoritativeSnapshot> ReadSnapshotsAsync([EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            await foreach (var snapshot in _snapshots.Reader.ReadAllAsync(cancellationToken)) yield return snapshot;
        }
        public ValueTask SaveAsync(string slotId, CancellationToken cancellationToken = default) => ValueTask.CompletedTask;
        public ValueTask DisposeAsync() { _snapshots.Writer.TryComplete(); return ValueTask.CompletedTask; }
    }
}
