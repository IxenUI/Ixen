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

        public override void Compute(float x, float y, float width, float height)
        {
            X = x;
            Y = y;
            Width = width;
            Height = height;

            if (Styles != null)
            {
                if(Styles.Width != null)
                {
                    Width = Styles.Width.Value;
                }

                if (Styles.Height != null)
                {
                    Height = Styles.Width.Value;
                }
            }

            foreach (VisualElement element in _contents)
            {
                element.Compute(X, Y, Width, Height);
            }
        }

        public override void Render(RendererContext context, ViewPort viewPort)
        {
            if (Styles != null)
            {
                if (Styles.Background != null)
                {
                    context.FillRectangle(X, Y, Width, Height, new Brush(Styles.Background.Color));
                }

                if (Styles.Border != null)
                {
                    switch (Styles.Border.Type)
                    {
                        case Visual.Styles.BorderType.Center:
                            context.DrawRectangle(X, Y, Width, Height, new Pen(Styles.Border.Color, Styles.Border.Thickness));
                            break;
                        case Visual.Styles.BorderType.Inner:
                            context.DrawInnerRectangle(X, Y, Width, Height, new Pen(Styles.Border.Color, Styles.Border.Thickness));
                            break;
                        case Visual.Styles.BorderType.Outer:
                            context.DrawOuterRectangle(X, Y, Width, Height, new Pen(Styles.Border.Color, Styles.Border.Thickness));
                            break;
                    }
                }
            }

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
