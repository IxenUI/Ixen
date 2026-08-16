using Ixen.Core.Visual.Styles.Descriptors;

namespace Ixen.Core.Visual.Styles.Handlers
{
    internal class TransitionStyleHandler : StyleHandler
    {
        public TransitionStyleDescriptor Descriptor { get; private set; }

        public TransitionStyleHandler()
            : this(new())
        { }

        public TransitionStyleHandler(TransitionStyleDescriptor descriptor)
            : base()
        {
            Descriptor = descriptor;
        }
    }
}
