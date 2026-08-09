using Ixen.Core.Rendering;
using Ixen.Core.Visual.Styles.Descriptors;

namespace Ixen.Core.Visual.Styles.Handlers
{
    internal class ColorStyleHandler : StyleHandler
    {
        private const string DEFAULT_COLOR = "#000000";

        public ColorStyleDescriptor Descriptor { get; private set; }
        public Brush Brush { get; private set; }

        public ColorStyleHandler()
            : this(new())
        { }

        public ColorStyleHandler(ColorStyleDescriptor descriptor)
            : base()
        {
            Descriptor = descriptor;

            string value = string.IsNullOrWhiteSpace(descriptor.Value)
                ? DEFAULT_COLOR
                : descriptor.Value;

            Brush = new Brush(new Color(value), true);
        }
    }
}
