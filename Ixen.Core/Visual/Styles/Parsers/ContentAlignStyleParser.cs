using Ixen.Core.Visual.Styles.Descriptors;
using System.Text.RegularExpressions;

namespace Ixen.Core.Visual.Styles.Parsers
{
    internal class ContentAlignStyleParser : StyleParser
    {
        private static Regex _splitter = new Regex(@"[^ \t]+");

        public ContentAlignStyleDescriptor Descriptor { get; } = new();

        public ContentAlignStyleParser(string content)
            : base(content)
        { }

        protected override bool Parse()
        {
            MatchCollection parts = _splitter.Matches(_content ?? string.Empty);

            if (parts.Count < 1 || parts.Count > 2)
            {
                return false;
            }

            foreach (Match part in parts)
            {
                switch (part.Value.ToLower())
                {
                    case "left":
                        if (Descriptor.Horizontal != ContentAlign.Unset) { return false; }
                        Descriptor.Horizontal = ContentAlign.Left;
                        break;

                    case "center":
                        if (Descriptor.Horizontal != ContentAlign.Unset) { return false; }
                        Descriptor.Horizontal = ContentAlign.Center;
                        break;

                    case "right":
                        if (Descriptor.Horizontal != ContentAlign.Unset) { return false; }
                        Descriptor.Horizontal = ContentAlign.Right;
                        break;

                    case "top":
                        if (Descriptor.Vertical != ContentVAlign.Unset) { return false; }
                        Descriptor.Vertical = ContentVAlign.Top;
                        break;

                    case "middle":
                        if (Descriptor.Vertical != ContentVAlign.Unset) { return false; }
                        Descriptor.Vertical = ContentVAlign.Middle;
                        break;

                    case "bottom":
                        if (Descriptor.Vertical != ContentVAlign.Unset) { return false; }
                        Descriptor.Vertical = ContentVAlign.Bottom;
                        break;

                    default:
                        return false;
                }
            }

            return true;
        }
    }
}
