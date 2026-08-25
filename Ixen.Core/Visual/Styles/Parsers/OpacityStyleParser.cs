using Ixen.Core.Visual.Styles.Descriptors;
using System.Globalization;
using System.Text.RegularExpressions;

namespace Ixen.Core.Visual.Styles.Parsers
{
    internal class OpacityStyleParser : StyleParser
    {
        private static Regex _regex = new Regex(@"^\s*([0-9]+(?:\.[0-9]+)?)(%?)\s*$");

        public OpacityStyleDescriptor Descriptor { get; } = new();

        public OpacityStyleParser(string content)
            : base(content)
        { }

        protected override bool Parse()
        {
            Match match = _regex.Match(_content ?? string.Empty);

            if (!match.Success || !float.TryParse(match.Groups[1].Value, NumberStyles.Float,
                CultureInfo.InvariantCulture, out float value))
            {
                return false;
            }

            if (match.Groups[2].Value == "%")
            {
                value /= 100f;
            }

            if (value > OpacityStyleDescriptor.OPAQUE)
            {
                return false;
            }

            Descriptor.Value = value;
            return true;
        }
    }
}
