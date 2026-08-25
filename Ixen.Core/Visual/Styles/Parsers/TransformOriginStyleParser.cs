using Ixen.Core.Visual.Styles.Descriptors;
using System;
using System.Globalization;
using System.Text.RegularExpressions;

namespace Ixen.Core.Visual.Styles.Parsers
{
    internal class TransformOriginStyleParser : StyleParser
    {
        internal const string LEFT = "left";
        internal const string CENTER = "center";
        internal const string RIGHT = "right";
        internal const string TOP = "top";
        internal const string MIDDLE = "middle";
        internal const string BOTTOM = "bottom";

        private static Regex _length = new Regex(@"^(-?[0-9]+(?:\.[0-9]+)?)(px|%|)$");

        public TransformOriginStyleDescriptor Descriptor { get; } = new();

        public TransformOriginStyleParser(string content)
            : base(content)
        { }

        protected override bool Parse()
        {
            string[] parts = _content?.Trim()
                .Split(TransformStyleParser.Blanks, StringSplitOptions.RemoveEmptyEntries);

            if (parts == null || parts.Length == 0 || parts.Length > 2)
            {
                return false;
            }

            bool horizontal = false;
            bool vertical = false;

            foreach (string part in parts)
            {
                switch (part.ToLower())
                {
                    case LEFT:
                        if (horizontal)
                        {
                            return false;
                        }

                        Descriptor.XUnit = SizeUnit.Percents;
                        Descriptor.X = 0;
                        horizontal = true;
                        continue;

                    case CENTER:
                        if (horizontal)
                        {
                            return false;
                        }

                        Descriptor.XUnit = SizeUnit.Percents;
                        Descriptor.X = TransformOriginStyleDescriptor.CENTRE;
                        horizontal = true;
                        continue;

                    case RIGHT:
                        if (horizontal)
                        {
                            return false;
                        }

                        Descriptor.XUnit = SizeUnit.Percents;
                        Descriptor.X = 100;
                        horizontal = true;
                        continue;

                    case TOP:
                        if (vertical)
                        {
                            return false;
                        }

                        Descriptor.YUnit = SizeUnit.Percents;
                        Descriptor.Y = 0;
                        vertical = true;
                        continue;

                    case MIDDLE:
                        if (vertical)
                        {
                            return false;
                        }

                        Descriptor.YUnit = SizeUnit.Percents;
                        Descriptor.Y = TransformOriginStyleDescriptor.CENTRE;
                        vertical = true;
                        continue;

                    case BOTTOM:
                        if (vertical)
                        {
                            return false;
                        }

                        Descriptor.YUnit = SizeUnit.Percents;
                        Descriptor.Y = 100;
                        vertical = true;
                        continue;
                }

                Match match = _length.Match(part);

                if (!match.Success || !float.TryParse(match.Groups[1].Value, NumberStyles.Float,
                    CultureInfo.InvariantCulture, out float value))
                {
                    return false;
                }

                SizeUnit unit = match.Groups[2].Value == "%" ? SizeUnit.Percents : SizeUnit.Pixels;

                if (!horizontal)
                {
                    Descriptor.XUnit = unit;
                    Descriptor.X = value;
                    horizontal = true;
                    continue;
                }

                if (vertical)
                {
                    return false;
                }

                Descriptor.YUnit = unit;
                Descriptor.Y = value;
                vertical = true;
            }

            return true;
        }
    }
}
