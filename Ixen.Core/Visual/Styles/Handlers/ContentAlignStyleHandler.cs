using Ixen.Core.Visual.Styles.Descriptors;

namespace Ixen.Core.Visual.Styles.Handlers
{
    internal class ContentAlignStyleHandler : StyleHandler
    {
        public ContentAlignStyleDescriptor Descriptor { get; private set; }

        public ContentAlignStyleHandler()
            : this(new())
        { }

        public ContentAlignStyleHandler(ContentAlignStyleDescriptor descriptor)
            : base()
        {
            Descriptor = descriptor;
        }
    }
}
