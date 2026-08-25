using Ixen.Core.Visual.Styles.Descriptors;
using System.Text.RegularExpressions;

namespace Ixen.Core.Visual.Styles.Parsers
{
    internal class TextDecorationStyleParser : StyleParser
    {
        internal const string NONE = "none";
        internal const string UNDERLINE = "underline";
        internal const string LINE_THROUGH = "line-through";
        internal const string OVERLINE = "overline";

        private static Regex _splitter = new Regex(@"[^ \t]+");

        public TextDecorationStyleDescriptor Descriptor { get; } = new();

        public TextDecorationStyleParser(string content)
            : base(content)
        { }

        protected override bool Parse()
        {
            MatchCollection parts = _splitter.Matches(_content ?? string.Empty);

            if (parts.Count < 1 || parts.Count > 3)
            {
                return false;
            }

            TextDecorations decorations = TextDecorations.None;

            foreach (Match part in parts)
            {
                TextDecorations one;

                switch (part.Value.ToLower())
                {
                    case NONE:
                        if (parts.Count > 1)
                        {
                            return false;
                        }

                        Descriptor.IsDeclared = true;
                        return true;

                    case UNDERLINE:
                        one = TextDecorations.Underline;
                        break;

                    case LINE_THROUGH:
                        one = TextDecorations.LineThrough;
                        break;

                    case OVERLINE:
                        one = TextDecorations.Overline;
                        break;

                    default:
                        return false;
                }

                if ((decorations & one) == one)
                {
                    return false;
                }

                decorations |= one;
            }

            Descriptor.Value = decorations;
            Descriptor.IsDeclared = true;

            return true;
        }
    }
}
