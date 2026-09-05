using Ixen.Core.Visual.Styles.Descriptors;

namespace Ixen.Core.Visual.Styles.Parsers
{
    internal class ScrollBehaviorStyleParser : StyleParser
    {
        internal const string AUTO = "auto";
        internal const string SMOOTH = "smooth";

        public ScrollBehaviorStyleDescriptor Descriptor { get; } = new();

        public ScrollBehaviorStyleParser(string content)
            : base(content)
        { }

        protected override bool Parse()
        {
            switch (_content?.Trim().ToLower())
            {
                case AUTO:
                    Descriptor.Value = ScrollBehavior.Auto;
                    return true;

                case SMOOTH:
                    Descriptor.Value = ScrollBehavior.Smooth;
                    return true;

                default:
                    return false;
            }
        }
    }
}
