using Ixen.Core.Visual.Styles.Descriptors;

namespace Ixen.Core.Visual.Styles.Parsers
{
    public class BorderStyleParser : StyleParser
    {
        public BorderStyleDescriptor Descriptor { get; } = new BorderStyleDescriptor();

        public BorderStyleParser(string content)
            : base(content)
        { }

        protected override bool Parse() => true;
    }
}
