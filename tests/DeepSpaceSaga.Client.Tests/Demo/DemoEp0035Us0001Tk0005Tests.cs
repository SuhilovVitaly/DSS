namespace DeepSpaceSaga.Client.Tests.Demo;

public sealed class DemoEp0035Us0001Tk0005Tests
{
    [Theory]
    [InlineData("Russian")]
    [InlineData("English")]
    public void ObservationLabels_AreLocalizedAndHaveValidPlaceholders(string language)
    {
        using var locale = Localization.UseLocaleForTests(language);
        foreach (string suffix in new[] { "NoContacts", "Unknown", "Observed", "DebugVisible", "Captured", "Lair", "Tancred" })
        {
            string key = "Demo.Pirate." + suffix;
            Assert.NotEqual(key, Localization.Get(key));
            Assert.False(string.IsNullOrWhiteSpace(Localization.Get(key)));
        }
        Assert.Contains("7", string.Format(Localization.Get("Demo.Pirate.ApproximateDrones"), 7));
    }
}
