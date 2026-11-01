using Ixen.Core.Visual;

namespace Ixen.Core.Rendering
{
    internal static class InspectorRenderer
    {
        private const float PLATE_PADDING = 4f;
        private const float PLATE_GAP = 2f;
        private const float LABEL_SIZE = 11f;

        private static readonly Color _content = new Color("#552D7FF9");
        private static readonly Color _padding = new Color("#553DDC84");
        private static readonly Color _plate = new Color("#F2101216");
        private static readonly Brush _text = new Brush(new Color("#FFE8ECF5"));
        private static readonly Brush _path = new Brush(new Color("#FF8FA3C8"));
        private static readonly Pen _outline = new Pen(new Color("#FF2D7FF9"), 1f, false);

        private const int PINNED_PATH_SEGMENTS = 5;

        internal static void Render(RendererContext context, VisualElement element, bool pinned)
        {
            if (element == null)
            {
                return;
            }

            float x = element.X;
            float y = element.Y;
            float width = element.ActualWidth;
            float height = element.ActualHeight;

            float contentX = element.ContentX;
            float contentY = element.ContentY;
            float contentWidth = element.ContentWidth;
            float contentHeight = element.ContentHeight;

            context.FillRectangle(x, y, width, contentY - y, _padding);
            context.FillRectangle(x, contentY + contentHeight, width,
                y + height - contentY - contentHeight, _padding);
            context.FillRectangle(x, contentY, contentX - x, contentHeight, _padding);
            context.FillRectangle(contentX + contentWidth, contentY,
                x + width - contentX - contentWidth, contentHeight, _padding);

            context.FillRectangle(contentX, contentY, contentWidth, contentHeight, _content);

            context.DrawInnerRectangle(x, y, width, height, _outline);

            Plate(context, element, x, y, width, height, pinned);
        }

        private static void Plate(RendererContext context, VisualElement element,
            float x, float y, float width, float height, bool pinned)
        {
            string label = InspectorHit.Describe(element, width, height);
            string path = pinned ? InspectorHit.PathOf(element, PINNED_PATH_SEGMENTS) : null;
            var spec = new FontSpec(null, LABEL_SIZE, false, false);

            float lineHeight = context.GetLineHeight(spec);
            float textWidth = context.MeasureTextWidth(label, spec);

            if (path != null)
            {
                textWidth = System.Math.Max(textWidth, context.MeasureTextWidth(path, spec));
            }

            float plateWidth = textWidth + (PLATE_PADDING * 2);
            float plateHeight = (path == null ? lineHeight : lineHeight * 2) + (PLATE_PADDING * 2);

            float plateY = y - plateHeight - PLATE_GAP;

            if (plateY < 0)
            {
                plateY = y + height + PLATE_GAP;
            }

            context.FillRectangle(x, plateY, plateWidth, plateHeight, _plate);
            context.DrawText(label, x + PLATE_PADDING, plateY + PLATE_PADDING, spec, _text);

            if (path != null)
            {
                context.DrawText(path, x + PLATE_PADDING, plateY + PLATE_PADDING + lineHeight, spec, _path);
            }
        }
    }
}
