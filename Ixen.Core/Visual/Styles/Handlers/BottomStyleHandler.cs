using Ixen.Core.Visual.Styles.Descriptors;

namespace Ixen.Core.Visual.Styles.Handlers
{
    internal class BottomStyleHandler : StyleHandler
    {
        public BottomStyleDescriptor Descriptor { get; private set; }

        public BottomStyleHandler()
            : this(new())
        { }

        public BottomStyleHandler(BottomStyleDescriptor descriptor)
            : base()
        {
            Descriptor = descriptor;
        }
    }
}
