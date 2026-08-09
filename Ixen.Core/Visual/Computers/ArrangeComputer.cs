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

            if (type != LayoutType.Row && type != LayoutType.Column)
            {
                return;
            }

            bool isRow = type == LayoutType.Row;
            float childX = element.X + element.PaddingLeft;
            float childY = element.Y + element.PaddingTop;

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
    }
}
