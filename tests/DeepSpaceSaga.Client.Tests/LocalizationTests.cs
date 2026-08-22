using DeepSpaceSaga.Client;

namespace DeepSpaceSaga.Client.Tests;

/// <summary>
/// Guards the Data\Locale\*.json asset pipeline used by MainMenuScreen's button labels
/// (must resolve at the client's working directory and be registered in the .csproj with
/// CopyToOutputDirectory, mirroring MainMenuScreenTests.Background_image_is_loaded) and
/// the Localization.Get key/value contract.
/// </summary>
public class LocalizationTests
{
    [Theory]
    [InlineData("English")]
    [InlineData("Russian")]
    public void Locale_file_defines_all_MainMenu_keys(string language)
    {
        var strings = Localization.LoadLocaleFile(language);

        Assert.NotNull(strings);
        Assert.True(strings!.ContainsKey("MainMenu.NewGame"));
        Assert.True(strings.ContainsKey("MainMenu.Load"));
        Assert.True(strings.ContainsKey("MainMenu.Settings"));
        Assert.True(strings.ContainsKey("MainMenu.Exit"));
    }

    [Fact]
    public void Get_falls_back_to_the_key_itself_when_missing()
    {
        Assert.Equal("MainMenu.DoesNotExist", Localization.Get("MainMenu.DoesNotExist"));
    }
}
