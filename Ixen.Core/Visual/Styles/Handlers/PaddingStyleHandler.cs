using Ixen.Core.Visual.Styles.Descriptors;

namespace Ixen.Core.Visual.Styles.Handlers
{
    internal class PaddingStyleHandler : StyleHandler
    {
        public PaddingStyleDescriptor Descriptor { get; private set; }

        public PaddingStyleHandler()
            : this(new())
        { }

        public PaddingStyleHandler(PaddingStyleDescriptor descriptor)
            : base()
        {
            Descriptor = descriptor;
        }
    }
}
