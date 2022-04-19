using Ixen.Rendering;
using System.Collections.Generic;

namespace Ixen.Visual
{
    public class VisualElement : AbstractVisualElement
    {
        private List<VisualElement> _contents = new();

        internal VisualElement Parent { get; private set; }
        public string Id { get; set; }
        public string Name { get; set; }

        internal override void Compute(VisualElement container, DimensionalElement targetZone)
        {
            Styles.Compute(this, container, targetZone);

            foreach (VisualElement element in _contents)
            {
                element.Compute(this, element);
            }
        }

        internal override void Render(RendererContext context, ViewPort viewPort)
        {
            Styles.Render(this, context, viewPort);

            foreach (VisualElement element in _contents)
            {
                element.Render(context, viewPort);
            }
        }

        public void AddContent(VisualElement element)
        {
            element.Parent = this;
            _contents.Add(element);
        }

        public void AddContent(params VisualElement[] elements)
        {
            foreach (VisualElement element in elements)
            {
                AddContent(element);
            }
        }

        public void RemoveContent(VisualElement element)
        {
            if (_contents.Remove(element))
            {
                element.Parent = null;
            }
        }

        public IEnumerable<VisualElement> GetElements()
        {
            foreach (VisualElement element in _contents)
            {
                yield return element;
            }
        }
    }
}
