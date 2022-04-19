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

        internal override void Compute(VisualElement container)
        {
            Styles.Width?.Compute(this, container);
            Styles.Height?.Compute(this, container);

            foreach (VisualElement element in _contents)
            {
                element.Compute(this);
            }

            switch (Styles.Layout.Type)
            {
                case Visual.Styles.LayoutType.Column:
                    ComputeColumnLayout();
                    break;
                case Visual.Styles.LayoutType.Row:
                    ComputeRowLayout();
                    break;
                case Visual.Styles.LayoutType.Grid:
                    break;
                case Visual.Styles.LayoutType.Absolute:
                    break;
                case Visual.Styles.LayoutType.Fixed:
                    break;
                case Visual.Styles.LayoutType.Dock:
                    break;
            }
        }

        private void ComputeColumnLayout()
        {
            float x = X;
            float y = Y;

            foreach (VisualElement element in _contents)
            {
                element.SetPosition(x, y);
                y += element.Height;
            }
        }

        private void ComputeRowLayout()
        {
            float x = X;
            float y = Y;

            foreach (VisualElement element in _contents)
            {
                element.SetPosition(x, y);
                x += element.Width;
            }
        }

        internal override void Render(RendererContext context, ViewPort viewPort)
        {
            Styles.Render(this, context);

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
