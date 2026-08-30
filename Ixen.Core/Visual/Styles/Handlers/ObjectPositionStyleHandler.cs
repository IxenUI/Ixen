using Ixen.Core.Visual.Styles.Descriptors;

namespace Ixen.Core.Visual.Styles.Handlers
{
    internal class ObjectPositionStyleHandler : StyleHandler
    {
        public ObjectPositionStyleDescriptor Descriptor { get; private set; }

        public ObjectPositionStyleHandler()
            : this(new())
        { }

        public ObjectPositionStyleHandler(ObjectPositionStyleDescriptor descriptor)
            : base()
        {
            Descriptor = descriptor;
        }
    }
}
