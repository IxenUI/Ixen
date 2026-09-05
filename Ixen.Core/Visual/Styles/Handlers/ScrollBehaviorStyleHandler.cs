using Ixen.Core.Visual.Styles.Descriptors;

namespace Ixen.Core.Visual.Styles.Handlers
{
    internal class ScrollBehaviorStyleHandler : StyleHandler
    {
        public ScrollBehaviorStyleDescriptor Descriptor { get; private set; }

        public ScrollBehaviorStyleHandler()
            : this(new())
        { }

        public ScrollBehaviorStyleHandler(ScrollBehaviorStyleDescriptor descriptor)
            : base()
        {
            Descriptor = descriptor;
        }
    }
}
