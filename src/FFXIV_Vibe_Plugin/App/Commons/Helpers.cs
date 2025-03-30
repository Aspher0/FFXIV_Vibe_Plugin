using System;
using System.Text.RegularExpressions;

namespace FFXIV_Vibe_Plugin.Commons;

internal class Helpers
{
    public static int GetUnix() => (int)DateTimeOffset.Now.ToUnixTimeMilliseconds();

    public static int ClampInt(int value, int min, int max)
    {
        if (value < min)
            return min;

        return value > max ? max : value;
    }

    public static float ClampFloat(float value, float min, float max)
    {
        if (value < min)
            return min;

        return value > max ? max : value;
    }

    public static int ClampIntensity(int intensity, int threshold)
    {
        intensity = ClampInt(intensity, 0, 100);

        return (int)(intensity / (100.0 / threshold));
    }

    public static bool RegExpMatch(string text, string regexp)
    {
        if (regexp.Trim() == "")
            return true;

        string pattern = "" + regexp;

        try
        {
            if (Regex.Match(text, pattern, RegexOptions.IgnoreCase).Success)
                return true;
        }
        catch (Exception ex)
        {
            Logger.Error("Probably a wrong REGEXP for " + regexp);
        }

        return false;
    }
}
