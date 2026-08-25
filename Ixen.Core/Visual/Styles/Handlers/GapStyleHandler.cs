using Ixen.Core.Visual.Styles.Descriptors;

namespace Ixen.Core.Visual.Styles.Handlers
{
    internal class GapStyleHandler : StyleHandler
    {
        public GapStyleDescriptor Descriptor { get; private set; }

        public GapStyleHandler()
            : this(new())
        { }

        public GapStyleHandler(GapStyleDescriptor descriptor)
            : base()
        {
            Descriptor = descriptor;
        }
    }
}
