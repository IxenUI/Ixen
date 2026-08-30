using Ixen.Core.Visual.Styles.Descriptors;
using System;
using System.Globalization;
using System.Text.RegularExpressions;

namespace Ixen.Core.Visual.Styles.Parsers
{
    internal class ObjectPositionStyleParser : StyleParser
    {
        internal const string LEFT = "left";
        internal const string CENTER = "center";
        internal const string RIGHT = "right";
        internal const string TOP = "top";
        internal const string MIDDLE = "middle";
        internal const string BOTTOM = "bottom";

        private static readonly Regex _percent = new Regex(@"^(-?[0-9]+(?:\.[0-9]+)?)%$");
        private static readonly char[] _separators = { ' ', '\t' };

        public ObjectPositionStyleDescriptor Descriptor { get; } = new();

        public ObjectPositionStyleParser(string content)
            : base(content)
        { }

        protected override bool Parse()
        {
            string content = _content?.Trim();

            if (string.IsNullOrEmpty(content))
            {
                return false;
            }

            string[] parts = content.ToLower().Split(_separators, StringSplitOptions.RemoveEmptyEntries);

            if (parts.Length == 0 || parts.Length > 2)
            {
                return false;
            }

            bool horizontal = false;
            bool vertical = false;

            foreach (string part in parts)
            {
                if (part == LEFT || part == CENTER || part == RIGHT)
                {
                    if (horizontal)
                    {
                        return false;
                    }

                    horizontal = true;
                    Descriptor.X = part == LEFT ? 0f : part == RIGHT ? 1f : 0.5f;

                    continue;
                }

                if (part == TOP || part == MIDDLE || part == BOTTOM)
                {
                    if (vertical)
                    {
                        return false;
                    }

                    vertical = true;
                    Descriptor.Y = part == TOP ? 0f : part == BOTTOM ? 1f : 0.5f;

                    continue;
                }

                Match match = _percent.Match(part);

                if (!match.Success || !float.TryParse(match.Groups[1].Value, NumberStyles.Float,
                    CultureInfo.InvariantCulture, out float value))
                {
                    return false;
                }

                value /= 100f;

                if (!horizontal)
                {
                    horizontal = true;
                    Descriptor.X = value;
                }
                else if (!vertical)
                {
                    vertical = true;
                    Descriptor.Y = value;
                }
                else
                {
                    return false;
                }
            }

            return true;
        }
    }
}
