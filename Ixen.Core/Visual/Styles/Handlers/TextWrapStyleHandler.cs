using Ixen.Core.Visual.Styles.Descriptors;

namespace Ixen.Core.Visual.Styles.Handlers
{
    internal class TextWrapStyleHandler : StyleHandler
    {
        public TextWrapStyleDescriptor Descriptor { get; private set; }

        public TextWrapStyleHandler()
            : this(new())
        { }

        public TextWrapStyleHandler(TextWrapStyleDescriptor descriptor)
            : base()
        {
            Descriptor = descriptor;
        }
    }
}
