using Ixen.Core.Visual.Styles.Descriptors;

namespace Ixen.Core.Visual.Styles.Parsers
{
    public class BackgroundStyleParser : StyleParser
    {
        public BackgroundStyleDescriptor Descriptor { get; } = new BackgroundStyleDescriptor();

        public BackgroundStyleParser(string content)
            : base(content)
        { }

        protected override bool Parse()
        {
            var colorParser = new ColorStyleParser(Content);
            Descriptor.Color = new ColorStyleParser(Content).Descriptor.Value;

            return colorParser.IsValid;
        }
    }
}
