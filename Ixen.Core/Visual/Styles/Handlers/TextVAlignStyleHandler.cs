using Ixen.Core.Visual.Styles.Descriptors;

namespace Ixen.Core.Visual.Styles.Handlers
{
    internal class TextVAlignStyleHandler : StyleHandler
    {
        public TextVAlignStyleDescriptor Descriptor { get; private set; }

        public TextVAlignStyleHandler()
            : this(new())
        { }

        public TextVAlignStyleHandler(TextVAlignStyleDescriptor descriptor)
            : base()
        {
            Descriptor = descriptor;
        }
    }
}
