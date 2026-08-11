using Ixen.Core.Visual.Styles.Descriptors;
using System.Text.RegularExpressions;

namespace Ixen.Core.Visual.Styles.Parsers
{
    internal class TextAlignStyleParser : StyleParser
    {
        private static Regex _splitter = new Regex(@"[^ \t]+");

        public TextAlignStyleDescriptor Descriptor { get; } = new();

        public TextAlignStyleParser(string content)
            : base(content)
        { }

        protected override bool Parse()
        {
            MatchCollection parts = _splitter.Matches(_content ?? string.Empty);

            if (parts.Count < 1 || parts.Count > 2)
            {
                return false;
            }

            bool hasHorizontal = false;
            bool hasVertical = false;

            foreach (Match part in parts)
            {
                switch (part.Value.ToLower())
                {
                    case "left":
                        if (hasHorizontal) { return false; }
                        Descriptor.Horizontal = TextAlign.Left;
                        hasHorizontal = true;
                        break;

                    case "center":
                        if (hasHorizontal) { return false; }
                        Descriptor.Horizontal = TextAlign.Center;
                        hasHorizontal = true;
                        break;

                    case "right":
                        if (hasHorizontal) { return false; }
                        Descriptor.Horizontal = TextAlign.Right;
                        hasHorizontal = true;
                        break;

                    case "top":
                        if (hasVertical) { return false; }
                        Descriptor.Vertical = TextVAlign.Top;
                        hasVertical = true;
                        break;

                    case "middle":
                        if (hasVertical) { return false; }
                        Descriptor.Vertical = TextVAlign.Middle;
                        hasVertical = true;
                        break;

                    case "bottom":
                        if (hasVertical) { return false; }
                        Descriptor.Vertical = TextVAlign.Bottom;
                        hasVertical = true;
                        break;

                    default:
                        return false;
                }
            }

            return true;
        }
    }
}
