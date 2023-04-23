using Ixen.Core.Visual.Styles.Descriptors;

namespace Ixen.Core.Visual.Styles.Handlers
{
    internal class RowTemplateStyleHandler : StyleHandler
    {
        public RowTemplateStyleDescriptor Descriptor { get; private set; }

        public RowTemplateStyleHandler()
            : this(new())
        { }

        public RowTemplateStyleHandler(RowTemplateStyleDescriptor descriptor)
            : base()
        {
            Descriptor = descriptor;
        }
    }
}
