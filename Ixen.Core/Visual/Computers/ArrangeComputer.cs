using Ixen.Core.Visual.Styles.Descriptors;

namespace Ixen.Core.Visual.Computers
{
    internal class ArrangeComputer
    {
        internal void Arrange(VisualElement element, float x, float y)
        {
            element.SetPosition(x, y);

            LayoutStyleDescriptor layoutStyle = element.StylesHandlers.Layout.Descriptor;
            LayoutType type = layoutStyle != null ? layoutStyle.Type : LayoutType.Column;

            if (type == LayoutType.Grid)
            {
                ArrangeGrid(element);
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
            float x = originX;
            float y = ContentOriginY(element);
            int column = 0;
            int row = 0;

            foreach (VisualElement child in element.Children)
            {
                if (column == columns.Length)
                {
                    column = 0;
                    x = originX;
                    y += rows[row];
                    row++;

                    if (row == rows.Length)
                    {
                        return;
                    }
                }

                Arrange(child, x, y);
                x += columns[column];
                column++;
            }
        }
    }
}
