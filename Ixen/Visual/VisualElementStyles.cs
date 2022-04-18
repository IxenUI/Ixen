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

        public void Compute(VisualElement element, VisualElement container, DimensionalElement targetZone)
        {
            Width?.Compute(element, container, targetZone);
            Height?.Compute(element, container, targetZone);
        }

        public void Render(VisualElement element, RendererContext context, ViewPort viewPort)
        {
            Background?.Render(element, context);
            Border?.Render(element, context);
        }
    }
}
