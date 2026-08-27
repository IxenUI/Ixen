using Ixen.Core.Visual.Styles.Descriptors;
using System.Globalization;
using System.Text.RegularExpressions;

namespace Ixen.Core.Visual.Styles.Parsers
{
    internal class LineHeightStyleParser : StyleParser
    {
        internal const string NORMAL = "normal";

        private static Regex _regex = new Regex(@"^([0-9]+(?:\.[0-9]+)?)(px|%|)$");

        public LineHeightStyleDescriptor Descriptor { get; } = new();

        public LineHeightStyleParser(string content)
            : base(content)
        { }

        protected override bool Parse()
        {
            string content = _content?.Trim();

            if (string.IsNullOrEmpty(content))
            {
                return false;
            }

            if (content.ToLower() == NORMAL)
            {
                Descriptor.Kind = LineHeightKind.Normal;
                return true;
            }

            Match match = _regex.Match(content);

            if (!match.Success || !float.TryParse(match.Groups[1].Value, NumberStyles.Float,
                CultureInfo.InvariantCulture, out float value))
            {
                return false;
            }

            switch (match.Groups[2].Value)
            {
                case "px":
                    Descriptor.Kind = LineHeightKind.Pixels;
                    break;

                case "%":
                    Descriptor.Kind = LineHeightKind.Percents;
                    break;

                default:
                    Descriptor.Kind = LineHeightKind.Multiplier;
                    break;
            }

            if (value <= 0)
            {
                return false;
            }

            Descriptor.Value = value;
            return true;
        }
    }
}
