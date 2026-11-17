using DeepSpaceSaga.Client.UI.Screens;
using DeepSpaceSaga.Client.UI.Screens.Ship;
using DeepSpaceSaga.Client.UI.Screens.Hire;
using DeepSpaceSaga.Client.UI.Screens.Finance;
using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Engine.Content;
using SkiaSharp;

namespace DeepSpaceSaga.Client.Tests.Demo;

public sealed class DemoEp0033ServiceFlowTests
{
    [Fact]
    public async Task RealEngineServicesRenderAndCommitThroughScreens()
    {
        using var engine = EngineContentLoader.CreateEngineFromScenarioFile(Root() + "/Settings.json", Root() + "/Scenarios/Demo/scenario.json");
        var save = engine.CaptureSaveState();
        var ship = save.GameState.SpaceObjects.Single(s => s.ObjectId == save.GameState.PlayerShipObjectId);
        var drones = ship.CarrierState!.OwnedDrones!.ToArray();
        string departed = drones[0].CrewId;
        drones[0] = drones[0] with { CrewId = "", Destroyed = true, CurrentHull = 0 };
        drones[1] = drones[1] with { CurrentHull = drones[1].MaxHull - 2, DecoyCharges = 0 };
        var changed = ship with
        {
            IsDocked = true,
            DockedStationObjectId = "SPC-0002",
            FirstPortFeeGameTimeMs = 0,
            NextPortFeeDueGameTimeMs = 86400000,
            Crew = ship.Crew!.Where(c => c.CrewId != departed).ToArray(),
            CarrierState = ship.CarrierState with { OwnedDrones = drones }
        };
        engine.LoadScenario(save with
        {
            GameState = save.GameState with
            {
                PlayerTokens = 5000,
                SpaceObjects = save.GameState.SpaceObjects.Select(s => s.ObjectId == ship.ObjectId ? changed : s).ToArray()
            }
        }, isSave: true);
        ValueTask<DemoCampaignResponse> Service(DemoCampaignCommand c, CancellationToken _) => ValueTask.FromResult(engine.ExecuteDemoCampaign(c));
        DemoCampaignSnapshot? Campaign() => engine.CaptureSnapshot().DemoCampaign;
        var buffer = new SnapshotBuffer(); buffer.Update(engine.CaptureSnapshot());
        var hire = new HireScreen(buffer, Service, Campaign);
        Render(hire, "hire");
        var offer = Campaign()!.Company!.Recruitment!.Offers[0];
        hire.Hire(offer.Id); await hire.PendingRecruitment!;
        Assert.Contains(engine.CaptureSnapshot().CarrierOperations!.Crew, c => c.Id == offer.CrewId);
        var screen = new ShipScreen(ShipViewModel.From(engine.CaptureSnapshot().CarrierOperations), (int)Campaign()!.Company!.Supply!.Stock, null,
            campaign: Campaign, service: Service, carrier: () => engine.CaptureSnapshot().CarrierOperations);
        screen.RequestRepair(drones[1].DroneId); await screen.PendingService!;
        Render(screen, "repair-quote");
        screen.ConfirmRepair(); await screen.PendingService!;
        screen.RequestDecoys(drones[1].DroneId); await screen.PendingService!;
        screen.ConfirmDecoys(); await screen.PendingService!;
        Assert.Equal(1, engine.CaptureSnapshot().CarrierOperations!.OwnedDrones.Single(d => d.Id == drones[1].DroneId).DecoyCharges);
        Render(screen, "supply");
        screen.OnMouseDown(ShipLayout.PanelLeft(1920) + 1100, ShipLayout.PanelTop(1080) + 280);
        Render(screen, "shipyard");
        var replacement = Campaign()!.Company!.Replacement!.Offers.First(o => !o.Equipment);
        screen.PurchaseReplacement(replacement, replacement.CompatibleSlots[0]); await screen.PendingService!;
        string replacementId = engine.CaptureSnapshot().CarrierOperations!.OwnedDrones.Single(d => d.ReadinessReasons.Contains("no_pilot")).Id;
        hire.Assign(offer.CrewId, replacementId); await hire.PendingRecruitment!;
        Assert.Equal(offer.CrewId, engine.CaptureSnapshot().CarrierOperations!.OwnedDrones.Single(d => d.Id == replacementId).CrewId);
        buffer.Update(engine.CaptureSnapshot());
        Render(new FinanceScreen(buffer, Service, Campaign), "finance");
        Assert.Equal(3155, engine.PlayerCredits); // hire 300, hull 20, charge 25, frame 1500
    }
    private static void Render(IScreen screen, string name)
    {
        using var bitmap = new SKBitmap(1920, 1080);
        using var canvas = new SKCanvas(bitmap);
        screen.Render(canvas, 1920, 1080);
        Assert.NotEqual(SKColors.Transparent, bitmap.GetPixel(960, 540));
        string? output = Environment.GetEnvironmentVariable("DSS_EP0033_CAPTURE_ROOT");
        if (output is null) return;
        string full = Path.GetFullPath(output);
        Assert.StartsWith("D:\\DeepSpaceSaga\\artifacts\\validation\\", full, StringComparison.OrdinalIgnoreCase);
        Directory.CreateDirectory(full);
        using var image = SKImage.FromBitmap(bitmap); using var data = image.Encode(SKEncodedImageFormat.Png, 100);
        using var stream = File.Create(Path.Combine(full, name + ".png")); data.SaveTo(stream);
    }
    private static string Root([System.Runtime.CompilerServices.CallerFilePath] string file = "") =>
        Path.GetFullPath(Path.Combine(Path.GetDirectoryName(file)!, "../../../src/DeepSpaceSaga.Client"));
}

