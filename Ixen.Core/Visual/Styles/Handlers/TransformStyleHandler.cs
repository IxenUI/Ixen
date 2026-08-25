using Ixen.Core.Visual.Styles.Descriptors;

namespace Ixen.Core.Visual.Styles.Handlers
{
    internal class TransformStyleHandler : StyleHandler
    {
        public TransformStyleDescriptor Descriptor { get; private set; }

        public TransformStyleHandler()
            : this(new())
        { }

        public TransformStyleHandler(TransformStyleDescriptor descriptor)
            : base()
        {
            Descriptor = descriptor;
        }
    }
}
