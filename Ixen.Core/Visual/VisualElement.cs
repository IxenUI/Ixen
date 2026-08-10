using System.Collections.Generic;

namespace Ixen.Core.Visual
{
    public class VisualElement : BoxedElement
    {
        internal int ChildIndex { get; set; }
        internal List<VisualElement> Children { get; private set; } = new();

        private float[] _gridColumns;
        private float[] _gridRows;

        internal float[] GridColumns => _gridColumns;
        internal float[] GridRows => _gridRows;

        internal float[] EnsureGridColumns(int count)
        {
            if (_gridColumns == null || _gridColumns.Length != count)
            {
                _gridColumns = new float[count];
            }

            return _gridColumns;
        }

        internal float[] EnsureGridRows(int count)
        {
            if (_gridRows == null || _gridRows.Length != count)
            {
                _gridRows = new float[count];
            }

            return _gridRows;
        }

        internal VisualElement Parent { get; private set; }
        internal DimensionalElement Clip { get; set; }
        internal bool MustRefreshStyles { get; set; } = true;
        internal bool IsLayoutDirty { get; private set; } = true;

        private string _text;

        public string Text
        {
            get => _text;
            set
            {
                if (_text == value)
                {
                    return;
                }

                _text = value;
                InvalidateLayout();
            }
        }

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
                InvalidateLayout();
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
            MarkStylesDirty();
            InvalidateLayout();
        }

        public void InvalidateLayout()
        {
            IsLayoutDirty = true;

            for (VisualElement parent = Parent; parent != null && !parent.IsLayoutDirty; parent = parent.Parent)
            {
                parent.IsLayoutDirty = true;
            }
        }

        internal void ClearLayoutDirty()
        {
            IsLayoutDirty = false;

            foreach (VisualElement child in Children)
            {
                child.ClearLayoutDirty();
            }
        }

        private void MarkStylesDirty()
        {
            MustRefreshStyles = true;

            foreach (VisualElement child in Children)
            {
                child.MarkStylesDirty();
            }
        }
    }
}
