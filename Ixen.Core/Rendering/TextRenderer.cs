using Ixen.Core.Visual;
using Ixen.Core.Visual.Styles.Descriptors;
using System.Collections.Generic;

namespace Ixen.Core.Rendering
{
    internal class TextRenderer
    {
        internal void Render(VisualElement element, RendererContext context)
        {
            List<string> lines = element.TextLines;

            if (lines == null || lines.Count == 0)
            {
                return;
            }

            VisualElementStylesHandlers handlers = element.StylesHandlers;
            float fontSize = handlers.FontSize.Descriptor.Value;

            if (fontSize <= 0)
            {
                return;
            }

            string fontFamily = handlers.FontFamily.Descriptor.Value;
            TextAlign align = handlers.TextAlign.Descriptor.Value;
            TextVAlign valign = handlers.TextVAlign.Descriptor.Value;

            float lineHeight = context.GetLineHeight(fontFamily, fontSize);
            float contentLeft = element.X + element.PaddingLeft + element.BorderInsideLeft;
            float contentWidth = element.ContentWidth;
            float top = element.Y + element.PaddingTop + element.BorderInsideTop;

            if (valign != TextVAlign.Top)
            {
                float verticalSlack = element.ContentHeight - lineHeight * lines.Count;

                if (verticalSlack > 0)
                {
                    top += valign == TextVAlign.Middle ? verticalSlack / 2 : verticalSlack;
                }
            }

            foreach (string line in lines)
            {
                float x = contentLeft;

                if (align != TextAlign.Left)
                {
                    float slack = contentWidth - context.MeasureTextWidth(line, fontFamily, fontSize);
                    x += align == TextAlign.Center ? slack / 2 : slack;
                }

                context.DrawText(line, x, top, fontFamily, fontSize, handlers.Color.Brush);
                top += lineHeight;
            }
        }
    }
}
