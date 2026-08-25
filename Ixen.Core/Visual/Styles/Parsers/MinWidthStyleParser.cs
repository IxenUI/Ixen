using Ixen.Core.Visual.Styles.Descriptors;

namespace Ixen.Core.Visual.Styles.Parsers
{
    internal class MinWidthStyleParser : BoundStyleParser
    {
        public new MinWidthStyleDescriptor Descriptor { get; } = new MinWidthStyleDescriptor();

        public MinWidthStyleParser(string content)
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
