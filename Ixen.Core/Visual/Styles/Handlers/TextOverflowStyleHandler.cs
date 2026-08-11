using Ixen.Core.Visual.Styles.Descriptors;

namespace Ixen.Core.Visual.Styles.Handlers
{
    internal class TextOverflowStyleHandler : StyleHandler
    {
        public TextOverflowStyleDescriptor Descriptor { get; private set; }

        public TextOverflowStyleHandler()
            : this(new())
        { }

        public TextOverflowStyleHandler(TextOverflowStyleDescriptor descriptor)
            : base()
        {
            Descriptor = descriptor;
        }
    }
}
