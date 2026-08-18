using Ixen.Core.Visual.Styles.Descriptors;

namespace Ixen.Core.Visual.Styles.Handlers
{
    internal class AnimationStyleHandler : StyleHandler
    {
        public AnimationStyleDescriptor Descriptor { get; private set; }

        public AnimationStyleHandler()
            : this(new())
        { }

        public AnimationStyleHandler(AnimationStyleDescriptor descriptor)
            : base()
        {
            Descriptor = descriptor;
        }
    }
}
