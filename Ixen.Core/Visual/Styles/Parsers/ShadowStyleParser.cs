using Ixen.Core.Visual.Styles.Descriptors;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;

namespace Ixen.Core.Visual.Styles.Parsers
{
    internal class ShadowStyleParser : StyleParser
    {
        private static Regex _splitter = new Regex(@"[^ \t]+");
        private static Regex _length = new Regex(@"^(-?[0-9]+(?:\.[0-9]+)?)(px)?$");
        private static Regex _color = new Regex(@"^#(?:[0-9A-Fa-f]{8}|[0-9A-Fa-f]{6})$");

        protected virtual int MaxLengths => 4;

        public ShadowStyleDescriptor Descriptor { get; } = new();

        public ShadowStyleParser(string content)
            : base(content)
        { }

        protected override bool Parse()
        {
            MatchCollection parts = _splitter.Matches(_content ?? string.Empty);

            var lengths = new List<float>();
            string color = null;

            foreach (Match part in parts)
            {
                if (_color.IsMatch(part.Value))
                {
                    if (color != null)
                    {
                        return false;
                    }

                    color = part.Value;
                    continue;
                }

                Match length = _length.Match(part.Value);

                if (!length.Success || lengths.Count == MaxLengths)
                {
                    return false;
                }

                if (!float.TryParse(length.Groups[1].Value, NumberStyles.Float,
                    CultureInfo.InvariantCulture, out float value))
                {
                    return false;
                }

                lengths.Add(value);
            }

            if (lengths.Count < 2 || color == null)
            {
                return false;
            }

            Descriptor.OffsetX = lengths[0];
            Descriptor.OffsetY = lengths[1];
            Descriptor.Blur = lengths.Count > 2 ? lengths[2] : 0;
            Descriptor.Spread = lengths.Count > 3 ? lengths[3] : 0;
            Descriptor.Color = color;

            return Descriptor.Blur >= 0 && Descriptor.Spread >= 0;
        }
    }
}
