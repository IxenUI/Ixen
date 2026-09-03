using Ixen.Core.Visual.Styles.Descriptors;

namespace Ixen.Core.Visual.Styles.Handlers
{
    internal class AspectRatioStyleHandler : StyleHandler
    {
        public AspectRatioStyleDescriptor Descriptor { get; private set; }

        public AspectRatioStyleHandler()
            : this(new())
        { }

        public AspectRatioStyleHandler(AspectRatioStyleDescriptor descriptor)
            : base()
        {
            Descriptor = descriptor;
        }
    }
}
