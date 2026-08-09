using System.Collections.Generic;

namespace Ixen.Core.Visual
{
    public class VisualElement : BoxedElement
    {
        internal int ChildIndex { get; set; }
        internal List<VisualElement> Children { get; private set; } = new();
        internal ViewPort ViewPort { get; private set; } = new();
        
        internal VisualElement Parent { get; private set; }
        internal DimensionalElement Clip { get; set; }
        internal bool MustRefreshStyles { get; set; } = true;

        public string Id { get; set; }
        public string Name { get; set; }
        public string TypeName { get; set; }
        public VisualElementStylesDescriptors Styles { get; set; } = new();
        internal VisualElementStylesHandlers StylesHandlers { get; set; } = new();
        public List<string> Classes { get; set; } = new ();

        public void AddChild(VisualElement element)
        {
            element.Parent = this;
            element.Invalidate();
            Children.Add(element);
            ComputeChildrenIndexes();
        }

        public void AddChildren(params VisualElement[] elements)
        {
            foreach (VisualElement element in elements)
            {
                element.Parent = this;
                element.Invalidate();
                Children.Add(element);
            }

            ComputeChildrenIndexes();
        }

        public void RemoveChild(VisualElement element)
        {
            if (Children.Remove(element))
            {
                element.Parent = null;
            }

            ComputeChildrenIndexes();
        }

        private void ComputeChildrenIndexes()
        {
            for (int i = 0; i < Children.Count; i++)
            {
                Children[i].ChildIndex = i;
            }
        }

        public void Invalidate()
        {
            MustRefreshStyles = true;

            foreach (VisualElement child in Children)
            {
                child.Invalidate();
            }
        }
    }
}
