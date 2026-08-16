using Ixen.Core.Visual.Styles.Descriptors;

namespace Ixen.Core.Visual.Styles.Handlers
{
    internal class CursorStyleHandler : StyleHandler
    {
        public CursorStyleDescriptor Descriptor { get; private set; }

        public CursorStyleHandler()
            : this(new())
        { }

        public CursorStyleHandler(CursorStyleDescriptor descriptor)
            : base()
        {
            Descriptor = descriptor;
        }
    }
}
