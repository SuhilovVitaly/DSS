using DeepSpaceSaga.Client.UI.Screens.Ship;
using DeepSpaceSaga.Contracts;

namespace DeepSpaceSaga.Client.Tests.Demo;

public sealed class DemoEp0033Us0003Tk0004Tests
{
    [Theory]
    [InlineData("English")]
    [InlineData("Russian")]
    public async Task OfferContentsAndInstalledEquipmentFollowConfirmedCarrier(string language)
    {
        using var locale = Localization.UseLocaleForTests(language);
        var drone = new OwnedDroneSnapshot("d", "ship", "bay", 0, "pilot", "light", "gun.sword.t1", "plate.medium",
            ShieldId: "shield.t1", ShieldCount: 1, DecoyId: "decoy.t1", EngineIds: ["drive.standard", "drive.standard"]);
        var carrier = new CarrierOperationsSnapshot("ship", "class", "Ship", 1, 10, 10, false, OwnedDrones: [drone]);
        var slot = new ReplacementSlot("bay", 1);
        var frame = new ReplacementOffer("replacement.light", "layout", false, 1500, 1,
            ["light", "plate.medium", "gun.sword.t1", "shield.t1", "decoy.t1", "drive.standard", "drive.standard"], [slot]);
        var upgrade = new ReplacementOffer("equipment.spear", "gun.spear.t1", true, 400, 1, ["gun.spear.t1"], []);
        var armor = new ReplacementOffer("equipment.armor", "plate.heavy", true, 600, 1, ["plate.heavy"], []);
        var campaign = new DemoCampaignSnapshot(1, 1, new(Replacement: new([frame, frame with { Id = "replacement.heavy", Items = frame.Items.SetItem(0, "heavy") }, upgrade, armor])));
        var screen = new ShipScreen(ShipViewModel.From(carrier), 0, null, campaign: () => campaign, carrier: () => carrier,
            service: (command, _) =>
            {
                carrier = carrier with { Revision = carrier.Revision + 1, OwnedDrones = [drone with { WeaponId = "gun.spear.t1", ArmorId = "plate.heavy" }] };
                return ValueTask.FromResult(new DemoCampaignResponse(command.CommandId, true, null, 2));
            });
        Draw(screen);
        var row = ShipLayout.WingRowLocalRect(0);
        screen.OnMouseDown(ShipLayout.PanelLeft(1920) + row.Left + 5, ShipLayout.PanelTop(1080) + row.Top + 5);
        screen.OnMouseDown(ShipLayout.PanelLeft(1920) + 1100, ShipLayout.PanelTop(1080) + 280);
        Assert.Contains(Localization.Get("Demo.Replacement.Item.gun.sword.t1"), screen.SelectedEquipmentText);
        foreach (string item in frame.Items.Distinct())
            Assert.Contains(Localization.Get("Demo.Replacement.Item." + item), ShipScreen.OfferItemsText(frame));
        Assert.Contains("×2", ShipScreen.OfferItemsText(frame));
        // Exercise the relocated fourth offer button, not only the command helper.
        screen.OnMouseDown(ShipLayout.PanelLeft(1920) + 1000, ShipLayout.PanelTop(1080) + 450 + 3 * 80);
        await screen.PendingService!;
        Draw(screen, language);
        Assert.Contains(Localization.Get("Demo.Replacement.Item.gun.spear.t1"), screen.SelectedEquipmentText);
        Assert.Contains(Localization.Get("Demo.Replacement.Item.plate.heavy"), screen.SelectedEquipmentText);
        Assert.DoesNotContain(Localization.Get("Demo.Replacement.Item.gun.sword.t1"), screen.SelectedEquipmentText);
        Assert.Equal("future.module", ShipScreen.OfferItemsText(upgrade with { Items = ["future.module"] }));
    }
    private static void Draw(ShipScreen screen, string? language = null)
    {
        using var bitmap = new SkiaSharp.SKBitmap(1920, 1080);
        using var canvas = new SkiaSharp.SKCanvas(bitmap);
        screen.Render(canvas, 1920, 1080);
        if (language is null || Environment.GetEnvironmentVariable("DSS_EP0033_CAPTURE_ROOT") is not { } root) return;
        Directory.CreateDirectory(root);
        using var image = SkiaSharp.SKImage.FromBitmap(bitmap);
        using var data = image.Encode(SkiaSharp.SKEncodedImageFormat.Png, 100);
        using var output = File.Create(Path.Combine(root, "shipyard-" + language + ".png"));
        data.SaveTo(output);
    }
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
