using System.Globalization;
using System.Text.RegularExpressions;

namespace Ixen.Core.Visual.Styles.Parsers
{
    internal class FontSizeStyleParser : StyleParser
    {
        private static Regex _regex = new Regex(@"^([0-9]+(?:\.[0-9]+)?)(px)?$");

        public Descriptors.FontSizeStyleDescriptor Descriptor { get; } = new();

        public FontSizeStyleParser(string content)
            : base(content)
        { }

        protected override bool Parse()
        {
            Match m = _regex.Match(_content?.Trim() ?? string.Empty);

            if (!m.Success
                || !float.TryParse(m.Groups[1].Value, NumberStyles.Float, CultureInfo.InvariantCulture, out float value)
                || value <= 0)
            {
                return false;
            }

            Descriptor.Value = value;

            return true;
        }
    }
}
