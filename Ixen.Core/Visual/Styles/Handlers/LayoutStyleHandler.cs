using Ixen.Core.Visual.Styles.Descriptors;

namespace Ixen.Core.Visual.Styles.Handlers
{
    internal class LayoutStyleHandler : StyleHandler
    {
        public LayoutStyleDescriptor Descriptor { get; private set; }

        public LayoutStyleHandler()
            : this(new())
        { }

        public LayoutStyleHandler(LayoutStyleDescriptor descriptor)
            : base()
        {
            Descriptor = descriptor;
        }
    }
}
