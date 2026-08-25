using Ixen.Core.Visual.Styles.Descriptors;

namespace Ixen.Core.Visual.Styles.Handlers
{
    internal class BoxShadowStyleHandler : StyleHandler
    {
        public BoxShadowStyleDescriptor Descriptor { get; private set; }

        public BoxShadowStyleHandler()
            : this(new())
        { }

        public BoxShadowStyleHandler(BoxShadowStyleDescriptor descriptor)
            : base()
        {
            Descriptor = descriptor;
        }
    }
}
