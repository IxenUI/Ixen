using Ixen.Core.Rendering;
using Ixen.Core.Visual.Classes;
using Ixen.Core.Visual.Styles.Descriptors;
using System;

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
        private readonly int _tokens;
        private readonly BorderStyle _style;

        public BorderStyleHandler()
            : this(new(), null)
        { }

        public BorderStyleHandler(BorderStyleDescriptor descriptor, StyleTokens tokens)
        {
            Descriptor = descriptor;

            _color = new Color(StyleColors.Resolve(tokens, descriptor.Color));
            _pen = new Pen(_color, descriptor.Top, descriptor.Style);

            _top = new Color(StyleColors.Resolve(tokens, descriptor.ColorTop));
            _right = new Color(StyleColors.Resolve(tokens, descriptor.ColorRight));
            _bottom = new Color(StyleColors.Resolve(tokens, descriptor.ColorBottom));
            _left = new Color(StyleColors.Resolve(tokens, descriptor.ColorLeft));

            _colorSource = descriptor.Color;
            _topSource = descriptor.TopColor;
            _rightSource = descriptor.RightColor;
            _bottomSource = descriptor.BottomColor;
            _leftSource = descriptor.LeftColor;
            _thickness = descriptor.Top;
            _style = descriptor.Style;
            _tokens = StyleColors.VersionOf(tokens);
        }

        internal static BorderStyleHandler For(BorderStyleDescriptor descriptor, StyleTokens tokens)
        {
            if (descriptor.Handler is BorderStyleHandler handler && handler.IsCurrent(tokens))
            {
                return handler;
            }

            handler = new BorderStyleHandler(descriptor, tokens);

            descriptor.Handler = handler;

            return handler;
        }

        private bool IsCurrent(StyleTokens tokens) => _tokens == StyleColors.VersionOf(tokens)
            && _colorSource == Descriptor.Color
            && _thickness == Descriptor.Top
            && _topSource == Descriptor.TopColor
            && _rightSource == Descriptor.RightColor
            && _bottomSource == Descriptor.BottomColor
            && _leftSource == Descriptor.LeftColor
            && _style == Descriptor.Style;

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

            if (radius.HasRadius && Descriptor.Style == BorderStyle.Solid)
            {
                context.FillRing(element.X, element.Y, element.ActualWidth, element.ActualHeight, radius,
                    element.BorderOutside, element.BorderInside, pen.Color);
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

            if (clipped && Descriptor.IsOneColor && Descriptor.Style == BorderStyle.Solid)
            {
                context.FillRing(element.X, element.Y, element.ActualWidth, element.ActualHeight, radius,
                    element.BorderOutside, element.BorderInside, color);
                return;
            }

            if (clipped)
            {
                context.PushRing(element.X, element.Y, element.ActualWidth, element.ActualHeight, radius,
                    element.BorderOutside, element.BorderInside);
            }

            if (Descriptor.Style != BorderStyle.Solid)
            {
                RenderDashedSides(element, context, left, top, right, bottom);

                if (clipped)
                {
                    context.PopClip();
                }

                return;
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
                float tlX = innerLeft, tlY = innerTop, trX = innerRight, trY = innerTop;
                float brX = innerRight, brY = innerBottom, blX = innerLeft, blY = innerBottom;

                if (clipped)
                {
                    float scale = radius.ScaleFor(element.ActualWidth, element.ActualHeight);

                    Mitre(left, top, ref tlX, ref tlY, radius.TopLeft * scale, insideLeft, insideTop);
                    Mitre(right, top, ref trX, ref trY, radius.TopRight * scale, insideRight, insideTop);
                    Mitre(right, bottom, ref brX, ref brY, radius.BottomRight * scale, insideRight, insideBottom);
                    Mitre(left, bottom, ref blX, ref blY, radius.BottomLeft * scale, insideLeft, insideBottom);
                }

                if (Descriptor.Top > 0)
                {
                    context.FillQuad(left, top, right, top, trX, trY, tlX, tlY, _top);
                }

                if (Descriptor.Right > 0)
                {
                    context.FillQuad(right, top, right, bottom, brX, brY, trX, trY, _right);
                }

                if (Descriptor.Bottom > 0)
                {
                    context.FillQuad(right, bottom, left, bottom, blX, blY, brX, brY, _bottom);
                }

                if (Descriptor.Left > 0)
                {
                    context.FillQuad(left, bottom, left, top, tlX, tlY, blX, blY, _left);
                }
            }

            if (clipped)
            {
                context.PopClip();
            }
        }

        private static void Mitre(float outerX, float outerY, ref float innerX, ref float innerY,
            float radius, float insideX, float insideY)
        {
            float radiusX = Math.Max(0, radius - insideX);
            float radiusY = Math.Max(0, radius - insideY);

            if (radiusX <= 0 && radiusY <= 0)
            {
                return;
            }

            float spanX = Math.Abs(innerX - outerX);
            float spanY = Math.Abs(innerY - outerY);

            float reach = Math.Max(spanX > 0 ? radiusX / spanX : 0, spanY > 0 ? radiusY / spanY : 0);

            innerX += (innerX - outerX) * reach;
            innerY += (innerY - outerY) * reach;
        }

        private void RenderDashedSides(VisualElement element, RendererContext context,
            float left, float top, float right, float bottom)
        {
            BorderStyle style = Descriptor.Style;

            float innerLeft = element.X + Inside(Descriptor.Left);
            float innerTop = element.Y + Inside(Descriptor.Top);
            float innerRight = element.X + element.ActualWidth - Inside(Descriptor.Right);
            float innerBottom = element.Y + element.ActualHeight - Inside(Descriptor.Bottom);

            float midTop = (top + innerTop) / 2;
            float midBottom = (bottom + innerBottom) / 2;
            float midLeft = (left + innerLeft) / 2;
            float midRight = (right + innerRight) / 2;

            context.DrawSide(left, midTop, right, midTop, Descriptor.Top, _top, style);
            context.DrawSide(left, midBottom, right, midBottom, Descriptor.Bottom, _bottom, style);
            context.DrawSide(midLeft, innerTop, midLeft, innerBottom, Descriptor.Left, _left, style);
            context.DrawSide(midRight, innerTop, midRight, innerBottom, Descriptor.Right, _right,
                style);
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
