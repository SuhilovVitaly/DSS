using DeepSpaceSaga.Contracts;
using SkiaSharp;

namespace DeepSpaceSaga.Client.UI.Screens.Ship;

public sealed partial class ShipScreen
{
    internal static string SensorCrewLabel(ShipViewModel.CrewRow member) =>
        $"{member.DisplayName} ({member.Role})" + (member.IsSensorOperator ? $" — {Localization.Get("Ship.Sensor.Operator")}" : "");

    internal static string SensorAvailabilityLabel(SensorOperatorSnapshot sensor) => sensor.AvailabilityReason switch
    {
        SensorOperatorReasons.Unassigned => Localization.Get("Ship.Sensor.Script") + ": " + Localization.Get("Ship.Sensor.Unassigned"),
        SensorOperatorReasons.CrewMissing => Localization.Get("Ship.Sensor.Script") + ": " + Localization.Get("Ship.Sensor.CrewMissing"),
        SensorOperatorReasons.ModuleUnavailable => Localization.Get("Ship.Sensor.ModuleUnavailable"),
        _ => ""
    };

    private void DrawSensorOperator(SKCanvas canvas, float x, float y, SKPaint paint)
    {
        if (_model.SensorOperator is not { } sensor) return;
        canvas.DrawText(string.Format(Localization.Get("Ship.Sensor.Range"),
            sensor.EffectiveRadiusWorldUnits / 10, sensor.BaseRadiusWorldUnits / 10), x, y, paint);
        if (sensor.AvailabilityReason is not null)
            canvas.DrawText(SensorAvailabilityLabel(sensor), x, y + ShipLayout.BodyLineHeight, paint);
    }
}
