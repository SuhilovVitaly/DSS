using DeepSpaceSaga.Client.UI.Screens.MainMenu;

namespace DeepSpaceSaga.Client.Tests;

/// <summary>
/// The MainMenu screen's background image. Open/close/hover behavior is already
/// covered by ScreenEventTests.cs — this only guards the asset regression, mirroring
/// FinanceScreenTests.cs/ShipScreenTests.cs.
/// </summary>
public class MainMenuScreenTests
{
    [Fact]
    public void Background_image_is_loaded()
    {
        // Regression: the window-background-500x550.png asset must resolve at the
        // client's working directory and be registered in the .csproj with
        // CopyToOutputDirectory, or the panel silently falls back to a plain fill.
        Assert.True(MainMenuScreen.HasLoadedBackground);
    }
}
