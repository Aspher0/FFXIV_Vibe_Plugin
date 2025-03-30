using System;
using System.Text.RegularExpressions;

#nullable enable
namespace FFXIV_Vibe_Plugin.Commons
{
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
            if ((double)value < (double)min)
                return min;
            return (double)value > (double)max ? max : value;
        }

        public static int ClampIntensity(int intensity, int threshold)
        {
            intensity = Helpers.ClampInt(intensity, 0, 100);
            return (int)((double)intensity / (100.0 / (double)threshold));
        }

        public static bool RegExpMatch(string text, string regexp)
        {
            bool flag = false;
            if (regexp.Trim() == "")
            {
                flag = true;
            }
            else
            {
                string pattern = "" + regexp;
                try
                {
                    if (Regex.Match(text, pattern, RegexOptions.IgnoreCase).Success)
                        flag = true;
                }
                catch (Exception ex)
                {
                    Logger.Error("Probably a wrong REGEXP for " + regexp);
                }
            }
            return flag;
        }
    }
}
