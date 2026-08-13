using Ixen.Core.Visual.Styles.Descriptors;

namespace Ixen.Core.Visual.Styles.Parsers
{
    internal class BottomStyleParser : OffsetStyleParser
    {
        public new BottomStyleDescriptor Descriptor { get; } = new BottomStyleDescriptor();

        public BottomStyleParser(string content)
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
