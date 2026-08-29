using Ixen.Core.Visual.Styles.Descriptors;

namespace Ixen.Core.Visual.Styles.Parsers
{
    internal class PointerEventsStyleParser : StyleParser
    {
        internal const string AUTO = "auto";
        internal const string NONE = "none";

        public PointerEventsStyleDescriptor Descriptor { get; } = new();

        public PointerEventsStyleParser(string content)
            : base(content)
        { }

        protected override bool Parse()
        {
            switch (_content?.Trim().ToLower())
            {
                case AUTO:
                    Descriptor.Value = PointerEvents.Auto;
                    return true;

                case NONE:
                    Descriptor.Value = PointerEvents.None;
                    return true;

                default:
                    return false;
            }
        }
    }
}
