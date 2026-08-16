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

        internal Color Color => _color;

        internal override void Render(VisualElement element, RendererContext context)
        {
            if (Descriptor.Thickness == 0)
            {
                return;
            }

            Pen pen = element.AnimatedPen(StyleIdentifier.BORDER, _pen) ?? _pen;
            CornerRadiusStyleDescriptor radius = element.StylesHandlers.CornerRadius.Descriptor;

            if (radius.HasRadius)
            {
                context.DrawRoundRectangle(element.X, element.Y, element.ActualWidth, element.ActualHeight,
                    radius, pen, Descriptor.Type);
                return;
            }

            switch (Descriptor.Type)
            {
                case BorderType.Center:
                    context.DrawRectangle(element.X, element.Y, element.ActualWidth, element.ActualHeight, pen);
                    break;
                case BorderType.Inner:
                    context.DrawInnerRectangle(element.X, element.Y, element.ActualWidth, element.ActualHeight, pen);
                    break;
                case BorderType.Outer:
                    context.DrawOuterRectangle(element.X, element.Y, element.ActualWidth, element.ActualHeight, pen);
                    break;
            }
        }
    }
}
