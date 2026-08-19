using System.Collections.Generic;

namespace Ixen.Core.Visual.Computers
{
    internal class ClippingComputer
    {
        private float _viewportWidth;
        private float _viewportHeight;

        private List<VisualElement> _collected;

        internal void Compute(VisualElement element, float viewportWidth, float viewportHeight)
        {
            _viewportWidth = viewportWidth;
            _viewportHeight = viewportHeight;

            _collected = element.Overlays;
            _collected.Clear();

            Compute(element);

            _collected = null;
        }

        private void Compute(VisualElement element)
        {
            ComputeElementClip(element);

            foreach (VisualElement child in element.Children)
            {
                Compute(child);
            }

            if (!element.HasChrome)
            {
                return;
            }

            foreach (VisualElement chrome in element.Chrome)
            {
                Compute(chrome);
            }
        }

        private void ComputeElementClip(VisualElement element)
        {
            if (element.IsOverlay)
            {
                _collected?.Add(element);

                element.Clip = new DimensionalElement
                {
                    X = 0,
                    Y = 0,
                    Width = _viewportWidth,
                    Height = _viewportHeight
                };

                return;
            }

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
