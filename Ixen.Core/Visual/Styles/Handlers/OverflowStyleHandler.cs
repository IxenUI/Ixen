using Ixen.Core.Visual.Styles.Descriptors;

namespace Ixen.Core.Visual.Styles.Handlers
{
    internal class OverflowStyleHandler : StyleHandler
    {
        public OverflowStyleDescriptor Descriptor { get; private set; }

        public OverflowStyleHandler()
            : this(new())
        { }

        public OverflowStyleHandler(OverflowStyleDescriptor descriptor)
            : base()
        {
            Descriptor = descriptor;
        }
    }
}
