using Ixen.Core.Visual.Styles.Descriptors;

namespace Ixen.Core.Visual.Styles.Handlers
{
    internal class TextShadowStyleHandler : StyleHandler
    {
        public TextShadowStyleDescriptor Descriptor { get; private set; }

        public TextShadowStyleHandler()
            : this(new())
        { }

        public TextShadowStyleHandler(TextShadowStyleDescriptor descriptor)
            : base()
        {
            Descriptor = descriptor;
        }
    }
}
