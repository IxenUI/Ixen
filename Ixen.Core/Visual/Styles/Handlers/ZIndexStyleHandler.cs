using Ixen.Core.Visual.Styles.Descriptors;

namespace Ixen.Core.Visual.Styles.Handlers
{
    internal class ZIndexStyleHandler : StyleHandler
    {
        public ZIndexStyleDescriptor Descriptor { get; private set; }

        public ZIndexStyleHandler()
            : this(new())
        { }

        public ZIndexStyleHandler(ZIndexStyleDescriptor descriptor)
            : base()
        {
            Descriptor = descriptor;
        }
    }
}
