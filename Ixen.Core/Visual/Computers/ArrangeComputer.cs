using Ixen.Core.Visual.Styles.Descriptors;

namespace Ixen.Core.Visual.Computers
{
    internal class ArrangeComputer
    {
        internal void Arrange(VisualElement element, float x, float y)
        {
            element.SetPosition(x, y);

            ArrangeChrome(element);

            LayoutType type = MeasureComputer.LayoutTypeOf(element);

            if (type == LayoutType.Grid)
            {
                ArrangeGrid(element);
                return;
            }

            if (type == LayoutType.Absolute || type == LayoutType.Dock)
            {
                ArrangePlaced(element, ContentOriginX(element), ContentOriginY(element));
                return;
            }

            if (type == LayoutType.Fixed)
            {
                ArrangePlaced(element, 0, 0);
                return;
            }

            if (type != LayoutType.Row && type != LayoutType.Column)
            {
                return;
            }

            bool isRow = type == LayoutType.Row;
            float childX = ContentOriginX(element);
            float childY = ContentOriginY(element);

            foreach (VisualElement child in element.Children)
            {
                Arrange(child, childX, childY);

                if (isRow)
                {
                    childX += child.BoxWidth;
                }
                else
                {
                    childY += child.BoxHeight;
                }
            }
        }

        private void ArrangeChrome(VisualElement element)
        {
            if (!element.HasChrome)
            {
                return;
            }

            foreach (VisualElement chrome in element.Chrome)
            {
                Arrange(chrome, element.X + chrome.LayoutOffsetX, element.Y + chrome.LayoutOffsetY);
            }
        }

        private void ArrangePlaced(VisualElement element, float originX, float originY)
        {
            foreach (VisualElement child in element.Children)
            {
                Arrange(child, originX + child.LayoutOffsetX, originY + child.LayoutOffsetY);
            }
        }

        private static float ContentOriginX(VisualElement element)
            => element.X + element.PaddingLeft + element.BorderInsideLeft - element.ScrollX;

        private static float ContentOriginY(VisualElement element)
            => element.Y + element.PaddingTop + element.BorderInsideTop - element.ScrollY;

        private void ArrangeGrid(VisualElement element)
        {
            float[] columns = element.GridColumns;
            float[] rows = element.GridRows;

            if (columns == null || rows == null || columns.Length == 0 || rows.Length == 0)
            {
                return;
            }

            float originX = ContentOriginX(element);
            float originY = ContentOriginY(element);

            foreach (VisualElement child in element.Children)
            {
                if (child.GridColumn >= columns.Length || child.GridRow >= rows.Length)
                {
                    continue;
                }

                Arrange(child,
                    originX + Offset(columns, child.GridColumn),
                    originY + Offset(rows, child.GridRow));
            }
        }

        private static float Offset(float[] tracks, int index)
        {
            float offset = 0;

            for (int i = 0; i < index && i < tracks.Length; i++)
            {
                offset += tracks[i];
            }

            return offset;
        }
    }
}
