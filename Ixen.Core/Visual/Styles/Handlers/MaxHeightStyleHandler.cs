using Ixen.Core.Visual.Styles.Descriptors;

namespace Ixen.Core.Visual.Styles.Handlers
{
    internal class MaxHeightStyleHandler : StyleHandler
    {
        public MaxHeightStyleDescriptor Descriptor { get; private set; }

        public MaxHeightStyleHandler()
            : this(new())
        { }

        public MaxHeightStyleHandler(MaxHeightStyleDescriptor descriptor)
            : base()
        {
            Descriptor = descriptor;
        }
    }
}
