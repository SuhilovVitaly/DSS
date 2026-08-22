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

    [Fact]
    public void Content_panel_image_is_loaded()
    {
        // Regression: the micro-panel.png asset (nine-sliced by NinePatch — see
        // NinePatchTests.cs) must resolve at the client's working directory and be
        // registered in the .csproj with CopyToOutputDirectory, or the scenario list's
        // content panel silently falls back to a plain fill.
        Assert.True(ScenarioSelectScreen.HasLoadedContentPanel);
    }

    [Fact]
    public void Button_image_is_loaded()
    {
        // Regression: the button.png asset (nine-sliced by NinePatch for BACK/PLAY — see
        // NinePatchTests.cs) must resolve at the client's working directory and be
        // registered in the .csproj with CopyToOutputDirectory, or the buttons silently
        // fall back to MenuStyle's flat style.
        Assert.True(ScenarioSelectScreen.HasLoadedButtonImage);
    }
}
