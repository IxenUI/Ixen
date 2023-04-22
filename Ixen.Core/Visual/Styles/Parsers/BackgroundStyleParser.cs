using Ixen.Core.Visual.Styles.Descriptors;

namespace Ixen.Core.Visual.Styles.Parsers
{
    internal class BackgroundStyleParser : StyleParser
    {
        public BackgroundStyleDescriptor Descriptor { get; } = new BackgroundStyleDescriptor();

        public BackgroundStyleParser(string content)
            : base(content)
        { }

        protected override bool Parse()
        {
            var colorParser = new ColorStyleParser(_content);
            Descriptor.Color = colorParser.Descriptor.Value;

            return colorParser.IsValid;
        }
    }
}
