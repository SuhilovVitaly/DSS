using System.Text.Json;

namespace DeepSpaceSaga.Client.Tests.Demo;

public sealed class DemoEp0035Us0010Tk0004Tests
{
    [Theory]
    [InlineData("Russian")]
    [InlineData("English")]
    public void ContactTextUsesOneEstimateAndExplicitUnknownEta(string language)
    {
        using var locale = Localization.UseLocaleForTests(language);
        foreach (var suffix in new[] { "Intro", "Script", "NoEta", "Acknowledge", "Footer" })
            Assert.NotEqual("Demo.SensorReport." + suffix, Localization.Get("Demo.SensorReport." + suffix));
        string text = string.Format(Localization.Get("Demo.SensorReport.Contact"), "Lair", 90, 150, 7, Localization.Get("Demo.SensorReport.NoEta"));
        Assert.Contains("7", text);
        Assert.DoesNotContain("{", text);
        Assert.Contains("0", string.Format(Localization.Get("Demo.SensorReport.Eta"), 0));
    }
}
