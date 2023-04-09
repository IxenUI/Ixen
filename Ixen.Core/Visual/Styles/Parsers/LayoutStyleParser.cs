using Ixen.Core.Visual.Styles.Descriptors;

namespace Ixen.Core.Visual.Styles.Parsers
{
    public class LayoutStyleParser : StyleParser
    {
        public LayoutStyleDescriptor Descriptor { get; } = new LayoutStyleDescriptor();

        public LayoutStyleParser(string content)
            : base(content)
        { }

        protected override bool Parse() => true;
    }
}
