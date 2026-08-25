using Ixen.Core.Visual.Styles.Descriptors;

namespace Ixen.Core.Visual.Styles.Handlers
{
    internal class TransformOriginStyleHandler : StyleHandler
    {
        public TransformOriginStyleDescriptor Descriptor { get; private set; }

        public TransformOriginStyleHandler()
            : this(new())
        { }

        public TransformOriginStyleHandler(TransformOriginStyleDescriptor descriptor)
            : base()
        {
            Descriptor = descriptor;
        }
    }
}
