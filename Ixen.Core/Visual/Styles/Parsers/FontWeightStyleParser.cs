using Ixen.Core.Visual.Styles.Descriptors;

namespace Ixen.Core.Visual.Styles.Parsers
{
    internal class FontWeightStyleParser : StyleParser
    {
        public FontWeightStyleDescriptor Descriptor { get; } = new();

        public FontWeightStyleParser(string content)
            : base(content)
        { }

        protected override bool Parse()
        {
            switch (_content?.Trim().ToLower())
            {
                case "normal":
                    Descriptor.Value = FontWeight.Normal;
                    return true;

                case "bold":
                    Descriptor.Value = FontWeight.Bold;
                    return true;

                default:
                    return false;
            }
        }
    }
}
