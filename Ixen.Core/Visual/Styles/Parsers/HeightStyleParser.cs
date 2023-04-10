using Ixen.Core.Visual.Styles.Descriptors;

namespace Ixen.Core.Visual.Styles.Parsers
{
    internal class HeightStyleParser : SizeStyleParser
    {
        public new HeightStyleDescriptor Descriptor { get; } = new HeightStyleDescriptor();

        public HeightStyleParser(string content)
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
