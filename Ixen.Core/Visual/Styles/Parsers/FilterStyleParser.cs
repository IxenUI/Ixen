using Ixen.Core.Visual.Styles.Descriptors;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace Ixen.Core.Visual.Styles.Parsers
{
    internal class FilterStyleParser : StyleParser
    {
        internal const string NONE = "none";
        internal const string BLUR = "blur";
        internal const string GRAYSCALE = "grayscale";
        internal const string SEPIA = "sepia";
        internal const string SATURATE = "saturate";
        internal const string INVERT = "invert";
        internal const string BRIGHTNESS = "brightness";
        internal const string CONTRAST = "contrast";
        internal const string HUE_ROTATE = "hue-rotate";
        internal const string OPACITY = "opacity";

        private static Regex _length = new Regex(@"^([0-9]+(?:\.[0-9]+)?)(px|)$");
        private static Regex _amount = new Regex(@"^([0-9]+(?:\.[0-9]+)?)(%|)$");
        private static Regex _angle = new Regex(@"^(-?[0-9]+(?:\.[0-9]+)?)deg$");

        public FilterStyleDescriptor Descriptor { get; } = new();

        public FilterStyleParser(string content)
            : base(content)
        { }

        protected override bool Parse()
        {
            string content = _content?.Trim();

            if (string.IsNullOrEmpty(content))
            {
                return false;
            }

            if (content.ToLower() == NONE)
            {
                return true;
            }

            List<string> calls = Split(content);

            if (calls == null || calls.Count == 0)
            {
                return false;
            }

            foreach (string call in calls)
            {
                if (!ParseCall(call))
                {
                    return false;
                }
            }

            return true;
        }

        private bool ParseCall(string call)
        {
            int open = call.IndexOf('(');

            if (open <= 0 || !call.EndsWith(")"))
            {
                return false;
            }

            string name = call.Substring(0, open).ToLower();
            string body = call.Substring(open + 1, call.Length - open - 2).Trim();

            switch (name)
            {
                case BLUR:
                    return Add(FilterKind.Blur, Length(body));

                case GRAYSCALE:
                    return Add(FilterKind.Grayscale, Clamped(body));

                case SEPIA:
                    return Add(FilterKind.Sepia, Clamped(body));

                case INVERT:
                    return Add(FilterKind.Invert, Clamped(body));

                case OPACITY:
                    return Add(FilterKind.Opacity, Clamped(body));

                case SATURATE:
                    return Add(FilterKind.Saturate, Amount(body));

                case BRIGHTNESS:
                    return Add(FilterKind.Brightness, Amount(body));

                case CONTRAST:
                    return Add(FilterKind.Contrast, Amount(body));

                case HUE_ROTATE:
                    return Add(FilterKind.HueRotate, Angle(body));

                default:
                    return false;
            }
        }

        private bool Add(FilterKind kind, float? value)
        {
            if (!value.HasValue)
            {
                return false;
            }

            Descriptor.Operations.Add(new FilterOperation
            {
                Kind = kind,
                Value = value.Value
            });

            return true;
        }

        private static float? Length(string body)
        {
            Match match = _length.Match(body);

            return match.Success && float.TryParse(match.Groups[1].Value, NumberStyles.Float,
                CultureInfo.InvariantCulture, out float value)
                ? value
                : (float?)null;
        }

        private static float? Amount(string body)
        {
            Match match = _amount.Match(body);

            if (!match.Success || !float.TryParse(match.Groups[1].Value, NumberStyles.Float,
                CultureInfo.InvariantCulture, out float value))
            {
                return null;
            }

            return match.Groups[2].Value == "%" ? value / 100f : value;
        }

        private static float? Clamped(string body)
        {
            float? amount = Amount(body);

            return amount.HasValue && amount.Value > 1 ? 1 : amount;
        }

        private static float? Angle(string body)
        {
            Match match = _angle.Match(body);

            return match.Success && float.TryParse(match.Groups[1].Value, NumberStyles.Float,
                CultureInfo.InvariantCulture, out float value)
                ? value
                : (float?)null;
        }

        private static List<string> Split(string content)
        {
            var parts = new List<string>();
            var current = new StringBuilder();
            int depth = 0;

            foreach (char c in content)
            {
                if (c == '(')
                {
                    depth++;
                }
                else if (c == ')')
                {
                    depth--;

                    if (depth < 0)
                    {
                        return null;
                    }
                }

                if (depth == 0 && (c == ' ' || c == '\t'))
                {
                    if (current.Length > 0)
                    {
                        parts.Add(current.ToString());
                        current.Clear();
                    }

                    continue;
                }

                current.Append(c);
            }

            if (depth != 0)
            {
                return null;
            }

            if (current.Length > 0)
            {
                parts.Add(current.ToString());
            }

            return parts;
        }
    }
}
