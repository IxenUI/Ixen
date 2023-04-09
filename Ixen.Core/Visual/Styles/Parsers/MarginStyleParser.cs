using Ixen.Core.Visual.Styles.Descriptors;

namespace Ixen.Core.Visual.Styles.Parsers
{
    public class MarginStyleParser : SpaceStyleParser
    {
        public new MarginStyleDescriptor Descriptor { get; } = new MarginStyleDescriptor();

        public MarginStyleParser(string content)
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
