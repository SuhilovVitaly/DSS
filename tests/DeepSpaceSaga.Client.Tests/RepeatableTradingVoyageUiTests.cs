using System.Collections.Immutable;
using DeepSpaceSaga.Client.UI;
using DeepSpaceSaga.Client.UI.Screens;
using DeepSpaceSaga.Client.UI.Screens.Station;
using DeepSpaceSaga.Client.UI.Screens.Trade;
using DeepSpaceSaga.Contracts;
using SkiaSharp;

namespace DeepSpaceSaga.Client.Tests;

internal sealed class VoyageUiFixture : IAsyncDisposable
{
    internal sealed class Connection : IGameSessionConnection
    {
        internal List<SimulationSpeed> Speeds { get; } = [];
        internal List<PlayerCommand> Commands { get; } = [];
        internal List<TradeQuoteRequest> Quotes { get; } = [];
        internal TaskCompletionSource? SpeedGate { get; set; }

        public ValueTask SendCommandAsync(PlayerCommand command, CancellationToken cancellationToken = default)
        { Commands.Add(command); return ValueTask.CompletedTask; }
        public ValueTask SendDialogueCommandAsync(DialogueCommand command,
            CancellationToken cancellationToken = default) => ValueTask.CompletedTask;
        public async ValueTask SetSimulationSpeedAsync(SimulationSpeed speed,
            CancellationToken cancellationToken = default)
        {
            Speeds.Add(speed);
            if (SpeedGate is { } gate) await gate.Task;
        }
        public ValueTask<TradeQuoteSnapshot> GetTradeQuoteAsync(TradeQuoteRequest request,
            CancellationToken cancellationToken = default)
        {
            Quotes.Add(request);
            var snapshot = CurrentSnapshot!;
            return new(new TradeQuoteSnapshot(request.RequestId, $"quote-{Quotes.Count}",
                snapshot.DockedStationTrade?.MarketRevision ?? 0,
                snapshot.DockedStationTrade?.StationObjectId ?? "", request.ObjectId, request.ModuleId,
                request.CommandType, request.ItemTypeId, request.Quantity, request.Quantity, 100,
                request.Quantity * 17, [new(request.Quantity, 17)], null, []));
        }
        internal AuthoritativeSnapshot? CurrentSnapshot { get; set; }
        public ValueTask SetObjectInteractionStateAsync(string? activeObjectId, string? selectedObjectId,
            CancellationToken cancellationToken = default) => ValueTask.CompletedTask;
        public ValueTask SaveAsync(string slotId, CancellationToken cancellationToken = default) => ValueTask.CompletedTask;
        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
        public async IAsyncEnumerable<AuthoritativeSnapshot> ReadSnapshotsAsync(
            [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
        { await Task.Delay(Timeout.Infinite, cancellationToken); yield break; }
    }

    internal Connection Wire { get; } = new();
    internal GameSessionHandle Handle { get; }
    internal SnapshotBuffer Buffer => Handle.Buffer;

    internal VoyageUiFixture()
    {
        Handle = new(Wire);
        At("A", 100, 1);
    }

    internal AuthoritativeSnapshot At(string? station, long visitStart, ulong sequence,
        long credits = 1000, long water = 60)
    {
        var original = TradeUxTests.Snapshot();
        var snapshot = original with
        {
            SnapshotSequence = sequence,
            GameTimeMs = (long)sequence * 1000,
            CurrentSpeed = SimulationSpeed.Speed1,
            PlayerCredits = credits,
            Objects = original.Objects.Select(obj => obj.ObjectId == "ship"
                ? obj with { IsDocked = station is not null, DockedStationObjectId = station }
                : obj).ToImmutableArray(),
            InstalledModules = original.InstalledModules.Select(module => module.ModuleId == "hold-1"
                ? module with
                {
                    Cargo = module.Cargo.Select(cargo => cargo.ItemTypeId == "item.water"
                    ? cargo with { Quantity = water } : cargo).ToImmutableArray()
                }
                : module).Append(new InstalledModuleSnapshot("nav-1", "navigation", "Navigation", 3,
                    [NavigationComputerCommandTypes.Undock], "On", "Ready", 100)).ToImmutableArray(),
            DockedStationTrade = station is null ? null : original.DockedStationTrade! with
            { StationObjectId = station, MarketRevision = (long)sequence },
            PortFees = station is null ? null : new(visitStart, visitStart + GameCalendar.DayMs, 0),
            Voyage = new(station is null ? VoyagePhases.InTransit : VoyagePhases.Docked,
                RouteOptions: station is null ? [] :
                    [new(station == "A" ? "B" : "A", station == "A" ? "B" : "A", 1000, "Short")])
        };
        Wire.CurrentSnapshot = snapshot;
        Buffer.Update(snapshot);
        return snapshot;
    }

    internal StationScreen Station() => new(Buffer, Handle);
    internal TradeScreen Trade() => new(Buffer, Handle);
    internal static void Render(IScreen screen)
    {
        using var bitmap = new SKBitmap(1920, 1080);
        using var canvas = new SKCanvas(bitmap);
        screen.Render(canvas, 1920, 1080);
    }
    internal static (float X, float Y) TradeClick()
    {
        var rect = StationLayout.TradeButtonLocalRect();
        return (StationLayout.PanelLeft(1920) + (rect.Left + rect.Right) / 2f,
            StationLayout.PanelTop(1080) + (rect.Top + rect.Bottom) / 2f);
    }
    public ValueTask DisposeAsync() => Handle.DisposeAsync();
}

public sealed class RepeatableTradingVoyageUiTests
{
    [Fact]
    public async Task Two_round_trips_reuse_station_trade_flow_without_old_quotes()
    {
        await using var fixture = new VoyageUiFixture();
        for (int leg = 0; leg < 5; leg++)
        {
            string stationId = leg % 2 == 0 ? "A" : "B";
            fixture.At(stationId, 1000 + leg * 1000, (ulong)(leg * 2 + 1));
            var station = fixture.Station();
            VoyageUiFixture.Render(station);
            var click = VoyageUiFixture.TradeClick();
            Assert.Equal(ScreenEvent.OpenTrade, station.OnMouseDown(click.X, click.Y));
            var trade = fixture.Trade();
            trade.Model.Select("item.water");
            VoyageUiFixture.Render(trade);
            Assert.True(trade.HasValidVisit);
            Assert.Equal(stationId, trade.OpenedForStationObjectId);

            if (leg < 4)
            {
                Assert.Equal(ScreenEvent.CloseTrade, trade.OnKeyDown(Silk.NET.Input.Key.Escape));
                Assert.Equal(ScreenEvent.None, station.OnMouseDown(
                    StationLayout.PanelLeft(1920) + 410,
                    StationLayout.PanelTop(1080) + 450));
                Assert.Equal(stationId == "A" ? "B" : "A", station.SelectedDestinationId);
                var undock = StationLayout.UndockButtonLocalRect();
                Assert.Equal(ScreenEvent.Undock, station.OnMouseDown(
                    StationLayout.PanelLeft(1920) + (undock.Left + undock.Right) / 2f,
                    StationLayout.PanelTop(1080) + (undock.Top + undock.Bottom) / 2f));
                Assert.NotNull(fixture.Handle.SendUndockCommand(station.SelectedDestinationId));
                fixture.At(null, 0, (ulong)(leg * 2 + 2));
                VoyageUiFixture.Render(trade);
                Assert.False(trade.HasValidVisit);
                Assert.Null(trade.Model.AuthoritativeQuote);
                Assert.Equal(ScreenEvent.None, station.OnMouseDown(click.X, click.Y));
            }
        }
        Assert.Equal(5, fixture.Wire.Quotes.Count);
        Assert.Equal(4, fixture.Wire.Commands.Count);
        Assert.Equal(["B", "A", "B", "A"],
            fixture.Wire.Commands.Select(command => command.TargetObjectId));
    }

    [Fact]
    public async Task Cargo_credits_and_history_follow_current_snapshots_and_receipts()
    {
        await using var fixture = new VoyageUiFixture();
        var first = fixture.Trade();
        first.Model.Select("item.water");
        VoyageUiFixture.Render(first);
        Assert.True(first.CanConfirm);
        first.OnMouseDown(160 + TradeLayout.Confirm.MidX, 140 + TradeLayout.Confirm.MidY);
        var command = Assert.Single(fixture.Wire.Commands);
        var receipt = new TradeExecutionReceipt("A", "item.water", "quote-1", 1, 2,
            1, 1, 17, []);
        var result = new CommandResult(command.CommandId, "ship", command.ModuleId,
            command.CommandType, CommandResultStatus.Executed, 2000, TradeReceipt: receipt);
        var updated = fixture.At("A", 100, 2, credits: 983, water: 61);
        fixture.Buffer.Update(updated with { CommandResults = [result] });
        VoyageUiFixture.Render(first);
        Assert.Equal(61, first.Model.Cargo("item.water"));
        Assert.Equal(983, fixture.Buffer.Latest!.Snapshot.PlayerCredits);
        Assert.Equal(receipt, Assert.Single(first.History).ConfirmedReceipt);

        fixture.At("B", 200, 3, credits: 980, water: 59);
        var second = fixture.Trade();
        Assert.Equal(59, second.Model.Cargo("item.water"));
        Assert.Equal(980, fixture.Buffer.Latest!.Snapshot.PlayerCredits);
        Assert.Equal(receipt, Assert.Single(second.History).ConfirmedReceipt);
        Assert.Null(second.Model.SelectedItemId);
    }

    [Fact]
    public async Task Trade_close_returns_to_current_station_without_resuming_nested_modal()
    {
        await using var fixture = new VoyageUiFixture();
        var stack = new ScreenStack();
        stack.SetRoot(new StationScreen());
        var station = fixture.Station();
        stack.Push(station);
        stack.Push(fixture.Trade());
        int resumes = 0;
        async Task PopModal()
        {
            stack.Pop();
            if (stack.Count == 1)
            {
                resumes++;
                await fixture.Handle.SetSpeedAsync(SimulationSpeed.Speed1);
            }
        }
        await PopModal();
        Assert.Same(station, stack.Current);
        Assert.True(station.HasValidVisit);
        Assert.Equal(0, resumes);
        Assert.Empty(fixture.Wire.Speeds);
        await PopModal();
        Assert.Equal(1, resumes);
        Assert.Equal(SimulationSpeed.Speed1, Assert.Single(fixture.Wire.Speeds));
        Assert.Single(stack.AllBottomToTop());
    }
}
