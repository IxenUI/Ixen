using Ixen.Core.Visual.Styles.Descriptors;

namespace Ixen.Core.Visual.Styles.Parsers
{
    internal class TextAlignStyleParser : StyleParser
    {
        public TextAlignStyleDescriptor Descriptor { get; } = new();

        public TextAlignStyleParser(string content)
            : base(content)
        { }

        protected override bool Parse()
        {
            switch (_content?.Trim().ToLower())
            {
                case "left":
                    Descriptor.Value = TextAlign.Left;
                    return true;

                case "center":
                    Descriptor.Value = TextAlign.Center;
                    return true;

                case "right":
                    Descriptor.Value = TextAlign.Right;
                    return true;

                default:
                    return false;
            }
        }
    }
}
