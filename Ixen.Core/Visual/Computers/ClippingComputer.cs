using System.Collections.Generic;

namespace Ixen.Core.Visual.Computers
{
    internal class ClippingComputer
    {
        private float _viewportWidth;
        private float _viewportHeight;

        private List<VisualElement> _collected;
        private bool _layered;

        internal void Compute(VisualElement element, float viewportWidth, float viewportHeight)
        {
            _viewportWidth = viewportWidth;
            _viewportHeight = viewportHeight;
            _layered = false;

            _collected = element.Overlays;
            _collected.Clear();

            Compute(element);

            if (_layered)
            {
                SortByDepth(_collected);
            }

            _collected = null;
        }

        private static void SortByDepth(List<VisualElement> layers)
        {
            for (int i = 1; i < layers.Count; i++)
            {
                VisualElement moving = layers[i];
                int depth = DepthOf(moving);
                int j = i - 1;

                while (j >= 0 && DepthOf(layers[j]) > depth)
                {
                    layers[j + 1] = layers[j];
                    j--;
                }

                layers[j + 1] = moving;
            }
        }

        private static int DepthOf(VisualElement element)
            => element.StylesHandlers.ZIndex.Descriptor.Value;

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

                if (element.StylesHandlers.ZIndex.Descriptor.Value != 0)
                {
                    _layered = true;
                }

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
