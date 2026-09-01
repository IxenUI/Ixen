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

        internal const string INSET = "inset";

        protected virtual int MaxLengths => 4;

        protected virtual bool AllowsInset => true;

        public ShadowStyleDescriptor Descriptor { get; } = new();

        public ShadowStyleParser(string content)
            : base(content)
        { }

        protected override bool Parse()
        {
            string[] entries = (_content ?? string.Empty).Split(',');

            foreach (string entry in entries)
            {
                Shadow shadow = ParseOne(entry);

                if (shadow == null)
                {
                    return false;
                }

                Descriptor.Shadows.Add(shadow);
            }

            return Descriptor.Shadows.Count > 0;
        }

        private Shadow ParseOne(string content)
        {
            MatchCollection parts = _splitter.Matches(content);

            var lengths = new List<float>();
            string color = null;
            bool inset = false;

            foreach (Match part in parts)
            {
                if (part.Value == INSET)
                {
                    if (inset || !AllowsInset)
                    {
                        return null;
                    }

                    inset = true;
                    continue;
                }

                if (_color.IsMatch(part.Value))
                {
                    if (color != null)
                    {
                        return null;
                    }

                    color = part.Value;
                    continue;
                }

                Match length = _length.Match(part.Value);

                if (!length.Success || lengths.Count == MaxLengths)
                {
                    return null;
                }

                if (!float.TryParse(length.Groups[1].Value, NumberStyles.Float,
                    CultureInfo.InvariantCulture, out float value))
                {
                    return null;
                }

                lengths.Add(value);
            }

            if (lengths.Count < 2 || color == null)
            {
                return null;
            }

            var shadow = new Shadow
            {
                OffsetX = lengths[0],
                OffsetY = lengths[1],
                Blur = lengths.Count > 2 ? lengths[2] : 0,
                Spread = lengths.Count > 3 ? lengths[3] : 0,
                Inset = inset,
                Color = color
            };

            return shadow.Blur >= 0 && shadow.Spread >= 0 ? shadow : null;
        }
    }
}
