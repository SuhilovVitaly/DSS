using System.Collections.Immutable;
using DeepSpaceSaga.Client.UI.Screens.Trade;
using DeepSpaceSaga.Contracts;
using SkiaSharp;

namespace DeepSpaceSaga.Client.Tests;

public sealed class TradeVisitContextTests
{
    private sealed class ControlledConnection : IGameSessionConnection
    {
        internal List<TradeQuoteRequest> Requests { get; } = [];
        internal List<TaskCompletionSource<TradeQuoteSnapshot>> PendingQuotes { get; } = [];
        internal List<CancellationToken> Tokens { get; } = [];
        internal List<PlayerCommand> Commands { get; } = [];

        public ValueTask<TradeQuoteSnapshot> GetTradeQuoteAsync(TradeQuoteRequest request,
            CancellationToken cancellationToken = default)
        {
            Requests.Add(request);
            Tokens.Add(cancellationToken);
            var pending = new TaskCompletionSource<TradeQuoteSnapshot>(TaskCreationOptions.RunContinuationsAsynchronously);
            PendingQuotes.Add(pending);
            return new(pending.Task);
        }

        internal TradeQuoteSnapshot Answer(int index, string station = "A", long revision = 7)
        {
            var request = Requests[index];
            var quote = new TradeQuoteSnapshot(request.RequestId, $"quote-{request.RequestId}", revision,
                station, request.ObjectId, request.ModuleId, request.CommandType, request.ItemTypeId,
                request.Quantity, request.Quantity, 100, checked(request.Quantity * 17),
                [new(request.Quantity, 17)], null, []);
            PendingQuotes[index].SetResult(quote);
            return quote;
        }

        public ValueTask SendCommandAsync(PlayerCommand command, CancellationToken cancellationToken = default)
        { Commands.Add(command); return ValueTask.CompletedTask; }
        public ValueTask SendDialogueCommandAsync(DialogueCommand command,
            CancellationToken cancellationToken = default) => ValueTask.CompletedTask;
        public ValueTask SetSimulationSpeedAsync(SimulationSpeed speed,
            CancellationToken cancellationToken = default) => ValueTask.CompletedTask;
        public ValueTask SetObjectInteractionStateAsync(string? activeObjectId, string? selectedObjectId,
            CancellationToken cancellationToken = default) => ValueTask.CompletedTask;
        public ValueTask SaveAsync(string slotId, CancellationToken cancellationToken = default) => ValueTask.CompletedTask;
        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
        public async IAsyncEnumerable<AuthoritativeSnapshot> ReadSnapshotsAsync(
            [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
        { await Task.Delay(Timeout.Infinite, cancellationToken); yield break; }
    }

    private sealed class Fixture : IAsyncDisposable
    {
        internal ControlledConnection Connection { get; } = new();
        internal GameSessionHandle Handle { get; }
        internal SnapshotBuffer Buffer => Handle.Buffer;
        internal TradeScreen Screen { get; }

        internal Fixture()
        {
            Handle = new(Connection);
            Buffer.Update(At("A", 1, 100, 1000, 60));
            Screen = new(Buffer, Handle);
            Screen.OnActivated();
            Screen.Model.Select("item.water");
            Render(Screen);
        }

        public ValueTask DisposeAsync() => Handle.DisposeAsync();
    }

    private static AuthoritativeSnapshot At(string? station, ulong sequence, long visitStart,
        long credits, long waterCargo, long stock = 240, long revision = 7)
    {
        var source = TradeUxTests.Snapshot();
        return source with
        {
            SnapshotSequence = sequence,
            GameTimeMs = (long)sequence * 1000,
            PlayerCredits = credits,
            Objects = source.Objects.Select(obj => obj.ObjectId == "ship"
                ? obj with { IsDocked = station is not null, DockedStationObjectId = station }
                : obj).ToImmutableArray(),
            InstalledModules = source.InstalledModules.Select(module => module.ModuleId == "hold-1"
                ? module with
                {
                    Cargo = module.Cargo.Select(cargo => cargo.ItemTypeId == "item.water"
                    ? cargo with { Quantity = waterCargo } : cargo).ToImmutableArray()
                }
                : module).ToImmutableArray(),
            DockedStationTrade = station is null ? null : source.DockedStationTrade! with
            {
                StationObjectId = station,
                MarketRevision = revision,
                Items = source.DockedStationTrade.Items.Select(item => item.ItemTypeId == "item.water"
                    ? item with { StockQuantity = stock } : item).ToImmutableArray()
            },
            PortFees = station is null ? null : new(visitStart, visitStart + GameCalendar.DayMs, 0),
            Voyage = station is null ? new(VoyagePhases.InTransit) : new(VoyagePhases.Docked)
        };
    }

    private static void Render(TradeScreen screen)
    {
        using var bitmap = new SKBitmap(1920, 1080);
        using var canvas = new SKCanvas(bitmap);
        screen.Render(canvas, 1920, 1080);
    }

    private static void Confirm(TradeScreen screen) => screen.OnMouseDown(
        160 + TradeLayout.Confirm.MidX, 140 + TradeLayout.Confirm.MidY);

    [Fact]
    public void Local_station_requires_living_docked_player_matching_market_and_voyage()
    {
        var valid = At("A", 1, 100, 1000, 60);
        Assert.Equal("A", TradeModel.ResolveLocalStationId(valid));
        Assert.Null(TradeModel.ResolveLocalStationId(valid with { PlayerShipObjectId = "missing" }));
        Assert.Null(TradeModel.ResolveLocalStationId(valid with
        {
            Objects = valid.Objects.Select(obj => obj.ObjectId == "ship"
                ? obj with { IsDestroyed = true } : obj).ToImmutableArray()
        }));
        Assert.Null(TradeModel.ResolveLocalStationId(valid with
        {
            DockedStationTrade = valid.DockedStationTrade! with { StationObjectId = "B" }
        }));
        Assert.Null(TradeModel.ResolveLocalStationId(valid with { Voyage = new(VoyagePhases.InTransit) }));
        Assert.Null(TradeModel.ResolveLocalStationId(At(null, 2, 0, 1000, 60)));
    }

    [Fact]
    public async Task Departure_clears_market_selection_quote_and_confirm()
    {
        await using var fixture = new Fixture();
        var quote = fixture.Connection.Answer(0);
        Render(fixture.Screen);
        Assert.True(fixture.Screen.CanConfirm);
        fixture.Screen.Model.Quantity = 2;
        Render(fixture.Screen);
        Assert.Equal(2, fixture.Connection.Requests.Count);
        fixture.Buffer.Update(At(null, 2, 0, 1000, 60));
        Render(fixture.Screen);

        Assert.False(fixture.Screen.HasValidVisit);
        Assert.Empty(fixture.Screen.Model.Rows);
        Assert.Null(fixture.Screen.Model.SelectedItemId);
        Assert.Null(fixture.Screen.Model.SelectedModuleId);
        Assert.Equal(1, fixture.Screen.Model.Quantity);
        Assert.Null(fixture.Screen.Model.AuthoritativeQuote);
        Assert.Equal("NotDocked", fixture.Screen.Model.Quote.DisabledReason);
        Assert.True(fixture.Connection.Tokens[1].IsCancellationRequested);
        Assert.False(fixture.Screen.CanConfirm);
        Confirm(fixture.Screen);
        Assert.Empty(fixture.Connection.Commands);
        Assert.NotEmpty(quote.QuoteId);
    }

    [Fact]
    public async Task Destination_uses_current_cargo_credits_and_requires_new_confirmation()
    {
        await using var fixture = new Fixture();
        fixture.Buffer.Update(At(null, 2, 0, 1000, 60));
        Render(fixture.Screen);
        fixture.Buffer.Update(At("B", 3, 3000, 777, 5, stock: 19));
        var destination = new TradeScreen(fixture.Buffer, fixture.Handle);
        destination.OnActivated();
        Assert.Equal("B", destination.OpenedForStationObjectId);
        Assert.Null(destination.Model.SelectedItemId);
        Assert.Equal(1, destination.Model.Quantity);
        Assert.Equal(5, destination.Model.Cargo("item.water"));
        Assert.Equal(19, destination.Model.Rows.Single(row => row.ItemTypeId == "item.water").StockQuantity);
        Assert.False(destination.CanConfirm);
        destination.Model.Select("item.water");
        Render(destination);
        var quote = fixture.Connection.Answer(1, "B");
        Render(destination);
        Assert.Equal(760, destination.Model.Quote.BalanceAfter);
        Confirm(destination);
        var command = Assert.Single(fixture.Connection.Commands);
        Assert.Equal(("B", "item.water", 1L),
            (destination.OpenedForStationObjectId, command.ItemTypeId, command.Quantity));
        Assert.Equal(quote.QuoteId, command.QuoteId);
    }

    [Fact]
    public async Task Old_A_quote_cannot_become_current_after_A_B_A()
    {
        await using var fixture = new Fixture();
        Assert.Single(fixture.Connection.Requests);
        fixture.Buffer.Update(At(null, 2, 0, 1000, 60));
        Render(fixture.Screen);
        fixture.Buffer.Update(At("B", 3, 3000, 900, 5));
        Render(fixture.Screen);
        Assert.Empty(fixture.Screen.Model.Rows);
        fixture.Buffer.Update(At("A", 4, 4000, 800, 7));
        fixture.Connection.Answer(0);
        Render(fixture.Screen);
        Assert.False(fixture.Screen.HasValidVisit);
        Assert.Empty(fixture.Screen.Model.Rows);
        Assert.Null(fixture.Screen.Model.AuthoritativeQuote);
        Assert.Single(fixture.Connection.Requests);
        Confirm(fixture.Screen);
        Assert.Empty(fixture.Connection.Commands);

        var fresh = new TradeScreen(fixture.Buffer, fixture.Handle);
        fresh.OnActivated();
        fresh.Model.Select("item.water");
        Render(fresh);
        Assert.Equal(2, fixture.Connection.Requests.Count);
        Assert.NotEqual(fixture.Connection.Requests[0].RequestId,
            fixture.Connection.Requests[1].RequestId);

        await using var direct = new Fixture();
        direct.Buffer.Update(At("A", 2, 9000, 800, 7));
        direct.Connection.Answer(0);
        Render(direct.Screen);
        Assert.False(direct.Screen.HasValidVisit);
        Assert.Null(direct.Screen.Model.AuthoritativeQuote);
    }

    [Fact]
    public async Task Deactivated_screen_and_old_session_cannot_apply_quote_completion()
    {
        await using var old = new Fixture();
        old.Screen.OnDeactivated();
        Assert.True(old.Connection.Tokens[0].IsCancellationRequested);
        await using var replacement = new Fixture();
        old.Connection.Answer(0);
        Render(old.Screen);
        Render(replacement.Screen);
        Assert.Null(old.Screen.Model.AuthoritativeQuote);
        Assert.Null(replacement.Screen.Model.AuthoritativeQuote);
        Assert.Empty(replacement.Screen.History);
        Assert.NotSame(old.Handle, replacement.Handle);
    }

    [Fact]
    public async Task Late_origin_receipt_updates_history_once_without_replacing_destination_quote()
    {
        await using var fixture = new Fixture();
        var originQuote = fixture.Connection.Answer(0);
        Render(fixture.Screen);
        Confirm(fixture.Screen);
        var sent = Assert.Single(fixture.Connection.Commands);
        fixture.Buffer.Update(At(null, 2, 0, 1000, 61));
        Render(fixture.Screen);
        var receipt = new TradeExecutionReceipt("A", "item.water", originQuote.QuoteId, 7, 8,
            1, 1, 17, []);
        var result = new CommandResult(sent.CommandId, "ship", sent.ModuleId, sent.CommandType,
            CommandResultStatus.Executed, 3000, TradeReceipt: receipt);
        fixture.Buffer.Update(At("B", 3, 3000, 777, 61) with { CommandResults = [result] });
        var destination = new TradeScreen(fixture.Buffer, fixture.Handle);
        destination.OnActivated();
        destination.Model.Select("item.water");
        Render(destination);
        var destinationQuote = fixture.Connection.Answer(1, "B");
        Render(destination);
        Assert.Equal(destinationQuote.QuoteId, destination.Model.AuthoritativeQuote?.QuoteId);
        Assert.Equal(receipt, Assert.Single(destination.History).ConfirmedReceipt);
        fixture.Buffer.Update(At("B", 4, 3000, 777, 61) with { CommandResults = [result] });
        Render(destination);
        Assert.Single(destination.History);
        Assert.Equal(destinationQuote.QuoteId, destination.Model.AuthoritativeQuote?.QuoteId);
    }

    [Fact]
    public async Task Submit_rechecks_visit_after_snapshot_changes()
    {
        await using var fixture = new Fixture();
        fixture.Connection.Answer(0);
        Render(fixture.Screen);
        Assert.True(fixture.Screen.CanConfirm);
        fixture.Buffer.Update(At("B", 2, 3000, 777, 5));
        Confirm(fixture.Screen);
        Assert.Empty(fixture.Connection.Commands);
        Assert.False(fixture.Screen.HasValidVisit);
    }

    [Fact]
    public async Task Same_station_refresh_preserves_filters_without_starting_duplicate_requests()
    {
        await using var fixture = new Fixture();
        fixture.Screen.Model.Query = "wat";
        fixture.Screen.Model.SetFilter(TradeFilter.Goods);
        fixture.Screen.Model.SetSort(TradeSort.Price);
        Render(fixture.Screen);
        for (ulong sequence = 2; sequence <= 4; sequence++)
        {
            fixture.Buffer.Update(At("A", sequence, 100, 1000, 60));
            Render(fixture.Screen);
        }
        Assert.Equal(1, fixture.Screen.Model.VisitEpoch);
        Assert.Equal("wat", fixture.Screen.Model.Query);
        Assert.Equal(TradeFilter.Goods, fixture.Screen.Model.Filter);
        Assert.Equal(TradeSort.Price, fixture.Screen.Model.Sort);
        Assert.Single(fixture.Connection.Requests);
    }
}
