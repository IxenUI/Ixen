using Ixen.Core.Visual.Styles.Descriptors;

namespace Ixen.Core.Visual.Styles.Handlers
{
    internal class MaskStyleHandler : StyleHandler
    {
        public MaskStyleDescriptor Descriptor { get; private set; }

        public MaskStyleHandler()
            : this(new())
        { }

        public MaskStyleHandler(MaskStyleDescriptor descriptor)
            : base()
        {
            Descriptor = descriptor;
        }
    }
}
