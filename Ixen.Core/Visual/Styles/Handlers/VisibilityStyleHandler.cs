using Ixen.Core.Visual.Styles.Descriptors;

namespace Ixen.Core.Visual.Styles.Handlers
{
    internal class VisibilityStyleHandler : StyleHandler
    {
        public VisibilityStyleDescriptor Descriptor { get; private set; }

        public VisibilityStyleHandler()
            : this(new())
        { }

        public VisibilityStyleHandler(VisibilityStyleDescriptor descriptor)
            : base()
        {
            Descriptor = descriptor;
        }
    }
}
