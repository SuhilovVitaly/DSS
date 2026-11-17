using DeepSpaceSaga.Contracts;
using DeepSpaceSaga.Engine.Scenario;

namespace DeepSpaceSaga.Engine.Tests.Demo;

public sealed class DemoEp0033Us0003Tk0003Tests
{
    private static (string Id, string Hangar, int Slot) Vacate(SimulationEngine engine, long credits = 5000)
    {
        string id = DemoEp0033Us0001Tk0003Tests.Damage(engine, credits);
        var save = engine.CaptureSaveState();
        var ship = save.GameState.SpaceObjects.Single(s => s.ObjectId == save.GameState.PlayerShipObjectId);
        var drone = ship.CarrierState!.OwnedDrones!.First(d => d.DroneId == id);
        var drones = ship.CarrierState.OwnedDrones.Select(d => d.DroneId == id ? d with { Destroyed = true, CurrentHull = 0, CrewId = "" } : d).ToArray();
        engine.LoadScenario(save with { GameState = save.GameState with { SpaceObjects = save.GameState.SpaceObjects.Select(s =>
            s.ObjectId == ship.ObjectId ? s with { CarrierState = ship.CarrierState with { OwnedDrones = drones } } : s).ToArray() } }, isSave: true);
        return (id, drone.HangarModuleId, drone.SlotIndex);
    }
    [Fact]
    public void ReplacementPreservesTombstoneAndHasNoPilotOrAmmunition()
    {
        using var engine = DemoEp0033Us0001Tk0003Tests.Demo();
        var slot = Vacate(engine);
        var command = new PurchaseDroneCommand("buy", 0, "replacement.light", slot.Hangar, slot.Slot);
        var response = engine.ExecuteDemoCampaign(command);
        Assert.True(response.Applied, response.Reason);
        var id = response.Replacement!.LastDroneId!;
        Assert.NotEqual(slot.Id, id);
        var wing = engine.CaptureSnapshot().CarrierOperations!.OwnedDrones;
        Assert.True(wing.Single(d => d.Id == slot.Id).Destroyed);
        var drone = wing.Single(d => d.Id == id);
        Assert.Equal("", drone.CrewId);
        Assert.Equal(0, drone.AmmoCurrent);
        Assert.Equal(0, drone.DecoyCharges);
        Assert.Equal(3500, engine.PlayerCredits);
        var serialized = ScenarioLoader.Serialize(engine.CaptureSaveState());
        engine.LoadScenario(ScenarioLoader.LoadFromJson(serialized, true, true), isSave: true);
        Assert.Equal(response, engine.ExecuteDemoCampaign(command));
        Assert.Equal(3500, engine.PlayerCredits);
        Assert.Equal(id, engine.CaptureSnapshot().CarrierOperations!.Hangars.Single(h => h.HangarModuleId == slot.Hangar && h.SlotIndex == slot.Slot).DroneId);
    }
    [Fact]
    public void OccupiedSlotAndInsufficientFundsDoNotPurchase()
    {
        using var engine = DemoEp0033Us0001Tk0003Tests.Demo();
        DemoEp0033Us0001Tk0003Tests.Damage(engine, 5000);
        var slot = engine.CaptureSnapshot().CarrierOperations!.Hangars.First(h => h.DroneId is not null);
        Assert.False(engine.ExecuteDemoCampaign(new PurchaseDroneCommand("occupied", 0, "replacement.light", slot.HangarModuleId, slot.SlotIndex)).Applied);
        Assert.Equal(5000, engine.PlayerCredits);
        var empty = Vacate(engine, 1);
        Assert.Equal(CompanyServiceReasons.InsufficientFunds, engine.ExecuteDemoCampaign(new PurchaseDroneCommand("poor", 0, "replacement.light", empty.Hangar, empty.Slot)).Reason);
        Assert.Equal(1, engine.PlayerCredits);
    }
    [Fact]
    public void EquipmentUsesActualModuleAndKeepsAmmo()
    {
        using var engine = DemoEp0033Us0001Tk0003Tests.Demo();
        string id = DemoEp0033Us0001Tk0003Tests.Damage(engine, 5000);
        var before = engine.CaptureSnapshot().CarrierOperations!.OwnedDrones.Single(d => d.Id == id);
        var result = engine.ExecuteDemoCampaign(new EquipDroneCommand("equip", 0, id, "equipment.spear"));
        Assert.True(result.Applied, result.Reason);
        var after = engine.CaptureSnapshot().CarrierOperations!.OwnedDrones.Single(d => d.Id == id);
        Assert.Equal("gun.spear.t1", after.WeaponId);
        Assert.Equal(before.AmmoCurrent, after.AmmoCurrent);
        Assert.Equal(before.DecoyCharges, after.DecoyCharges);
        Assert.Equal(4600, engine.PlayerCredits);
    }
}
