using Ixen.Core.Visual.Styles.Descriptors;

namespace Ixen.Core.Visual.Styles.Parsers
{
    internal class RowTemplateStyleParser : SizeTemplateStyleParser
    {
        public new RowTemplateStyleDescriptor Descriptor { get; } = new();

        public RowTemplateStyleParser(string content)
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
