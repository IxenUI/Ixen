using Ixen.Core.Visual.Styles.Descriptors;

namespace Ixen.Core.Visual.Styles.Parsers
{
    internal class BackdropFilterStyleParser : FilterStyleParser
    {
        public new BackdropFilterStyleDescriptor Descriptor { get; } = new BackdropFilterStyleDescriptor();

        public BackdropFilterStyleParser(string content)
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
