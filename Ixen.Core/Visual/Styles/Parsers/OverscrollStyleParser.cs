using Ixen.Core.Visual.Styles.Descriptors;
using System;

namespace Ixen.Core.Visual.Styles.Parsers
{
    internal class OverscrollStyleParser : StyleParser
    {
        internal const string AUTO = "auto";
        internal const string CONTAIN = "contain";
        internal const string NONE = "none";

        private static readonly char[] SEPARATORS = new[] { ' ', '\t' };

        public OverscrollStyleDescriptor Descriptor { get; } = new();

        public OverscrollStyleParser(string content)
            : base(content)
        { }

        protected override bool Parse()
        {
            if (_content == null)
            {
                return false;
            }

            string[] parts = _content.Trim().ToLower()
                .Split(SEPARATORS, StringSplitOptions.RemoveEmptyEntries);

            if (parts.Length == 0 || parts.Length > 2)
            {
                return false;
            }

            if (!TryParseKind(parts[0], out OverscrollKind x))
            {
                return false;
            }

            OverscrollKind y = x;

            if (parts.Length == 2 && !TryParseKind(parts[1], out y))
            {
                return false;
            }

            Descriptor.X = x;
            Descriptor.Y = y;

            return true;
        }

        private static bool TryParseKind(string content, out OverscrollKind kind)
        {
            switch (content)
            {
                case AUTO:
                    kind = OverscrollKind.Auto;
                    return true;

                case CONTAIN:
                    kind = OverscrollKind.Contain;
                    return true;

                case NONE:
                    kind = OverscrollKind.None;
                    return true;

                default:
                    kind = OverscrollKind.Unset;
                    return false;
            }
        }
    }
}
