namespace DeepSpaceSaga.Client.UI;

internal readonly record struct CombatTone(int FrequencyHz, int DurationMs)
{
    internal static CombatTone From(CombatAlertCue cue) => new(Math.Clamp(cue.FrequencyHz, 37, 32767), Math.Clamp(cue.DurationMs, 1, 250));
    internal int SampleRate => FrequencyHz < 22050 ? 44100 : 96000;

    internal short[] Generate()
    {
        int count = SampleRate * DurationMs / 1000;
        var samples = new short[count];
        int ramp = Math.Min(SampleRate * 5 / 1000, (count - 1) / 2);
        for (int i = 0; i < count; i++)
        {
            double envelope = ramp == 0 ? 0 : Math.Min(1d, Math.Min(i, count - 1 - i) / (double)ramp);
            samples[i] = (short)Math.Round(Math.Sin(2 * Math.PI * FrequencyHz * i / SampleRate) * short.MaxValue * .25 * envelope);
        }
        return samples;
    }
}
