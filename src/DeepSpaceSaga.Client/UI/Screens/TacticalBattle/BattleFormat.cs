using System.Globalization;
using DeepSpaceSaga.Client.UI.Controls;
using DeepSpaceSaga.Contracts;

namespace DeepSpaceSaga.Client.UI.Screens.TacticalBattle;

/// <summary>Display only: never feeds rounded values back into Engine orders.</summary>
public static class BattleFormat
{
    public static string Text(string text) => ControlText.SafeSymbols(text);
    public static string Localize(string key) => Text(Localization.Get("TacticalBattle." + key));
    public static string Number(double value, int decimals = 0)
    {
        if (decimals < 0 || decimals > 15) throw new ArgumentOutOfRangeException(nameof(decimals));
        if (!double.IsFinite(value)) return Text("—");
        double rounded = Math.Round(value, decimals, MidpointRounding.ToEven);
        if (rounded == 0) rounded = 0; // normalize negative zero for display
        string format = decimals == 0 ? "0" : "0." + new string('0', decimals);
        return Text(rounded.ToString(format, CultureInfo.InvariantCulture).Replace('-', '−'));
    }
    public static string Signed(double value, int decimals = 0)
    {
        string number = Number(value, decimals);
        return double.IsFinite(value) && Math.Round(value, decimals, MidpointRounding.ToEven) > 0 ? "+" + number : number;
    }
    /// <summary>Format an already scaled percentage-point value.</summary>
    public static string Percent(double value, int decimals = 0) => Number(value, decimals) + "%";
    /// <summary>Engine hit/rear-kill chances and scenario goals are zero-to-one probabilities.</summary>
    public static string Probability(double value, int decimals = 0) => Percent(value * 100.0, decimals);
    internal static string HitFactors(TacticalHitFactors factors)
    {
        (string Key, double Value)[] terms =
        [
            ("Base", factors.Base), ("Accuracy", factors.Accuracy), ("Maneuver", factors.Maneuver),
            ("Weapon", factors.Weapon), ("Range", factors.Range), ("Angle", factors.Angle),
            ("Rear", factors.Rear), ("Shield", factors.Shield), ("Evasion", factors.Evasion),
            ("Clamp", factors.Clamp), ("Decoy", factors.Decoy)
        ];
        var labels = terms.Where(term => term.Value != 0).Select(term =>
            Localize("Factors." + term.Key) + " " + Signed(term.Value, 1)).ToList();
        if (factors.Absorption > 0)
            labels.Add(Localize("Factors.Absorption") + " " + Probability(factors.Absorption, 1));
        return string.Join(" · ", labels);
    }
    public static string Degrees(double value, int decimals = 0) => Number(value, decimals) + Text("°");
    public static string Seed(long seed) => Text(seed.ToString(CultureInfo.InvariantCulture).Replace('-', '−'));
}
