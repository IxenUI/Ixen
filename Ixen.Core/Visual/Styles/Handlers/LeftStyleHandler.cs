using Ixen.Core.Visual.Styles.Descriptors;

namespace Ixen.Core.Visual.Styles.Handlers
{
    internal class LeftStyleHandler : StyleHandler
    {
        public LeftStyleDescriptor Descriptor { get; private set; }

        public LeftStyleHandler()
            : this(new())
        { }

        public LeftStyleHandler(LeftStyleDescriptor descriptor)
            : base()
        {
            Descriptor = descriptor;
        }
    }
}
