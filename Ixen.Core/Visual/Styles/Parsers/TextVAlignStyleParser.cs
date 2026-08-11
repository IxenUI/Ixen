using Ixen.Core.Visual.Styles.Descriptors;

namespace Ixen.Core.Visual.Styles.Parsers
{
    internal class TextVAlignStyleParser : StyleParser
    {
        public TextVAlignStyleDescriptor Descriptor { get; } = new();

        public TextVAlignStyleParser(string content)
            : base(content)
        { }

        protected override bool Parse()
        {
            switch (_content?.Trim().ToLower())
            {
                case "top":
                    Descriptor.Value = TextVAlign.Top;
                    return true;

                case "middle":
                    Descriptor.Value = TextVAlign.Middle;
                    return true;

                case "bottom":
                    Descriptor.Value = TextVAlign.Bottom;
                    return true;

                default:
                    return false;
            }
        }
    }
}
