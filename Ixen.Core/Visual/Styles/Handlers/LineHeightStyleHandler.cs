using Ixen.Core.Visual.Styles.Descriptors;

namespace Ixen.Core.Visual.Styles.Handlers
{
    internal class LineHeightStyleHandler : StyleHandler
    {
        public LineHeightStyleDescriptor Descriptor { get; private set; }

        public LineHeightStyleHandler()
            : this(new())
        { }

        public LineHeightStyleHandler(LineHeightStyleDescriptor descriptor)
            : base()
        {
            Descriptor = descriptor;
        }
    }
}
