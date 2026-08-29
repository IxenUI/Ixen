using Ixen.Core.Visual.Styles.Descriptors;

namespace Ixen.Core.Visual.Styles.Handlers
{
    internal class OverscrollStyleHandler : StyleHandler
    {
        public OverscrollStyleDescriptor Descriptor { get; private set; }

        public OverscrollStyleHandler()
            : this(new())
        { }

        public OverscrollStyleHandler(OverscrollStyleDescriptor descriptor)
            : base()
        {
            Descriptor = descriptor;
        }
    }
}
