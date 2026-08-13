using Ixen.Core.Visual.Styles.Descriptors;

namespace Ixen.Core.Visual.Styles.Handlers
{
    internal class RightStyleHandler : StyleHandler
    {
        public RightStyleDescriptor Descriptor { get; private set; }

        public RightStyleHandler()
            : this(new())
        { }

        public RightStyleHandler(RightStyleDescriptor descriptor)
            : base()
        {
            Descriptor = descriptor;
        }
    }
}
