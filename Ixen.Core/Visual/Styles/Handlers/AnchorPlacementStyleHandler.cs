using Ixen.Core.Visual.Styles.Descriptors;

namespace Ixen.Core.Visual.Styles.Handlers
{
    internal class AnchorPlacementStyleHandler : StyleHandler
    {
        public AnchorPlacementStyleDescriptor Descriptor { get; private set; }

        public AnchorPlacementStyleHandler()
            : this(new())
        { }

        public AnchorPlacementStyleHandler(AnchorPlacementStyleDescriptor descriptor)
            : base()
        {
            Descriptor = descriptor;
        }
    }
}
