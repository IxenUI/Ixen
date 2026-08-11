using Ixen.Core.Visual.Styles.Descriptors;

namespace Ixen.Core.Visual.Styles.Parsers
{
    internal class TextWrapStyleParser : StyleParser
    {
        public TextWrapStyleDescriptor Descriptor { get; } = new();

        public TextWrapStyleParser(string content)
            : base(content)
        { }

        protected override bool Parse()
        {
            switch (_content?.Trim().ToLower())
            {
                case "wrap":
                    Descriptor.Value = TextWrap.Wrap;
                    return true;

                case "nowrap":
                    Descriptor.Value = TextWrap.NoWrap;
                    return true;

                default:
                    return false;
            }
        }
    }
}
