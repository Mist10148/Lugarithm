using UnityEngine;

/// <summary>
/// Single source of truth for speedometer math in both modes. The jeepney's top
/// speed is 4 world units/sec; the dashboard displays it at a ×10 scale so full
/// throttle reads as a believable "40 km/h".
/// </summary>
public static class SpeedGauge
{
    /// <summary>Top speed in world units/sec (matches JeepneyController.topSpeed
    /// and the Automation cruise ceiling).</summary>
    public const float TopSpeed = 4f;

    /// <summary>Display scale from world units/sec to the km/h readout.</summary>
    public const float KphPerUnit = 10f;

    public static float Normalize(float speed) =>
        Mathf.Clamp01(speed / TopSpeed);

    public static int ToKph(float speed) =>
        Mathf.Max(0, Mathf.RoundToInt(speed * KphPerUnit));

    public static string FormatKph(float speed) => $"{ToKph(speed)} km/h";
}
