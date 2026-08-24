using Ixen.Core.Visual.Styles.Descriptors;

namespace Ixen.Core.Visual.Styles.Handlers
{
    internal class AnchorStyleHandler : StyleHandler
    {
        public AnchorStyleDescriptor Descriptor { get; private set; }

        public AnchorStyleHandler()
            : this(new())
        { }

        public AnchorStyleHandler(AnchorStyleDescriptor descriptor)
            : base()
        {
            Descriptor = descriptor;
        }
    }
}
