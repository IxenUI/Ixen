using Ixen.Core.Rendering;
using Ixen.Core.Visual.Styles.Descriptors;

namespace Ixen.Core.Visual.Styles.Handlers
{
    public class BackgroundStyleHandler : RenderedStyleHandler
    {
        public BackgroundStyleDescriptor Descriptor { get; private set; }

        private Brush _brush;
        private Color _color = Color.Transparent;

        public BackgroundStyleHandler()
            : this(new())
        { }

        public BackgroundStyleHandler(BackgroundStyleDescriptor descriptor)
        {
            Descriptor = descriptor;
            _color = new Color(descriptor.Color);
            _brush = new Brush(_color);
        }

        internal override void Render(VisualElement element, RendererContext context)
        {
            context.FillRectangle(element.X, element.Y, element.ActualWidth, element.ActualHeight, _brush);
        }
    }
}
