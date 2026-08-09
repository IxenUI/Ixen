using Ixen.Core.Visual.Styles.Descriptors;

namespace Ixen.Core.Visual.Styles.Handlers
{
    internal class FontFamilyStyleHandler : StyleHandler
    {
        public FontFamilyStyleDescriptor Descriptor { get; private set; }

        public FontFamilyStyleHandler()
            : this(new())
        { }

        public FontFamilyStyleHandler(FontFamilyStyleDescriptor descriptor)
            : base()
        {
            Descriptor = descriptor;
        }
    }
}
