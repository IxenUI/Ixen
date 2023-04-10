using Ixen.Core.Rendering;
using Ixen.Core.Visual.Styles.Descriptors;

namespace Ixen.Core.Visual.Styles.Handlers
{
    internal class BorderStyleHandler : RenderedStyleHandler
    {
        public BorderStyleDescriptor Descriptor { get; private set; }

        private Pen _pen;
        private Color _color = Color.Black;

        public BorderStyleHandler()
            : this(new())
        { }

        public BorderStyleHandler(BorderStyleDescriptor descriptor)
        {
            Descriptor = descriptor;

            _color = new Color(descriptor.Color);
            _pen = new Pen(_color, descriptor.Thickness);
        }

        internal override void Render(VisualElement element, RendererContext context)
        {
            switch (Descriptor.Type)
            {
                case BorderType.Center:
                    context.DrawRectangle(element.X, element.Y, element.ActualWidth, element.ActualHeight, _pen);
                    break;
                case BorderType.Inner:
                    context.DrawInnerRectangle(element.X, element.Y, element.ActualWidth, element.ActualHeight, _pen);
                    break;
                case BorderType.Outer:
                    context.DrawOuterRectangle(element.X, element.Y, element.ActualWidth, element.ActualHeight, _pen);
                    break;
            }
        }
    }
}
