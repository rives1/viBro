using System;
using System.Globalization;

namespace ViBro.Util;

/// <summary>
/// Times in one format everywhere (1.13.0): "0:05", "1:20", "15:02", "1:02:15" — hours only when
/// needed, no useless leading zeros (before: "00:01:20" on the bar, "1:20" in the gallery).
/// </summary>
internal static class TimeFormat
{
    public static string Format(double seconds)
    {
        if (double.IsNaN(seconds) || double.IsInfinity(seconds) || seconds < 0) seconds = 0;
        var t = TimeSpan.FromSeconds(Math.Floor(seconds));
        var c = CultureInfo.InvariantCulture;
        return t.TotalHours >= 1
            ? string.Format(c, "{0}:{1:00}:{2:00}", (int)t.TotalHours, t.Minutes, t.Seconds)
            : string.Format(c, "{0}:{1:00}", t.Minutes, t.Seconds);
    }
}
