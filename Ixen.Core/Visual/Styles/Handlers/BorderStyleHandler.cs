using Ixen.Core.Rendering;
using Ixen.Core.Visual.Styles.Descriptors;
using System.Runtime.CompilerServices;

namespace Ixen.Core.Visual.Styles.Handlers
{
    internal class BorderStyleHandler : RenderedStyleHandler
    {
        public BorderStyleDescriptor Descriptor { get; private set; }

        private Pen _pen;
        private Color _color = Color.Black;

        private readonly string _colorSource;
        private readonly float _thickness;

        private static readonly ConditionalWeakTable<BorderStyleDescriptor, BorderStyleHandler> _built =
            new ConditionalWeakTable<BorderStyleDescriptor, BorderStyleHandler>();

        public BorderStyleHandler()
            : this(new())
        { }

        public BorderStyleHandler(BorderStyleDescriptor descriptor)
        {
            Descriptor = descriptor;

            _color = new Color(descriptor.Color);
            _pen = new Pen(_color, descriptor.Top);

            _colorSource = descriptor.Color;
            _thickness = descriptor.Top;
        }

        internal static BorderStyleHandler For(BorderStyleDescriptor descriptor)
        {
            if (_built.TryGetValue(descriptor, out BorderStyleHandler handler) && handler.IsCurrent)
            {
                return handler;
            }

            handler = new BorderStyleHandler(descriptor);

            _built.Remove(descriptor);
            _built.Add(descriptor, handler);

            return handler;
        }

        private bool IsCurrent => _colorSource == Descriptor.Color && _thickness == Descriptor.Top;

        internal Color Color => _color;

        internal override void Render(VisualElement element, RendererContext context)
        {
            if (!Descriptor.HasBorder)
            {
                return;
            }

            Pen pen = element.AnimatedPen(StyleIdentifier.BORDER, _pen) ?? _pen;
            CornerRadiusStyleDescriptor radius = element.StylesHandlers.CornerRadius.Descriptor;

            if (!Descriptor.IsUniform)
            {
                RenderSides(element, context, pen.Color, radius);
                return;
            }

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

        private void RenderSides(VisualElement element, RendererContext context, Color color,
            CornerRadiusStyleDescriptor radius)
        {
            float insideTop = Inside(Descriptor.Top);
            float insideRight = Inside(Descriptor.Right);
            float insideBottom = Inside(Descriptor.Bottom);
            float insideLeft = Inside(Descriptor.Left);

            float left = element.X - Outside(Descriptor.Left);
            float top = element.Y - Outside(Descriptor.Top);
            float right = element.X + element.ActualWidth + Outside(Descriptor.Right);
            float bottom = element.Y + element.ActualHeight + Outside(Descriptor.Bottom);

            bool clipped = radius.HasRadius;

            if (clipped)
            {
                context.PushClip(element.X, element.Y, element.ActualWidth, element.ActualHeight, radius);
            }

            if (Descriptor.Top > 0)
            {
                context.FillRectangle(left, top, right - left, element.Y + insideTop - top, color);
            }

            if (Descriptor.Bottom > 0)
            {
                float edge = element.Y + element.ActualHeight - insideBottom;
                context.FillRectangle(left, edge, right - left, bottom - edge, color);
            }

            if (Descriptor.Left > 0)
            {
                context.FillRectangle(left, top, element.X + insideLeft - left, bottom - top, color);
            }

            if (Descriptor.Right > 0)
            {
                float edge = element.X + element.ActualWidth - insideRight;
                context.FillRectangle(edge, top, right - edge, bottom - top, color);
            }

            if (clipped)
            {
                context.PopClip();
            }
        }

        private float Inside(float thickness)
        {
            if (thickness <= 0)
            {
                return 0;
            }

            switch (Descriptor.Type)
            {
                case BorderType.Inner:
                    return thickness;

                case BorderType.Outer:
                    return 0;

                default:
                    return thickness / 2;
            }
        }

        private float Outside(float thickness)
        {
            if (thickness <= 0)
            {
                return 0;
            }

            switch (Descriptor.Type)
            {
                case BorderType.Inner:
                    return 0;

                case BorderType.Outer:
                    return thickness;

                default:
                    return thickness / 2;
            }
        }
    }
}
