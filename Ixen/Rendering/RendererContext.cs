using Ixen.Core;
using SkiaSharp;

namespace Ixen.Rendering
{
    public sealed class RendererContext
    {
        internal SKCanvas SKCanvas { get; set; }

        public void Clear(Color color)
        {
            SKCanvas.Clear(color.SKColor);
        }

        public void DrawInnerRectangle(float x, float y, float width, float height, Pen pen)
        {
            SKCanvas.DrawRect
            (
                x + pen.Width / 2,
                y + pen.Width / 2,
                width - pen.Width,
                height - pen.Width,
                pen.SKPaint
            );
        }

        public void DrawOuterRectangle(float x, float y, float width, float height, Pen pen)
        {
            SKCanvas.DrawRect
            (
                x - pen.Width / 2,
                y - pen.Width / 2,
                width + pen.Width,
                height + pen.Width,
                pen.SKPaint
            );
        }

        public void DrawRectangle(float x, float y, float width, float height, Pen pen)
        {
            SKCanvas.DrawRect(x, y, width, height, pen.SKPaint);
        }

        public void FillRectangle(float x, float y, float width, float height, Brush brush)
        {
            SKCanvas.DrawRect(x, y, width, height, brush.SKPaint);
        }
    }
}
