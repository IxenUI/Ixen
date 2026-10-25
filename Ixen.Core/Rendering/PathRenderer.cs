using Ixen.Core.Visual;
using Ixen.Core.Visual.Shapes;
using Ixen.Core.Visual.Styles.Descriptors;

namespace Ixen.Core.Rendering
{
    internal class PathRenderer
    {
        private const float EPSILON = 0.01f;

        internal void Render(VisualElement element, RendererContext context)
        {
            if (!(element is Path shape))
            {
                return;
            }

            PathGeometry geometry = shape.Geometry;

            if (geometry == null || geometry.IsEmpty)
            {
                return;
            }

            float viewWidth = shape.ViewWidth;
            float viewHeight = shape.ViewHeight;

            if (viewWidth <= 0 || viewHeight <= 0)
            {
                return;
            }

            float boxWidth = element.ContentWidth;
            float boxHeight = element.ContentHeight;

            if (boxWidth <= 0 || boxHeight <= 0)
            {
                return;
            }

            float boxX = element.X + element.PaddingLeft + element.BorderInsideLeft;
            float boxY = element.Y + element.PaddingTop + element.BorderInsideTop;

            ImageRenderer.Resolve(FitOf(element), viewWidth, viewHeight, boxWidth, boxHeight,
                out float width, out float height);

            CornerRadiusStyleDescriptor radius = element.StylesHandlers.CornerRadius.Descriptor;

            bool rounded = radius.HasRadius;
            bool overflows = width > boxWidth + EPSILON || height > boxHeight + EPSILON;

            if (rounded)
            {
                context.PushClip(element.X, element.Y, element.ActualWidth, element.ActualHeight, radius,
                    element.BorderInside);
            }

            if (overflows)
            {
                context.PushClip(boxX, boxY, boxWidth, boxHeight, null);
            }

            ObjectPositionStyleDescriptor position = element.StylesHandlers.ObjectPosition.Descriptor;

            context.DrawPath(geometry,
                boxX + (boxWidth - width) * position.X,
                boxY + (boxHeight - height) * position.Y,
                width / viewWidth,
                height / viewHeight,
                shape.ViewLeft,
                shape.ViewTop,
                shape.StrokeWidth,
                element.StylesHandlers.Color.Brush.Color);

            if (overflows)
            {
                context.PopClip();
            }

            if (rounded)
            {
                context.PopClip();
            }
        }

        private static ObjectFit FitOf(VisualElement element)
        {
            ObjectFit fit = element.StylesHandlers.ObjectFit.Descriptor.Value;

            return fit == ObjectFit.None || fit == ObjectFit.ScaleDown ? ObjectFit.Contain : fit;
        }
    }
}
