using Ixen.Core.Visual.Styles.Descriptors;

namespace Ixen.Core.Visual.Styles.Handlers
{
    internal class TextDecorationStyleHandler : StyleHandler
    {
        public TextDecorationStyleDescriptor Descriptor { get; private set; }

        public TextDecorationStyleHandler()
            : this(new())
        { }

        public TextDecorationStyleHandler(TextDecorationStyleDescriptor descriptor)
            : base()
        {
            Descriptor = descriptor;
        }
    }
}
