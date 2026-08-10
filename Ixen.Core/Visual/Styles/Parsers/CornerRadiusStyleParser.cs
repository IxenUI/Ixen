using Ixen.Core.Visual.Styles.Descriptors;
using System.Globalization;
using System.Text.RegularExpressions;

namespace Ixen.Core.Visual.Styles.Parsers
{
    internal class CornerRadiusStyleParser : StyleParser
    {
        private static Regex _splitter = new Regex(@"[^ \t]+");
        private static Regex _value = new Regex(@"^([0-9]+(?:\.[0-9]+)?)(px)?$");

        public CornerRadiusStyleDescriptor Descriptor { get; } = new();

        public CornerRadiusStyleParser(string content)
            : base(content)
        { }

        protected override bool Parse()
        {
            MatchCollection matches = _splitter.Matches(_content ?? string.Empty);

            if (matches.Count < 1 || matches.Count > 4)
            {
                return false;
            }

            var values = new float[matches.Count];

            for (int i = 0; i < matches.Count; i++)
            {
                if (!TryReadValue(matches[i].Value, out values[i]))
                {
                    return false;
                }
            }

            switch (values.Length)
            {
                case 1:
                    Set(values[0], values[0], values[0], values[0]);
                    return true;

                case 2:
                    Set(values[0], values[1], values[0], values[1]);
                    return true;

                case 3:
                    Set(values[0], values[1], values[2], values[1]);
                    return true;

                default:
                    Set(values[0], values[1], values[2], values[3]);
                    return true;
            }
        }

        private void Set(float topLeft, float topRight, float bottomRight, float bottomLeft)
        {
            Descriptor.TopLeft = topLeft;
            Descriptor.TopRight = topRight;
            Descriptor.BottomRight = bottomRight;
            Descriptor.BottomLeft = bottomLeft;
        }

        private static bool TryReadValue(string content, out float value)
        {
            value = 0;
            Match m = _value.Match(content);

            return m.Success
                && float.TryParse(m.Groups[1].Value, NumberStyles.Float, CultureInfo.InvariantCulture, out value);
        }
    }
}
