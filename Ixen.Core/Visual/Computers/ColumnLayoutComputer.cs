namespace Ixen.Core.Visual.Computers
{
    internal class ColumnLayoutComputer
    {
        internal void Compute(VisualElement element)
        {
            float x = element.X;
            float y = element.Y;
            float topW = 0;

            foreach (VisualElement child in element.Children)
            {
                child.SetPosition(x, y);
                y += child.BoxHeight;

                if (child.BoxWidth > topW)
                {
                    topW = child.BoxWidth;
                }
            }

            if (!element.IsHeightComputed)
            {
                element.Height = y;
                element.IsHeightComputed = true;
            }

            if (!element.IsWidthComputed)
            {
                element.Width = topW;
                element.IsWidthComputed = true;
            }
        }
    }
}
