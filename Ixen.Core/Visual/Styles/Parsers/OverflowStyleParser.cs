using Ixen.Core.Visual.Styles.Descriptors;

namespace Ixen.Core.Visual.Styles.Parsers
{
    internal class OverflowStyleParser : StyleParser
    {
        internal const string HIDDEN = "hidden";
        internal const string SCROLL = "scroll";
        internal const string AUTO = "auto";

        public OverflowStyleDescriptor Descriptor { get; } = new();

        public OverflowStyleParser(string content)
            : base(content)
        { }

        protected override bool Parse()
        {
            switch (_content?.Trim().ToLower())
            {
                case HIDDEN:
                    Descriptor.Value = OverflowKind.Hidden;
                    return true;

                case SCROLL:
                case AUTO:
                    Descriptor.Value = OverflowKind.Scroll;
                    return true;

                default:
                    return false;
            }
        }
    }
}
