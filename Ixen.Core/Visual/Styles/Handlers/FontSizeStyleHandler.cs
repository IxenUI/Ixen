using Ixen.Core.Visual.Styles.Descriptors;

namespace Ixen.Core.Visual.Styles.Handlers
{
    internal class FontSizeStyleHandler : StyleHandler
    {
        public FontSizeStyleDescriptor Descriptor { get; private set; }

        public FontSizeStyleHandler()
            : this(new())
        { }

        public FontSizeStyleHandler(FontSizeStyleDescriptor descriptor)
            : base()
        {
            Descriptor = descriptor;
        }
    }
}
