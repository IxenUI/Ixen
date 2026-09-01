using Ixen.Core.Rendering;
using Ixen.Core.Visual.Styles.Descriptors;

namespace Ixen.Core.Visual.Styles.Handlers
{
    internal class BorderStyleHandler : RenderedStyleHandler
    {
        public BorderStyleDescriptor Descriptor { get; private set; }

        private Pen _pen;
        private Color _color = Color.Black;

        private readonly Color _top;
        private readonly Color _right;
        private readonly Color _bottom;
        private readonly Color _left;

        private readonly string _colorSource;
        private readonly string _topSource;
        private readonly string _rightSource;
        private readonly string _bottomSource;
        private readonly string _leftSource;
        private readonly float _thickness;

        public BorderStyleHandler()
            : this(new())
        { }

        public BorderStyleHandler(BorderStyleDescriptor descriptor)
        {
            Descriptor = descriptor;

            _color = new Color(descriptor.Color);
            _pen = new Pen(_color, descriptor.Top);

            _top = new Color(descriptor.ColorTop);
            _right = new Color(descriptor.ColorRight);
            _bottom = new Color(descriptor.ColorBottom);
            _left = new Color(descriptor.ColorLeft);

            _colorSource = descriptor.Color;
            _topSource = descriptor.TopColor;
            _rightSource = descriptor.RightColor;
            _bottomSource = descriptor.BottomColor;
            _leftSource = descriptor.LeftColor;
            _thickness = descriptor.Top;
        }

        internal static BorderStyleHandler For(BorderStyleDescriptor descriptor)
        {
            if (descriptor.Handler is BorderStyleHandler handler && handler.IsCurrent)
            {
                return handler;
            }

            handler = new BorderStyleHandler(descriptor);

            descriptor.Handler = handler;

            return handler;
        }

        private bool IsCurrent => _colorSource == Descriptor.Color
            && _thickness == Descriptor.Top
            && _topSource == Descriptor.TopColor
            && _rightSource == Descriptor.RightColor
            && _bottomSource == Descriptor.BottomColor
            && _leftSource == Descriptor.LeftColor;

        internal Color Color => _color;

        internal override void Render(VisualElement element, RendererContext context)
        {
            if (!Descriptor.HasBorder)
            {
                return;
            }

            Pen pen = element.AnimatedPen(StyleIdentifier.BORDER, _pen) ?? _pen;
            CornerRadiusStyleDescriptor radius = element.StylesHandlers.CornerRadius.Descriptor;

            if (!Descriptor.IsUniform || !Descriptor.IsOneColor)
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

            float innerLeft = element.X + insideLeft;
            float innerTop = element.Y + insideTop;
            float innerRight = element.X + element.ActualWidth - insideRight;
            float innerBottom = element.Y + element.ActualHeight - insideBottom;

            if (Descriptor.IsOneColor)
            {
                if (Descriptor.Top > 0)
                {
                    context.FillRectangle(left, top, right - left, innerTop - top, color);
                }

                if (Descriptor.Bottom > 0)
                {
                    context.FillRectangle(left, innerBottom, right - left, bottom - innerBottom, color);
                }

                if (Descriptor.Left > 0)
                {
                    context.FillRectangle(left, top, innerLeft - left, bottom - top, color);
                }

                if (Descriptor.Right > 0)
                {
                    context.FillRectangle(innerRight, top, right - innerRight, bottom - top, color);
                }
            }
            else
            {
                if (Descriptor.Top > 0)
                {
                    context.FillQuad(left, top, right, top, innerRight, innerTop, innerLeft, innerTop, _top);
                }

                if (Descriptor.Right > 0)
                {
                    context.FillQuad(right, top, right, bottom, innerRight, innerBottom, innerRight, innerTop, _right);
                }

                if (Descriptor.Bottom > 0)
                {
                    context.FillQuad(right, bottom, left, bottom, innerLeft, innerBottom, innerRight, innerBottom, _bottom);
                }

                if (Descriptor.Left > 0)
                {
                    context.FillQuad(left, bottom, left, top, innerLeft, innerTop, innerLeft, innerBottom, _left);
                }
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
