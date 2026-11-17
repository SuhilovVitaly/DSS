using System.Collections.Immutable;
using DeepSpaceSaga.Client.UI.Screens.Ship;
using DeepSpaceSaga.Contracts;

namespace DeepSpaceSaga.Client.Tests.Demo;

public sealed class DemoEp0035Us0009Tk0004Tests
{
    [Fact]
    public void CrewView_ShowsAssignedRealOfficer_AndClearsOnLoad()
    {
        var carrier = new CarrierOperationsSnapshot("ship", "class", "Ship", 1, 100, 100, false,
            Crew: ImmutableArray.Create(new CrewMemberSnapshot("irina", "Irina", CrewGender.Female)));
        var sensor = new SensorOperatorSnapshot("sensor", "irina", null, 2400, 2640, 240);
        var model = ShipViewModel.From(carrier, sensor);
        Assert.True(Assert.Single(model.Crew).IsSensorOperator);
        Assert.Contains("Irina", ShipScreen.SensorCrewLabel(model.Crew[0]));
        Assert.Equal(2640, model.SensorOperator!.EffectiveRadiusWorldUnits);
        var loaded = ShipViewModel.From(carrier, sensor with { CrewId = null, AvailabilityReason = SensorOperatorReasons.Unassigned });
        Assert.False(Assert.Single(loaded.Crew).IsSensorOperator);
        Assert.False(string.IsNullOrWhiteSpace(ShipScreen.SensorAvailabilityLabel(loaded.SensorOperator!)));
        Assert.Null(ShipViewModel.From(carrier).SensorOperator);
    }
}
