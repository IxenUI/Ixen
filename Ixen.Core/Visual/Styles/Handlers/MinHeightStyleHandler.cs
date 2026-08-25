using Ixen.Core.Visual.Styles.Descriptors;

namespace Ixen.Core.Visual.Styles.Handlers
{
    internal class MinHeightStyleHandler : StyleHandler
    {
        public MinHeightStyleDescriptor Descriptor { get; private set; }

        public MinHeightStyleHandler()
            : this(new())
        { }

        public MinHeightStyleHandler(MinHeightStyleDescriptor descriptor)
            : base()
        {
            Descriptor = descriptor;
        }
    }
}
