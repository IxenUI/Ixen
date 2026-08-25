using Ixen.Core.Visual.Styles.Descriptors;

namespace Ixen.Core.Visual.Styles.Parsers
{
    internal class MaxHeightStyleParser : BoundStyleParser
    {
        public new MaxHeightStyleDescriptor Descriptor { get; } = new MaxHeightStyleDescriptor();

        public MaxHeightStyleParser(string content)
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
