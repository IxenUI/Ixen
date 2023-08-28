namespace Ixen.Core.Visual.Computers
{
    internal class ClippingComputer
    {
        internal void Compute(VisualElement element)
        {
            ComputeElementClip(element);

            foreach (VisualElement child in element.Children)
            {
                Compute(child);
            }
        }

        private void ComputeElementClip(VisualElement element)
        {
            var res = new DimensionalElement(element);
            var parent = element.Parent;

            while (parent != null)
            {
                if (parent.Clip != null)
                {
                    res = res.Intersect(parent.Clip);
                    break;
                }

                res = res.Intersect(parent);
                parent = parent.Parent;
            }

            element.Clip = res;
        }
    }
}
