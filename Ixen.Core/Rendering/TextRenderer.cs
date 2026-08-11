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
            FontSpec fontSpec = FontSpec.From(handlers);

            if (fontSpec.Size <= 0)
            {
                return;
            }

            TextAlign align = handlers.TextAlign.Descriptor.Horizontal;
            TextVAlign valign = handlers.TextAlign.Descriptor.Vertical;

            float lineHeight = context.GetLineHeight(fontSpec);
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
                    float slack = contentWidth - context.MeasureTextWidth(line, fontSpec);
                    x += align == TextAlign.Center ? slack / 2 : slack;
                }

                context.DrawText(line, x, top, fontSpec, handlers.Color.Brush);
                top += lineHeight;
            }
        }
    }
}
