using Ixen.Core.Visual.Styles.Descriptors;
using System.Globalization;
using System.Text.RegularExpressions;

namespace Ixen.Core.Visual.Styles.Parsers
{
    internal static class GradientParser
    {
        internal const string LINEAR = "linear-gradient";
        internal const string RADIAL = "radial-gradient";

        private static Regex _color = new Regex(@"^#(?:[0-9A-Fa-f]{8}|[0-9A-Fa-f]{6})$");
        private static Regex _offset = new Regex(@"^([0-9]+(?:\.[0-9]+)?)%$");
        private static Regex _angle = new Regex(@"^(-?[0-9]+(?:\.[0-9]+)?)deg$");

        internal static bool IsCall(string value)
            => Head(value) != null;

        private static string Head(string value)
        {
            if (value == null || !value.EndsWith(")"))
            {
                return null;
            }

            if (value.StartsWith(LINEAR + "("))
            {
                return LINEAR;
            }

            return value.StartsWith(RADIAL + "(") ? RADIAL : null;
        }

        internal static Gradient Parse(string value)
        {
            string head = Head(value);

            if (head == null)
            {
                return null;
            }

            string body = value.Substring(head.Length + 1, value.Length - head.Length - 2);

            var gradient = new Gradient
            {
                Kind = head == RADIAL ? GradientKind.Radial : GradientKind.Linear
            };

            string[] parts = body.Split(new[] { ' ', '\t' }, System.StringSplitOptions.RemoveEmptyEntries);

            bool directionSeen = false;
            bool towards = false;
            float horizontal = 0;
            float vertical = 0;

            foreach (string part in parts)
            {
                if (_color.IsMatch(part))
                {
                    gradient.Stops.Add(new GradientStop { Color = part });
                    continue;
                }

                Match offset = _offset.Match(part);

                if (offset.Success)
                {
                    if (gradient.Stops.Count == 0)
                    {
                        return null;
                    }

                    GradientStop last = gradient.Stops[gradient.Stops.Count - 1];

                    if (last.HasOffset || !float.TryParse(offset.Groups[1].Value, NumberStyles.Float,
                        CultureInfo.InvariantCulture, out float value2) || value2 > 100)
                    {
                        return null;
                    }

                    last.Offset = value2 / 100f;
                    continue;
                }

                Match angle = _angle.Match(part);

                if (angle.Success)
                {
                    if (directionSeen || towards || gradient.Kind == GradientKind.Radial
                        || !float.TryParse(angle.Groups[1].Value, NumberStyles.Float,
                            CultureInfo.InvariantCulture, out float degrees))
                    {
                        return null;
                    }

                    gradient.Angle = degrees;
                    directionSeen = true;
                    continue;
                }

                switch (part.ToLower())
                {
                    case "to":
                        if (towards || directionSeen || gradient.Kind == GradientKind.Radial)
                        {
                            return null;
                        }

                        towards = true;
                        continue;

                    case "top":
                        vertical = -1;
                        break;

                    case "bottom":
                        vertical = 1;
                        break;

                    case "left":
                        horizontal = -1;
                        break;

                    case "right":
                        horizontal = 1;
                        break;

                    default:
                        return null;
                }

                if (!towards || gradient.Stops.Count > 0)
                {
                    return null;
                }
            }

            if (towards)
            {
                if (horizontal == 0 && vertical == 0)
                {
                    return null;
                }

                gradient.Angle = AngleOf(horizontal, vertical);
                directionSeen = true;
            }

            return gradient.Stops.Count >= 2 && Ordered(gradient) ? gradient : null;
        }

        private static float AngleOf(float horizontal, float vertical)
        {
            if (horizontal == 0)
            {
                return vertical < 0 ? 0 : 180;
            }

            if (vertical == 0)
            {
                return horizontal > 0 ? 90 : 270;
            }

            if (vertical < 0)
            {
                return horizontal > 0 ? 45 : 315;
            }

            return horizontal > 0 ? 135 : 225;
        }

        private static bool Ordered(Gradient gradient)
        {
            float previous = -1;

            foreach (GradientStop stop in gradient.Stops)
            {
                if (!stop.HasOffset)
                {
                    continue;
                }

                if (stop.Offset < previous)
                {
                    return false;
                }

                previous = stop.Offset;
            }

            return true;
        }
    }
}
