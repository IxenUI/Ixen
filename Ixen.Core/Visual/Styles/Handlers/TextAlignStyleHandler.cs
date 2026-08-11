using Ixen.Core.Visual.Styles.Descriptors;

namespace Ixen.Core.Visual.Styles.Handlers
{
    internal class TextAlignStyleHandler : StyleHandler
    {
        public TextAlignStyleDescriptor Descriptor { get; private set; }

        public TextAlignStyleHandler()
            : this(new())
        { }

        public TextAlignStyleHandler(TextAlignStyleDescriptor descriptor)
            : base()
        {
            Descriptor = descriptor;
        }
    }
}
