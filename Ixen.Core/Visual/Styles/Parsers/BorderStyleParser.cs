using Ixen.Core.Visual.Styles.Descriptors;
using System;
using System.Collections.Generic;
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

            if (parts.Count < 1 || parts.Count > 9)
            {
                return false;
            }

            bool hasType = false;
            var colors = new List<string>();
            var thicknesses = new List<float>();

            foreach (Match part in parts)
            {
                string value = part.Value;

                if (value[0] == '#')
                {
                    var colorParser = new ColorStyleParser(value);

                    if (colors.Count == 4 || !colorParser.IsValid)
                    {
                        return false;
                    }

                    colors.Add(colorParser.Descriptor.Value);
                    continue;
                }

                Match thickness = _thickness.Match(value);

                if (thickness.Success)
                {
                    if (thicknesses.Count == 4
                        || !float.TryParse(thickness.Groups[1].Value, NumberStyles.Float,
                            CultureInfo.InvariantCulture, out float parsed))
                    {
                        return false;
                    }

                    thicknesses.Add(parsed);
                    continue;
                }

                if (hasType || !Enum.TryParse(value, true, out BorderType type))
                {
                    return false;
                }

                Descriptor.Type = type;
                hasType = true;
            }

            if (colors.Count == 0 || thicknesses.Count == 0)
            {
                return false;
            }

            ApplyColors(colors);
            ApplyThicknesses(thicknesses);

            return true;
        }

        private void ApplyColors(List<string> values)
        {
            Descriptor.Color = values[0];

            switch (values.Count)
            {
                case 1:
                    return;

                case 2:
                    Descriptor.SetColors(values[0], values[1], values[0], values[1]);
                    return;

                case 3:
                    Descriptor.SetColors(values[0], values[1], values[2], values[1]);
                    return;

                default:
                    Descriptor.SetColors(values[0], values[1], values[2], values[3]);
                    return;
            }
        }

        private void ApplyThicknesses(List<float> values)
        {
            switch (values.Count)
            {
                case 1:
                    Descriptor.Thickness = values[0];
                    return;

                case 2:
                    Descriptor.Thickness = values[0];
                    Descriptor.SetThickness(values[0], values[1], values[0], values[1]);
                    return;

                case 3:
                    Descriptor.Thickness = values[0];
                    Descriptor.SetThickness(values[0], values[1], values[2], values[1]);
                    return;

                default:
                    Descriptor.Thickness = values[0];
                    Descriptor.SetThickness(values[0], values[1], values[2], values[3]);
                    return;
            }
        }
    }
}
