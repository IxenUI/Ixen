using Ixen.Core.Visual.Styles.Descriptors;

namespace Ixen.Core.Visual.Styles.Handlers
{
    internal class LetterSpacingStyleHandler : StyleHandler
    {
        public LetterSpacingStyleDescriptor Descriptor { get; private set; }

        public LetterSpacingStyleHandler()
            : this(new())
        { }

        public LetterSpacingStyleHandler(LetterSpacingStyleDescriptor descriptor)
            : base()
        {
            Descriptor = descriptor;
        }
    }
}
