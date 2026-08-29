using Ixen.Core.Visual.Styles.Descriptors;

namespace Ixen.Core.Visual.Styles.Parsers
{
    internal class OverscrollStyleParser : StyleParser
    {
        internal const string AUTO = "auto";
        internal const string CONTAIN = "contain";
        internal const string NONE = "none";

        public OverscrollStyleDescriptor Descriptor { get; } = new();

        public OverscrollStyleParser(string content)
            : base(content)
        { }

        protected override bool Parse()
        {
            switch (_content?.Trim().ToLower())
            {
                case AUTO:
                    Descriptor.Value = OverscrollKind.Auto;
                    return true;

                case CONTAIN:
                case NONE:
                    Descriptor.Value = OverscrollKind.Contain;
                    return true;

                default:
                    return false;
            }
        }
    }
}
