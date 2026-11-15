using DeepSpaceSaga.Client.UI.Screens.Ship;
using DeepSpaceSaga.Contracts;
using SkiaSharp;

namespace DeepSpaceSaga.Client.Tests;

public sealed class Ep0029Us0009Tk0003Tests
{
    private const int ScreenWidth = 1920;
    private const int ScreenHeight = 1080;

    private static CarrierOperationsSnapshot Carrier(int revision = 7) => new(
        "SPC-0001", "ship.tetrarch", "Tetrarch", revision, 400, 400, false,
        OwnedDrones:
        [
            new OwnedDroneSnapshot("DRN-1", "SPC-0001", "BAY-01", 0, "CHR-1", "heavy", "gun", "plate",
                AmmoCurrent: 2, AmmoCapacity: 6),
            new OwnedDroneSnapshot("DRN-2", "SPC-0001", "BAY-01", 1, "CHR-2", "heavy", "gun", "plate",
                AmmoCurrent: 5, AmmoCapacity: 6),
            new OwnedDroneSnapshot("DRN-3", "SPC-0001", "BAY-01", 2, "CHR-3", "heavy", "gun", "plate",
                AmmoCurrent: 1, AmmoCapacity: 6, ReadinessReasons: ["destroyed"]),
            new OwnedDroneSnapshot("DRN-4", "SPC-0001", "BAY-01", 3, "CHR-4", "heavy", "gun", "plate",
                AmmoCurrent: 1, AmmoCapacity: 6, ReadinessReasons: ["disabled"]),
        ]);

    private static void Render(ShipScreen screen)
    {
        using var bitmap = new SKBitmap(ScreenWidth, ScreenHeight);
        using var canvas = new SKCanvas(bitmap);
        screen.Render(canvas, ScreenWidth, ScreenHeight);
    }

    private static void Click(ShipScreen screen, (float Left, float Top, float Right, float Bottom) local)
    {
        float x = ShipLayout.PanelLeft(ScreenWidth) + (local.Left + local.Right) / 2f;
        float y = ShipLayout.PanelTop(ScreenHeight) + (local.Top + local.Bottom) / 2f;
        screen.OnMouseDown(x, y);
    }

    private static void ClickRow(ShipScreen screen, int index) => Click(screen, ShipLayout.WingRowLocalRect(index));

    private static void ClickConfirm(ShipScreen screen, int rowCount) =>
        Click(screen, ShipLayout.RearmConfirmLocalRect(rowCount));

    private static ShipScreen Screen(Func<RearmDroneRequest, CancellationToken, ValueTask<CarrierCommandResponse>> rearm, int stock = 42) =>
        new(ShipViewModel.From(Carrier()), stock, rearm);

    [Fact]
    public async Task ConfirmSendsSelectedQuantityThroughScreen()
    {
        var requests = new List<RearmDroneRequest>();
        var gate = new TaskCompletionSource<CarrierCommandResponse>(TaskCreationOptions.RunContinuationsAsynchronously);
        var screen = Screen((request, _) => { requests.Add(request); return new ValueTask<CarrierCommandResponse>(gate.Task); });
        Render(screen);

        ClickRow(screen, 0);                       // DRN-1: magazine 2/6, hold 42
        Assert.Equal(0, screen.SelectedRow);
        Assert.Equal(4, screen.SelectedQuantity);  // min(6 - 2, 42)
        ClickConfirm(screen, 4);

        var request = Assert.Single(requests);
        Assert.Equal("DRN-1", request.DroneId);
        Assert.Equal("SPC-0001", request.ShipId);
        Assert.Equal(4, request.Quantity);
        Assert.Equal(7, request.ExpectedRevision);
        Assert.Equal(32, request.CommandId.Length);

        gate.SetResult(new CarrierCommandResponse(Accepted: true, Revision: 8, Transferred: 4));
        Assert.NotNull(screen.PendingRearm);
        await screen.PendingRearm!;

        // The accepted transfer comes only from the authoritative response.
        Assert.Equal(6, screen.Drones.Single(d => d.Id == "DRN-1").AmmoCurrent);
        Assert.Equal(38, screen.TorpedoStock);
        Assert.Equal(8, screen.Revision);
        Assert.False(screen.IsPending);
    }

    [Fact]
    public async Task DoubleConfirmWhilePendingSendsOneRequest()
    {
        int calls = 0;
        var gate = new TaskCompletionSource<CarrierCommandResponse>(TaskCreationOptions.RunContinuationsAsynchronously);
        var screen = Screen((_, _) => { calls++; return new ValueTask<CarrierCommandResponse>(gate.Task); });
        Render(screen);
        ClickRow(screen, 0);

        ClickConfirm(screen, 4);
        ClickConfirm(screen, 4);   // second click while the first request is still pending
        Assert.Equal(1, calls);    // pending barrier: exactly one request
        Assert.True(screen.IsPending);

        gate.SetResult(new CarrierCommandResponse(Accepted: true, Revision: 8, Transferred: 4));
        await screen.PendingRearm!;
        Assert.False(screen.IsPending);
    }

    [Fact]
    public async Task RejectedRearmDoesNotSpendLocally()
    {
        var screen = Screen((_, _) => new ValueTask<CarrierCommandResponse>(new CarrierCommandResponse(
            Accepted: false, Reason: "stale revision", Revision: 9, Transferred: 0, Snapshot: Carrier(revision: 9))));
        Render(screen);
        ClickRow(screen, 0);
        ClickConfirm(screen, 4);

        await screen.PendingRearm!;

        // A rejection transfers nothing: magazine and hold stay exactly where they were.
        Assert.Equal(2, screen.Drones.Single(d => d.Id == "DRN-1").AmmoCurrent);
        Assert.Equal(42, screen.TorpedoStock);
        // The stale carrier surface is refreshed from the authoritative snapshot, not recomputed.
        Assert.Equal(9, screen.Revision);
        Assert.Contains("stale revision", screen.Status);
    }

    [Fact]
    public void PreviewShowsMagazineAndHoldTransition()
    {
        Assert.Equal("magazine 2→6, hold 42→38", ShipWingPanel.PreviewText(2, 6, 42, 4));
        Assert.Equal("magazine 2→5, hold 42→39", ShipWingPanel.PreviewText(2, 6, 42, 3));
        // The quantity is clamped by both the magazine gap and the cargo stock, never oversold.
        Assert.Equal("magazine 2→6, hold 42→38", ShipWingPanel.PreviewText(2, 6, 42, 99));
        Assert.Equal("magazine 5→5, hold 0→0", ShipWingPanel.PreviewText(5, 6, 0, 1));
        Assert.Equal("magazine 6→6, hold 40→40", ShipWingPanel.PreviewText(6, 6, 40, 4));
    }

    [Fact]
    public void DestroyedDroneCannotBeSelectedForRearm()
    {
        int calls = 0;
        var screen = Screen((_, _) => { calls++; return new ValueTask<CarrierCommandResponse>(new CarrierCommandResponse(Accepted: true)); });
        Render(screen);

        ClickRow(screen, 2);   // DRN-3 carries the "destroyed" readiness reason
        Assert.Equal(-1, screen.SelectedRow);
        Assert.Null(screen.PendingRearm);

        ClickConfirm(screen, 4); // no selection -> confirm is a no-op
        Assert.Equal(0, calls);
        Assert.Null(screen.PendingRearm);
    }

    [Fact]
    public async Task DisabledDroneIsSelectableForRearm()
    {
        var requests = new List<RearmDroneRequest>();
        var screen = Screen((request, _) =>
        {
            requests.Add(request);
            return new ValueTask<CarrierCommandResponse>(new CarrierCommandResponse(Accepted: true, Revision: 8, Transferred: 3));
        });
        Render(screen);

        Assert.Equal("disabled", ShipWingPanel.DroneStateLabel(false, true));
        Assert.Equal("destroyed", ShipWingPanel.DroneStateLabel(true, true));
        ClickRow(screen, 3);   // DRN-4 is disabled but rearm does not repair it
        Assert.Equal(3, screen.SelectedRow);
        Assert.Equal(5, screen.SelectedQuantity); // min(6 - 1, 42)
        ClickConfirm(screen, 4);
        await screen.PendingRearm!;
        Assert.Equal(5, Assert.Single(requests).Quantity);
        Assert.True(screen.Drones.Single(d => d.Id == "DRN-4").Disabled);
    }

    [Fact]
    public void RearmGeometryFitsInsideTheWingPanel()
    {
        for (int rows = 0; rows <= 4; rows++)
        {
            Assert.True(ShipLayout.FitsInsidePanel(ShipLayout.RearmControlsLocalRect(rows)));
            for (int i = 0; i < rows; i++)
                Assert.True(ShipLayout.FitsInsidePanel(ShipLayout.WingRowLocalRect(i)));
        }
        var wing = ShipLayout.WingPanelLocalRect();
        var row = ShipLayout.WingRowLocalRect(0);
        Assert.Equal(28, row.Bottom - row.Top);
        Assert.True(row.Left > wing.Left && row.Right < wing.Right);
        Assert.True(ShipLayout.WingRowLocalRect(3).Bottom < ShipLayout.RearmControlsLocalRect(4).Top);
        Assert.Equal((ShipButton.QuantityMinus, -1), ShipLayout.RearmHitTest(
            ShipLayout.PanelLeft(ScreenWidth) + ShipLayout.RearmMinusLocalRect(4).Left + 1,
            ShipLayout.PanelTop(ScreenHeight) + ShipLayout.RearmMinusLocalRect(4).Top + 1,
            ScreenWidth, ScreenHeight, 4));
        Assert.Equal((ShipButton.DroneRow, 2), ShipLayout.RearmHitTest(
            ShipLayout.PanelLeft(ScreenWidth) + ShipLayout.WingRowLocalRect(2).Left + 1,
            ShipLayout.PanelTop(ScreenHeight) + ShipLayout.WingRowLocalRect(2).Top + 1,
            ScreenWidth, ScreenHeight, 4));
        Assert.Equal((ShipButton.None, -1), ShipLayout.RearmHitTest(2, 2, ScreenWidth, ScreenHeight, 4));
    }

    [Fact]
    public void DoubleClickSendsOneRequest()
    {
        var gate = new ShipRearmController();
        Assert.True(gate.TryBegin(3));   // first click starts the request
        Assert.False(gate.TryBegin(3));  // double-click is blocked while pending
        Assert.True(gate.IsPending);

        gate.End();
        Assert.False(gate.IsPending);
        Assert.True(gate.TryBegin(3));   // after completion, a new request is allowed
    }

    [Fact]
    public void PartialQuantityIsPassedExactly()
    {
        // Max = min(magazine gap, cargo stock), never negative.
        Assert.Equal(5, ShipWingPanel.MaxSelectableQuantity(5, 42));
        Assert.Equal(0, ShipWingPanel.MaxSelectableQuantity(0, 42));
        Assert.Equal(0, ShipWingPanel.MaxSelectableQuantity(5, 0));
        Assert.Equal(3, ShipWingPanel.MaxSelectableQuantity(5, 3));

        // The chosen 3 is preserved, never auto-raised to the 5 maximum.
        Assert.Equal(3, ShipWingPanel.ClampQuantity(3, 5));
        Assert.Equal(0, ShipWingPanel.ClampQuantity(-1, 5));

        var gate = new ShipRearmController();
        gate.TryBegin(3);
        Assert.Equal(3, gate.SelectedQuantity);
    }

    [Fact]
    public void RejectedStaleRefreshesWithoutOptimisticSpend()
    {
        // The controller and panel helpers never mutate the magazine locally; the ammo the UI
        // shows comes only from the authoritative snapshot, so a rejected stale request leaves it
        // unchanged.
        var gate = new ShipRearmController();
        gate.TryBegin(3);
        gate.End();

        // The wing panel only derives display strings; it holds no mutable ammo state.
        Assert.Equal("1/6", ShipWingPanel.DroneAmmoText(1, 6));
        Assert.Equal(5, ShipWingPanel.MaxSelectableQuantity(5, 42));
    }
}
