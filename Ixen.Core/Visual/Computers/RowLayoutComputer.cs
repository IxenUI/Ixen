namespace Ixen.Core.Visual.Computers
{
    internal class RowLayoutComputer
    {
        internal void Compute(VisualElement element)
        {
            float x = element.X;
            float y = element.Y;

            foreach (VisualElement child in element.Children)
            {
                child.SetPosition(x, y);
                x += child.BoxWidth;
            }
        }
    }
}
