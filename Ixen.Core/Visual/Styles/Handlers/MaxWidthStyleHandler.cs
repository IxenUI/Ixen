using Ixen.Core.Visual.Styles.Descriptors;

namespace Ixen.Core.Visual.Styles.Handlers
{
    internal class MaxWidthStyleHandler : StyleHandler
    {
        public MaxWidthStyleDescriptor Descriptor { get; private set; }

        public MaxWidthStyleHandler()
            : this(new())
        { }

        public MaxWidthStyleHandler(MaxWidthStyleDescriptor descriptor)
            : base()
        {
            Descriptor = descriptor;
        }
    }
}
