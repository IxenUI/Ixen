using Ixen.Core.Visual.Styles.Descriptors;

namespace Ixen.Core.Visual.Styles.Parsers
{
    internal class PaddingStyleParser : SpaceStyleParser
    {
        public new PaddingStyleDescriptor Descriptor { get; } = new PaddingStyleDescriptor();

        public PaddingStyleParser(string content)
            : base(content)
        { }

        protected override bool Parse()
        {
            bool valid = base.Parse();

            if (valid)
            {
                Descriptor.Set(base.Descriptor);
            }

            return valid;
        }
    }
}
