using Ixen.Core.Visual.Styles.Descriptors;

namespace Ixen.Core.Visual.Styles.Handlers
{
    internal class FontStyleStyleHandler : StyleHandler
    {
        public FontStyleStyleDescriptor Descriptor { get; private set; }

        public FontStyleStyleHandler()
            : this(new())
        { }

        public FontStyleStyleHandler(FontStyleStyleDescriptor descriptor)
            : base()
        {
            Descriptor = descriptor;
        }
    }
}
