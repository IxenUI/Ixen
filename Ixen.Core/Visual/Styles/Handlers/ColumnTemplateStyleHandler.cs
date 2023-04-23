using Ixen.Core.Visual.Styles.Descriptors;

namespace Ixen.Core.Visual.Styles.Handlers
{
    internal class ColumnTemplateStyleHandler : StyleHandler
    {
        public ColumnTemplateStyleDescriptor Descriptor { get; private set; }

        public ColumnTemplateStyleHandler()
            : this(new())
        { }

        public ColumnTemplateStyleHandler(ColumnTemplateStyleDescriptor descriptor)
            : base()
        {
            Descriptor = descriptor;
        }
    }
}
