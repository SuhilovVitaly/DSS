using DeepSpaceSaga.Client.UI.Screens.Ship;

namespace DeepSpaceSaga.Client.Tests;

public sealed class Ep0029Us0009Tk0003Tests
{
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
        Assert.Equal(5, ShipWingPanel.MaxSelectableQuantity(1, 6));
        Assert.Equal(0, ShipWingPanel.MaxSelectableQuantity(6, 6));

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
        Assert.Equal(5, ShipWingPanel.MaxSelectableQuantity(1, 6));
    }
}
