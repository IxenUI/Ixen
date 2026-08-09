namespace Ixen.Core.Visual.Styles.Parsers
{
    internal class FontFamilyStyleParser : StyleParser
    {
        public Descriptors.FontFamilyStyleDescriptor Descriptor { get; } = new();

        public FontFamilyStyleParser(string content)
            : base(content)
        { }

        protected override bool Parse()
        {
            string value = _content?.Trim();

            if (string.IsNullOrEmpty(value))
            {
                return false;
            }

            Descriptor.Value = value;

            return true;
        }
    }
}
