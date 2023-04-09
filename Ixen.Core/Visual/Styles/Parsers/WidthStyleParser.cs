using Ixen.Core.Visual.Styles.Descriptors;

namespace Ixen.Core.Visual.Styles.Parsers
{
    public class WidthStyleParser : SizeStyleParser
    {
        public new WidthStyleDescriptor Descriptor { get; } = new WidthStyleDescriptor();

        public WidthStyleParser(string content)
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
