using Ixen.Rendering;
using Ixen.Visual.Styles;

namespace Ixen.Visual
{
    public class VisualElementStyles
    {
        public LayoutStyle Layout { get; set; } = new();
        public SizeStyle Width { get; set; } = new();
        public SizeStyle Height { get; set; } = new();

        public BackgroundStyle Background { get; set; }
        public BorderStyle Border { get; set; }

        internal void Render(VisualElement element, RendererContext context)
        {
            Background?.Render(element, context);
            Border?.Render(element, context);
        }
    }
}
