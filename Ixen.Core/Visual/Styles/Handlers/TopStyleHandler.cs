using Ixen.Core.Visual.Styles.Descriptors;

namespace Ixen.Core.Visual.Styles.Handlers
{
    internal class TopStyleHandler : StyleHandler
    {
        public TopStyleDescriptor Descriptor { get; private set; }

        public TopStyleHandler()
            : this(new())
        { }

        public TopStyleHandler(TopStyleDescriptor descriptor)
            : base()
        {
            Descriptor = descriptor;
        }
    }
}
