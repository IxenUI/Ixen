using Ixen.Core.Visual.Styles.Descriptors;

namespace Ixen.Core.Visual.Styles.Handlers
{
    internal class OpacityStyleHandler : StyleHandler
    {
        public OpacityStyleDescriptor Descriptor { get; private set; }

        public OpacityStyleHandler()
            : this(new())
        { }

        public OpacityStyleHandler(OpacityStyleDescriptor descriptor)
            : base()
        {
            Descriptor = descriptor;
        }
    }
}
