using Ixen.Core.Visual;

namespace Ixen.Core.Rendering
{
    internal class TextRenderer
    {
        internal void Render(VisualElement element, RendererContext context)
        {
            if (string.IsNullOrEmpty(element.Text))
            {
                return;
            }

            VisualElementStylesHandlers handlers = element.StylesHandlers;
            float fontSize = handlers.FontSize.Descriptor.Value;

            if (fontSize <= 0)
            {
                return;
            }

            context.DrawText(
                element.Text,
                element.X + element.PaddingLeft,
                element.Y + element.PaddingTop,
                handlers.FontFamily.Descriptor.Value,
                fontSize,
                handlers.Color.Brush);
        }
    }
}
