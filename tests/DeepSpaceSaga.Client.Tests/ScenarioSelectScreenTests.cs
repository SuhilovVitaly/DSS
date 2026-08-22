using DeepSpaceSaga.Client.UI.Screens.ScenarioSelect;

namespace DeepSpaceSaga.Client.Tests;

/// <summary>
/// The ScenarioSelect screen's background image. Open/close/PLAY/scroll behavior is
/// already covered by ScreenEventTests.cs — this only guards the asset regression,
/// mirroring FinanceScreenTests.cs/ShipScreenTests.cs/MainMenuScreenTests.cs.
/// </summary>
public class ScenarioSelectScreenTests
{
    [Fact]
    public void Background_image_is_loaded()
    {
        // Regression: the window-background-900x620.png asset must resolve at the
        // client's working directory and be registered in the .csproj with
        // CopyToOutputDirectory, or the panel silently falls back to a plain fill.
        Assert.True(ScenarioSelectScreen.HasLoadedBackground);
    }
}
