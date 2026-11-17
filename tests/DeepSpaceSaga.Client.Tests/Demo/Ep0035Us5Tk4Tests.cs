namespace DeepSpaceSaga.Client.Tests.Demo;

public sealed class Ep0035Us5Tk4Tests
{
    [Theory]
    [InlineData("Russian")]
    [InlineData("English")]
    public void RouteFeedbackHasMatchingReadableTerms(string language)
    {
        using var locale = Localization.UseLocaleForTests(language);
        foreach (string suffix in new[] { "Direct", "Detour", "Terms", "Slack", "Late", "NoDeadline", "Unknown", "Exposed",
            "Lower", "Pending", "Selected", "Unavailable", "Stale", "Fuel", "Engine", "Debt", "Empty", "Failed" })
        {
            string key = "Demo.Route." + suffix;
            Assert.NotEqual(key, Localization.Get(key));
            Assert.False(string.IsNullOrWhiteSpace(string.Format(Localization.Get(key), "01:00", 25, "risk")));
        }
    }
}
