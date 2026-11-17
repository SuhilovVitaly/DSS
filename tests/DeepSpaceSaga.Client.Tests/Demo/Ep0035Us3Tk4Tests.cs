namespace DeepSpaceSaga.Client.Tests.Demo;

public sealed class Ep0035Us3Tk4Tests
{
    [Theory]
    [InlineData("Russian")]
    [InlineData("English")]
    public void EncounterWarningAndReasonsExistInBothLanguages(string language)
    {
        using var locale = Localization.UseLocaleForTests(language);
        foreach (var suffix in new[] { "Title", "Warning", "Continue", "Pending", "Refused", "encounter_not_captured",
            "sensor_report_pending", "dialogue_active", "battle_unavailable", "stale_revision", "Unavailable" })
        {
            string key = "Demo.Encounter." + suffix;
            Assert.NotEqual(key, Localization.Get(key));
        }
        Assert.DoesNotContain("{", string.Format(Localization.Get("Demo.Encounter.Refused"), "reason"));
    }
}
