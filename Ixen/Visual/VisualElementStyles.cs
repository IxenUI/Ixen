using Ixen.Rendering;
using Ixen.Visual.Styles;

namespace Ixen.Visual
{
    public class VisualElementStyles
    {
        public WidthStyle Width { get; set; }
        public HeightStyle Height { get; set; }
        public BackgroundStyle Background { get; set; }
        public BorderStyle Border { get; set; }

        public void Compute(VisualElement element, float x, float y, float width, float height)
        {
            Width?.Compute(element, x, y, width, height);
            Height?.Compute(element, x, y, width, height);
            Background?.Compute(element, x, y, width, height);
            Border?.Compute(element, x, y, width, height);
        }

        public void Render(VisualElement element, RendererContext context, ViewPort viewPort)
        {
            Width?.Render(element, context, viewPort);
            Height?.Render(element, context, viewPort);
            Background?.Render(element, context, viewPort);
            Border?.Render(element, context, viewPort);
        }
    }
}
