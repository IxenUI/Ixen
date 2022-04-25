using System.Collections.Generic;

namespace Ixen.Core.Visual
{
    public sealed class VisualElement : BoxedElement
    {
        internal List<VisualElement> Children { get; private set; } = new();
        internal ViewPort ViewPort { get; private set; } = new();
        
        internal float TotalWidthWeight { get; set; }
        internal float TotalHeightWeight { get; set; }
        internal bool TotalWeightSet { get; set; } = false;

        internal VisualElement Parent { get; private set; }
        internal bool IsRendered { get; private set; }

        public string Id { get; set; }
        public string Name { get; set; }
        public VisualElementStyles Styles { get; set; } = new();

        public void AddChild(VisualElement element)
        {
            element.Parent = this;
            Children.Add(element);
        }

        public void AddChildren(params VisualElement[] elements)
        {
            foreach (VisualElement element in elements)
            {
                AddChild(element);
            }
        }

        public void RemoveChild(VisualElement element)
        {
            if (Children.Remove(element))
            {
                element.Parent = null;
            }
        }

        internal void Invalidate()
        {
            IsRendered = false;
        }
    }
}
