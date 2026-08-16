using Ixen.Core.Visual;
using Ixen.Core.Visual.Styles.Descriptors;
using System.Collections.Generic;

namespace Ixen.Core.Rendering
{
    internal class TextRenderer
    {
        private const byte SELECTION_ALPHA = 0x40;
        private const byte PLACEHOLDER_ALPHA = 0x80;

        private Brush _selectionBrush;
        private Color _selectionSource;

        private Brush _dimmedBrush;
        private Color _dimmedSource;

        private Brush SelectionBrush(Color source)
        {
            if (_selectionBrush != null && _selectionSource.Equals(source))
            {
                return _selectionBrush;
            }

            _selectionSource = source;
            _selectionBrush = new Brush(source.WithAlpha(SELECTION_ALPHA));

            return _selectionBrush;
        }

        private Brush DimmedBrush(Color source)
        {
            if (_dimmedBrush != null && _dimmedSource.Equals(source))
            {
                return _dimmedBrush;
            }

            _dimmedSource = source;
            _dimmedBrush = new Brush(source.WithAlpha(PLACEHOLDER_ALPHA));

            return _dimmedBrush;
        }

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

            if (element is TextField field)
            {
                RenderField(field, lines[0], context, handlers, fontSpec);
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

        private void RenderField(TextField field, string value, RendererContext context,
            VisualElementStylesHandlers handlers, FontSpec fontSpec)
        {
            float contentLeft = field.X + field.PaddingLeft + field.BorderInsideLeft;
            float contentTop = field.Y + field.PaddingTop + field.BorderInsideTop;
            float lineHeight = context.GetLineHeight(fontSpec);
            float top = contentTop;

            if (handlers.TextAlign.Descriptor.Vertical != TextVAlign.Top)
            {
                float slack = field.ContentHeight - lineHeight;

                if (slack > 0)
                {
                    top += handlers.TextAlign.Descriptor.Vertical == TextVAlign.Middle ? slack / 2 : slack;
                }
            }

            context.PushClip(contentLeft, contentTop, field.ContentWidth, field.ContentHeight, null);

            float x = contentLeft - field.ContentOffset;

            if (field.SelectionLength > 0)
            {
                float from = field.OffsetAt(field.SelectionStart);
                float to = field.OffsetAt(field.SelectionStart + field.SelectionLength);

                context.FillRectangle(x + from, top, to - from, lineHeight,
                    SelectionBrush(handlers.Color.Brush.Color));
            }

            if (field.ShowsPlaceholder)
            {
                context.DrawText(field.Placeholder, x, top, fontSpec,
                    DimmedBrush(handlers.Color.Brush.Color));
            }
            else
            {
                context.DrawText(value, x, top, fontSpec, handlers.Color.Brush);
            }

            if (field.CaretVisible && field.IsFocused)
            {
                context.FillRectangle(x + field.OffsetAt(field.CaretIndex), top, 1, lineHeight,
                    handlers.Color.Brush);
            }

            context.PopClip();
        }
    }
}
