using Ixen.Core.Visual.Styles.Descriptors;

namespace Ixen.Core.Visual.Styles.Parsers
{
    public class MaskStyleParser : StyleParser
    {
        public MaskStyleDescriptor Descriptor { get; } = new MaskStyleDescriptor();

        public MaskStyleParser(string content)
            : base(content)
        { }

        protected override bool Parse() => true;
    }
}
