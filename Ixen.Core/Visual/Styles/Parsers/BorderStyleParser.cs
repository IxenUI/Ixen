using Ixen.Core.Visual.Styles.Descriptors;
using System;
using System.Globalization;
using System.Text.RegularExpressions;

namespace Ixen.Core.Visual.Styles.Parsers
{
    internal class BorderStyleParser : StyleParser
    {
        private static Regex _splitter = new Regex(@"[^ \t]+");
        private static Regex _thickness = new Regex(@"^([0-9]+(?:\.[0-9]+)?)(px)?$");

        public BorderStyleDescriptor Descriptor { get; } = new BorderStyleDescriptor();

        public BorderStyleParser(string content)
            : base(content)
        { }

        protected override bool Parse()
        {
            MatchCollection parts = _splitter.Matches(_content ?? string.Empty);

            if (parts.Count < 1 || parts.Count > 3)
            {
                return false;
            }

            bool hasColor = false;
            bool hasThickness = false;
            bool hasType = false;

            foreach (Match part in parts)
            {
                string value = part.Value;

                if (value[0] == '#')
                {
                    var colorParser = new ColorStyleParser(value);

                    if (hasColor || !colorParser.IsValid)
                    {
                        return false;
                    }

                    Descriptor.Color = colorParser.Descriptor.Value;
                    hasColor = true;
                    continue;
                }

                Match thickness = _thickness.Match(value);

                if (thickness.Success)
                {
                    if (hasThickness
                        || !float.TryParse(thickness.Groups[1].Value, NumberStyles.Float,
                            CultureInfo.InvariantCulture, out float parsed))
                    {
                        return false;
                    }

                    Descriptor.Thickness = parsed;
                    hasThickness = true;
                    continue;
                }

                if (hasType || !Enum.TryParse(value, true, out BorderType type))
                {
                    return false;
                }

                Descriptor.Type = type;
                hasType = true;
            }

            return hasColor && hasThickness;
        }
    }
}
