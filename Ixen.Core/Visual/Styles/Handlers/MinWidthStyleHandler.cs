using Ixen.Core.Visual.Styles.Descriptors;

namespace Ixen.Core.Visual.Styles.Handlers
{
    internal class MinWidthStyleHandler : StyleHandler
    {
        public MinWidthStyleDescriptor Descriptor { get; private set; }

        public MinWidthStyleHandler()
            : this(new())
        { }

        public MinWidthStyleHandler(MinWidthStyleDescriptor descriptor)
            : base()
        {
            Descriptor = descriptor;
        }
    }
}
