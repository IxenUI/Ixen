using Ixen.Core.Visual;
using Ixen.Core.Visual.Styles.Descriptors;
using SkiaSharp;

namespace Ixen.Core.Rendering
{
    public sealed class RendererContext
    {
        private readonly SKRoundRect _roundRect = new();
        private readonly SKPoint[] _radii = new SKPoint[4];

        private int _clipDepth;

        internal SKCanvas SKCanvas { get; private set; }

        internal void BeginFrame(SKCanvas canvas, float scale)
        {
            SKCanvas = canvas;
            _clipDepth = 0;

            if (scale <= 0 || scale == 1)
            {
                return;
            }

            SKCanvas.Save();
            _clipDepth++;
            SKCanvas.Scale(scale);
        }

        internal void EndFrame()
        {
            while (_clipDepth > 0)
            {
                SKCanvas.Restore();
                _clipDepth--;
            }
        }

        public void Clear(Color color)
        {
            SKCanvas.Clear(color.SKColor);
        }

        internal void PushClip(float x, float y, float width, float height, CornerRadiusStyleDescriptor radius)
        {
            SKCanvas.Save();
            _clipDepth++;

            if (radius != null && radius.HasRadius)
            {
                SKCanvas.ClipRoundRect(BuildRoundRect(x, y, width, height, radius), SKClipOperation.Intersect, true);
                return;
            }

            SKCanvas.ClipRect(new SKRect(x, y, x + width, y + height), SKClipOperation.Intersect, false);
        }

        internal void PopClip()
        {
            if (_clipDepth == 0)
            {
                return;
            }

            SKCanvas.Restore();
            _clipDepth--;
        }

        public void DrawInnerRectangle(float x, float y, float width, float height, Pen pen)
        {
            pen.Antialisasing = false;

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
            pen.Antialisasing = false;

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
            pen.Antialisasing = false;
            SKCanvas.DrawRect(x, y, width, height, pen.SKPaint);
        }

        internal float GetLineHeight(FontSpec fontSpec)
            => FontCache.Get(fontSpec).Spacing;

        internal float MeasureTextWidth(string text, FontSpec fontSpec)
            => FontCache.Get(fontSpec).MeasureText(text);

        internal void DrawText(string text, float x, float top, FontSpec fontSpec, Brush brush)
        {
            SKFont font = FontCache.Get(fontSpec);

            brush.Antialisasing = true;
            SKCanvas.DrawText(text, x, top - font.Metrics.Ascent, SKTextAlign.Left, font, brush.SKPaint);
        }

        internal void FillRoundRectangle(float x, float y, float width, float height,
            CornerRadiusStyleDescriptor radius, Brush brush)
        {
            brush.Antialisasing = true;
            SKCanvas.DrawRoundRect(BuildRoundRect(x, y, width, height, radius), brush.SKPaint);
        }

        internal void DrawRoundRectangle(float x, float y, float width, float height,
            CornerRadiusStyleDescriptor radius, Pen pen, BorderType type)
        {
            float offset = 0;

            if (type == BorderType.Inner)
            {
                offset = pen.Width / 2;
            }
            else if (type == BorderType.Outer)
            {
                offset = -pen.Width / 2;
            }

            pen.Antialisasing = true;
            SKCanvas.DrawRoundRect(
                BuildRoundRect(x + offset, y + offset, width - offset * 2, height - offset * 2, radius),
                pen.SKPaint);
        }

        private SKRoundRect BuildRoundRect(float x, float y, float width, float height,
            CornerRadiusStyleDescriptor radius)
        {
            _radii[0] = new SKPoint(radius.TopLeft, radius.TopLeft);
            _radii[1] = new SKPoint(radius.TopRight, radius.TopRight);
            _radii[2] = new SKPoint(radius.BottomRight, radius.BottomRight);
            _radii[3] = new SKPoint(radius.BottomLeft, radius.BottomLeft);

            _roundRect.SetRectRadii(new SKRect(x, y, x + width, y + height), _radii);

            return _roundRect;
        }

        public void FillRectangle(float x, float y, float width, float height, Brush brush)
        {
            brush.Antialisasing = false;
            SKCanvas.DrawRect(x, y, width, height, brush.SKPaint);
        }
    }
}
