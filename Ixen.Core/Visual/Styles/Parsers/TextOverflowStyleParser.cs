using Ixen.Core.Visual.Styles.Descriptors;

namespace Ixen.Core.Visual.Styles.Parsers
{
    internal class TextOverflowStyleParser : StyleParser
    {
        public TextOverflowStyleDescriptor Descriptor { get; } = new();

        public TextOverflowStyleParser(string content)
            : base(content)
        { }

        protected override bool Parse()
        {
            switch (_content?.Trim().ToLower())
            {
                case "clip":
                    Descriptor.Value = TextOverflow.Clip;
                    return true;

                case "ellipsis":
                    Descriptor.Value = TextOverflow.Ellipsis;
                    return true;

                default:
                    return false;
            }
        }
    }
}
