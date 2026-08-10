using Ixen.Core.Visual.Styles.Descriptors;

namespace Ixen.Core.Visual.Styles.Handlers
{
    internal class CornerRadiusStyleHandler : StyleHandler
    {
        public CornerRadiusStyleDescriptor Descriptor { get; private set; }

        public CornerRadiusStyleHandler()
            : this(new())
        { }

        public CornerRadiusStyleHandler(CornerRadiusStyleDescriptor descriptor)
            : base()
        {
            Descriptor = descriptor;
        }
    }
}
