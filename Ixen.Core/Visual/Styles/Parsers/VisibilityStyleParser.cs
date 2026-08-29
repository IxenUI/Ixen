using Ixen.Core.Visual.Styles.Descriptors;

namespace Ixen.Core.Visual.Styles.Parsers
{
    internal class VisibilityStyleParser : StyleParser
    {
        internal const string VISIBLE = "visible";
        internal const string HIDDEN = "hidden";

        public VisibilityStyleDescriptor Descriptor { get; } = new();

        public VisibilityStyleParser(string content)
            : base(content)
        { }

        protected override bool Parse()
        {
            switch (_content?.Trim().ToLower())
            {
                case VISIBLE:
                    Descriptor.Value = Visibility.Visible;
                    return true;

                case HIDDEN:
                    Descriptor.Value = Visibility.Hidden;
                    return true;

                default:
                    return false;
            }
        }
    }
}
