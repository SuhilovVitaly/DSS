using System.Globalization;
using DeepSpaceSaga.Client.UI.Controls;

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
    /// <summary>Chance values already use percentage units, not a zero-to-one fraction.</summary>
    public static string Percent(double value, int decimals = 0) => Number(value, decimals) + "%";
    public static string Degrees(double value, int decimals = 0) => Number(value, decimals) + Text("°");
    public static string Seed(long seed) => Text(seed.ToString(CultureInfo.InvariantCulture).Replace('-', '−'));
}
