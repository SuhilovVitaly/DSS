namespace DeepSpaceSaga.Client.Tests.Demo;

public sealed class DemoEp0035Us0002Tk0004Tests
{
    [Theory]
    [InlineData("Russian")]
    [InlineData("English")]
    public void PursuitFeedbackHasBothLocalesAndUnitPlaceholders(string language)
    {
        using var locale = Localization.UseLocaleForTests(language);
        foreach (var suffix in new[] { "Approaching", "Receding", "Captured", "Escaped", "Safe", "Lost" })
            Assert.NotEqual("Demo.Pursuit." + suffix, Localization.Get("Demo.Pursuit." + suffix));
        Assert.Contains("12", string.Format(Localization.Get("Demo.Pursuit.Telemetry"), 12, .5));
    }
}
