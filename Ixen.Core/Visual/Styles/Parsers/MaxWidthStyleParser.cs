using Ixen.Core.Visual.Styles.Descriptors;

namespace Ixen.Core.Visual.Styles.Parsers
{
    internal class MaxWidthStyleParser : BoundStyleParser
    {
        public new MaxWidthStyleDescriptor Descriptor { get; } = new MaxWidthStyleDescriptor();

        public MaxWidthStyleParser(string content)
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
