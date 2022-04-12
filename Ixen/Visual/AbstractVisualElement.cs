using Ixen.Rendering;

namespace Ixen.Visual
{
    public abstract class AbstractVisualElement : DimensionalElement
    {
        protected ViewPort _viewPort = new();

        public bool IsComputed { get; protected set; }
        public bool IsRendered { get; protected set; }

        public VisualElementStyles Styles { get; set; } = new();

        public abstract void Compute(float x, float y, float width, float height);
        public abstract void Render(RendererContext context, ViewPort viewPort);
    }
}
