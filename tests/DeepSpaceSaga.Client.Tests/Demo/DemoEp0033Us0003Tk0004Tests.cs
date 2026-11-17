using DeepSpaceSaga.Client.UI.Screens.Ship;
using DeepSpaceSaga.Contracts;

namespace DeepSpaceSaga.Client.Tests.Demo;

public sealed class DemoEp0033Us0003Tk0004Tests
{
    [Fact]
    public void UncrewedDroneIsNotPresentedAsReady() => Assert.NotEqual("ready", ShipWingPanel.DroneStateLabel(false, false, true));
    [Fact]
    public async Task PurchaseUsesExplicitSlotAndPendingBarrier()
    {
        var wait = new TaskCompletionSource<DemoCampaignResponse>();
        var calls = new List<DemoCampaignCommand>();
        var screen = new ShipScreen(ShipViewModel.From(null), 0, null, service: (c, _) => { calls.Add(c); return new(wait.Task); });
        var slot = new ReplacementSlot("hangar", 2);
        var offer = new ReplacementOffer("replacement.light", "layout", false, 1500, 1, [], [slot]);
        screen.PurchaseReplacement(offer, slot); screen.PurchaseReplacement(offer, slot);
        Assert.Single(calls);
        Assert.Equal(2, Assert.IsType<PurchaseDroneCommand>(calls[0]).SlotIndex);
        wait.SetResult(new("c", true, null, 1, Replacement: new([], "new-drone")));
        await screen.PendingService!;
        Assert.NotEmpty(screen.ReplacementStatus);
        screen.OnActivated(); Assert.Single(calls);
    }
    [Fact]
    public async Task EquipmentFailureIsLocalized()
    {
        var screen = new ShipScreen(ShipViewModel.From(null), 0, null, service: (c, _) =>
            ValueTask.FromResult(new DemoCampaignResponse(c.CommandId, false, "service_insufficient_funds", 0)));
        screen.EquipReplacement(new("equipment.spear", "gun.spear.t1", true, 400, 1, [], []), "d");
        await screen.PendingService!;
        Assert.DoesNotContain("service_insufficient_funds", screen.ReplacementStatus);
        Assert.NotEmpty(screen.ReplacementStatus);
    }
}
