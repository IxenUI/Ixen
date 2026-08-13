using Ixen.Core.Visual.Styles.Descriptors;

namespace Ixen.Core.Visual.Styles.Handlers
{
    internal class DockStyleHandler : StyleHandler
    {
        public DockStyleDescriptor Descriptor { get; private set; }

        public DockStyleHandler()
            : this(new())
        { }

        public DockStyleHandler(DockStyleDescriptor descriptor)
            : base()
        {
            Descriptor = descriptor;
        }
    }
}
