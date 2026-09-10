using Ixen.Core.Visual.Styles.Descriptors;

namespace Ixen.Core.Visual.Styles.Handlers
{
    internal class AreaTemplateStyleHandler : StyleHandler
    {
        public AreaTemplateStyleDescriptor Descriptor { get; private set; }

        public AreaTemplateStyleHandler()
            : this(new())
        { }

        public AreaTemplateStyleHandler(AreaTemplateStyleDescriptor descriptor)
            : base()
        {
            Descriptor = descriptor;
        }
    }

    internal class GridAreaStyleHandler : StyleHandler
    {
        public GridAreaStyleDescriptor Descriptor { get; private set; }

        public GridAreaStyleHandler()
            : this(new())
        { }

        public GridAreaStyleHandler(GridAreaStyleDescriptor descriptor)
            : base()
        {
            Descriptor = descriptor;
        }
    }
}
