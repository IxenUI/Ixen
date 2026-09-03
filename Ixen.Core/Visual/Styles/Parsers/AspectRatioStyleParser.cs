using Ixen.Core.Visual.Styles.Descriptors;
using System.Globalization;
using System.Text.RegularExpressions;

namespace Ixen.Core.Visual.Styles.Parsers
{
    internal class AspectRatioStyleParser : StyleParser
    {
        private static Regex _regex = new Regex(
            @"^\s*([0-9]+(?:\.[0-9]+)?)\s*(?:/\s*([0-9]+(?:\.[0-9]+)?)\s*)?$");

        public AspectRatioStyleDescriptor Descriptor { get; } = new();

        public AspectRatioStyleParser(string content)
            : base(content)
        { }

        protected override bool Parse()
        {
            Match match = _regex.Match(_content ?? string.Empty);

            if (!match.Success)
            {
                return false;
            }

            if (!float.TryParse(match.Groups[1].Value, NumberStyles.Float,
                CultureInfo.InvariantCulture, out float width))
            {
                return false;
            }

            float height = 1;

            if (match.Groups[2].Success && !float.TryParse(match.Groups[2].Value,
                NumberStyles.Float, CultureInfo.InvariantCulture, out height))
            {
                return false;
            }

            if (width <= 0 || height <= 0)
            {
                return false;
            }

            Descriptor.Ratio = width / height;

            return true;
        }
    }
}
