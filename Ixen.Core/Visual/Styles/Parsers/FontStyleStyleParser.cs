using Ixen.Core.Visual.Styles.Descriptors;

namespace Ixen.Core.Visual.Styles.Parsers
{
    internal class FontStyleStyleParser : StyleParser
    {
        public FontStyleStyleDescriptor Descriptor { get; } = new();

        public FontStyleStyleParser(string content)
            : base(content)
        { }

        protected override bool Parse()
        {
            switch (_content?.Trim().ToLower())
            {
                case "normal":
                    Descriptor.Value = FontStyle.Normal;
                    return true;

                case "italic":
                    Descriptor.Value = FontStyle.Italic;
                    return true;

                default:
                    return false;
            }
        }
    }
}
