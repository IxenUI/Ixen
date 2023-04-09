using Ixen.Core.Visual.Styles.Descriptors;

namespace Ixen.Core.Visual.Styles.Handlers
{
    public class WidthStyleHandler : StyleHandler
    {
        public WidthStyleDescriptor Descriptor { get; private set; }

        public WidthStyleHandler()
            : this(new())
        { }

        public WidthStyleHandler(WidthStyleDescriptor descriptor)
            : base()
        {
            Descriptor = descriptor;
        }
    }
}
