using Ixen.Core.Visual.Styles.Descriptors;
using System.Globalization;
using System.Text.RegularExpressions;

namespace Ixen.Core.Visual.Styles.Parsers
{
    internal class SizeStyleParser : StyleParser
    {
        private static Regex _regex = new Regex(
            @"^\s*(?:(\?)|([0-9]+(?:\.[0-9]+)?)(px|%|\*|)(?:([+-][0-9]+(?:\.[0-9]+)?)px)?)\s*$");
        public SizeStyleDescriptor Descriptor { get; } = new SizeStyleDescriptor();

        public SizeStyleParser(string content)
            : base(content)
        {}

        protected override bool Parse()
        {
            Match m = _regex.Match(_content);

            if (!m.Success)
            {
                return false;
            }

            if (m.Groups[1].Success)
            {
                Descriptor.Unit = SizeUnit.Content;
                Descriptor.Value = 0;
                return true;
            }

            if (!float.TryParse(m.Groups[2].Value, NumberStyles.Float, CultureInfo.InvariantCulture, out float floatValue))
            {
                return false;
            }

            if (m.Groups[4].Success)
            {
                if (m.Groups[3].Value != "%"
                    || !float.TryParse(m.Groups[4].Value, NumberStyles.Float,
                        CultureInfo.InvariantCulture, out float offset))
                {
                    return false;
                }

                Descriptor.Offset = offset;
            }

            Descriptor.Value = floatValue;
            switch(m.Groups[3].Value)
            {
                case "px":
                    Descriptor.Unit = SizeUnit.Pixels;
                    return true;

                case "%":
                    Descriptor.Unit = SizeUnit.Percents;
                    return true;

                case "*":
                    Descriptor.Unit = SizeUnit.Weight;
                    return true;

                case "":
                    if (Descriptor.Value == 0)
                    {
                        Descriptor.Unit = SizeUnit.Pixels;
                        return true;
                    }
                    return false;

                default:
                    return false;
            }
        }
    }
}
