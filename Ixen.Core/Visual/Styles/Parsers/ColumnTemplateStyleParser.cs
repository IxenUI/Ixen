using Ixen.Core.Visual.Styles.Descriptors;

namespace Ixen.Core.Visual.Styles.Parsers
{
    internal class ColumnTemplateStyleParser : SizeTemplateStyleParser
    {
        public new ColumnTemplateStyleDescriptor Descriptor { get; } = new();

        public ColumnTemplateStyleParser(string content)
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
