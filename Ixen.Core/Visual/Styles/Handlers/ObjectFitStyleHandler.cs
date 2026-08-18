using Ixen.Core.Visual.Styles.Descriptors;

namespace Ixen.Core.Visual.Styles.Handlers
{
    internal class ObjectFitStyleHandler : StyleHandler
    {
        public ObjectFitStyleDescriptor Descriptor { get; private set; }

        public ObjectFitStyleHandler()
            : this(new())
        { }

        public ObjectFitStyleHandler(ObjectFitStyleDescriptor descriptor)
            : base()
        {
            Descriptor = descriptor;
        }
    }
}
