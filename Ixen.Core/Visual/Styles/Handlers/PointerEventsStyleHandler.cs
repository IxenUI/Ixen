using Ixen.Core.Visual.Styles.Descriptors;

namespace Ixen.Core.Visual.Styles.Handlers
{
    internal class PointerEventsStyleHandler : StyleHandler
    {
        public PointerEventsStyleDescriptor Descriptor { get; private set; }

        public PointerEventsStyleHandler()
            : this(new())
        { }

        public PointerEventsStyleHandler(PointerEventsStyleDescriptor descriptor)
            : base()
        {
            Descriptor = descriptor;
        }
    }
}
