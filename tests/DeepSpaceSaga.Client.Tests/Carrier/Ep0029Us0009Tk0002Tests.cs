using System.Collections.Immutable;
using DeepSpaceSaga.Client.UI.Screens.Ship;
using DeepSpaceSaga.Contracts;

namespace DeepSpaceSaga.Client.Tests;

public sealed class Ep0029Us0009Tk0002Tests
{
    [Fact]
    public void LayoutAt1280And1920KeepsControlsInside()
    {
        // Wing and crew panel rects must stay inside the fixed 1400×800 panel.
        Assert.True(ShipLayout.FitsInsidePanel(ShipLayout.WingPanelLocalRect()));
        Assert.True(ShipLayout.FitsInsidePanel(ShipLayout.CrewPanelLocalRect()));

        // The panel is centered at both common resolutions.
        Assert.Equal((1280 - ShipLayout.PanelWidth) / 2f, ShipLayout.PanelLeft(1280));
        Assert.Equal((720 - ShipLayout.PanelHeight) / 2f, ShipLayout.PanelTop(720));
        Assert.Equal((1920 - ShipLayout.PanelWidth) / 2f, ShipLayout.PanelLeft(1920));
        Assert.Equal((1080 - ShipLayout.PanelHeight) / 2f, ShipLayout.PanelTop(1080));
    }

    [Fact]
    public void FourPortraitsAndOfficerAreDistinct()
    {
        var crew = new[]
        {
            new CrewMemberSnapshot("CHR-PILOT-1", "Irina", CrewGender.Female, PortraitAssetId: "crew-f01", Role: CrewRole.Pilot),
            new CrewMemberSnapshot("CHR-PILOT-2", "Evelyn", CrewGender.Female, PortraitAssetId: "crew-f02", Role: CrewRole.Pilot),
            new CrewMemberSnapshot("CHR-PILOT-3", "Audrey", CrewGender.Female, PortraitAssetId: "crew-f03", Role: CrewRole.Pilot),
            new CrewMemberSnapshot("CHR-PILOT-4", "Claire", CrewGender.Female, PortraitAssetId: "crew-f04", Role: CrewRole.Pilot),
            new CrewMemberSnapshot("CHR-0001", "Captain", CrewGender.Female, PortraitAssetId: "crew-f05", Role: CrewRole.Captain),
        };
        var portraits = ShipCrewPanel.PortraitAssetIds(crew);
        Assert.Equal(5, portraits.Count);
        Assert.All(portraits, p => Assert.NotNull(p));
        Assert.Equal(5, portraits.Distinct(StringComparer.Ordinal).Count());
    }

    [Fact]
    public void DestroyedDroneNotShownReady()
    {
        var destroyed = new OwnedDroneSnapshot("DRN-1", "SPC-0001", "BAY-01", 0, "CHR-PILOT-1",
            "heavy.sword.shield", "gun.sword.t1", "plate.medium", ReadinessReasons: ["destroyed"]);
        var ready = new OwnedDroneSnapshot("DRN-2", "SPC-0001", "BAY-01", 1, "CHR-PILOT-2",
            "heavy.sword.shield", "gun.sword.t1", "plate.medium");

        Assert.Equal("destroyed", ShipWingPanel.DroneCardState(destroyed));
        Assert.Equal("ready", ShipWingPanel.DroneCardState(ready));
        Assert.Equal("destroyed", ShipWingPanel.DroneStateLabel(true));
        Assert.Equal("ready", ShipWingPanel.DroneStateLabel(false));
    }
}
