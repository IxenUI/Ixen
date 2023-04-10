using Ixen.Core.Visual.Styles.Descriptors;

namespace Ixen.Core.Visual.Styles.Handlers
{
    internal class HeightStyleHandler : StyleHandler
    {
        public HeightStyleDescriptor Descriptor { get; private set; }

        public HeightStyleHandler()
            : this(new())
        { }

        public HeightStyleHandler(HeightStyleDescriptor descriptor)
            : base()
        {
            Descriptor = descriptor;
        }
    }
}
