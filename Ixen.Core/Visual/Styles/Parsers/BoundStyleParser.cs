using Ixen.Core.Visual.Styles.Descriptors;

namespace Ixen.Core.Visual.Styles.Parsers
{
    internal class BoundStyleParser : SizeStyleParser
    {
        public new BoundStyleDescriptor Descriptor { get; } = new BoundStyleDescriptor();

        public BoundStyleParser(string content)
            : base(content)
        { }

        protected override bool Parse()
        {
            if (!base.Parse() || base.Descriptor.Unit != SizeUnit.Pixels)
            {
                return false;
            }

            Descriptor.Set(base.Descriptor);
            return true;
        }
    }
}
