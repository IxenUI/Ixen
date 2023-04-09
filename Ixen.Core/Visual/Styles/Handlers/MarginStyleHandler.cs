using Ixen.Core.Visual.Styles.Descriptors;

namespace Ixen.Core.Visual.Styles.Handlers
{
    public class MarginStyleHandler : StyleHandler
    {
        public MarginStyleDescriptor Descriptor { get; private set; }

        public MarginStyleHandler()
            : this(new())
        { }

        public MarginStyleHandler(MarginStyleDescriptor descriptor)
            : base()
        {
            Descriptor = descriptor;
        }
    }
}
