using Ixen.Core.Visual.Styles.Descriptors;
using System.Globalization;
using System.Text.RegularExpressions;

namespace Ixen.Core.Visual.Styles.Parsers
{
    internal class GapStyleParser : StyleParser
    {
        private static Regex _splitter = new Regex(@"[^ \t]+");
        private static Regex _length = new Regex(@"^([0-9]+(?:\.[0-9]+)?)(px)?$");

        public GapStyleDescriptor Descriptor { get; } = new();

        public GapStyleParser(string content)
            : base(content)
        { }

        protected override bool Parse()
        {
            MatchCollection parts = _splitter.Matches(_content ?? string.Empty);

            if (parts.Count < 1 || parts.Count > 2)
            {
                return false;
            }

            var lengths = new float[parts.Count];

            for (int i = 0; i < parts.Count; i++)
            {
                Match length = _length.Match(parts[i].Value);

                if (!length.Success || !float.TryParse(length.Groups[1].Value, NumberStyles.Float,
                    CultureInfo.InvariantCulture, out lengths[i]))
                {
                    return false;
                }
            }

            Descriptor.Row = lengths[0];
            Descriptor.Column = parts.Count == 2 ? lengths[1] : lengths[0];

            return true;
        }
    }
}
