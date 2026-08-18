using System.Globalization;

namespace Ixen.Core.Visual.Styles
{
    internal static class StyleDuration
    {
        internal static bool TryParse(string value, out int milliseconds)
        {
            milliseconds = 0;

            if (string.IsNullOrEmpty(value))
            {
                return false;
            }

            string trimmed = value.ToLower();

            if (trimmed.EndsWith("ms"))
            {
                trimmed = trimmed.Substring(0, trimmed.Length - 2);
            }
            else if (trimmed.EndsWith("s"))
            {
                if (!float.TryParse(trimmed.Substring(0, trimmed.Length - 1),
                    NumberStyles.Float, CultureInfo.InvariantCulture, out float seconds))
                {
                    return false;
                }

                milliseconds = (int)(seconds * 1000);
                return milliseconds > 0;
            }

            if (!int.TryParse(trimmed, NumberStyles.Integer, CultureInfo.InvariantCulture, out milliseconds))
            {
                return false;
            }

            return milliseconds > 0;
        }
    }
}
