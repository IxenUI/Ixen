using Ixen.Core.Visual.Styles.Descriptors;

namespace Ixen.Core.Visual.Styles.Parsers
{
    internal class LeftStyleParser : OffsetStyleParser
    {
        public new LeftStyleDescriptor Descriptor { get; } = new LeftStyleDescriptor();

        public LeftStyleParser(string content)
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
