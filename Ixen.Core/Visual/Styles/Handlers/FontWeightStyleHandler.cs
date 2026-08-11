using Ixen.Core.Visual.Styles.Descriptors;

namespace Ixen.Core.Visual.Styles.Handlers
{
    internal class FontWeightStyleHandler : StyleHandler
    {
        public FontWeightStyleDescriptor Descriptor { get; private set; }

        public FontWeightStyleHandler()
            : this(new())
        { }

        public FontWeightStyleHandler(FontWeightStyleDescriptor descriptor)
            : base()
        {
            Descriptor = descriptor;
        }
    }
}
