using Ixen.Core.Visual.Styles.Descriptors;
using SkiaSharp;

namespace Ixen.Core.Rendering
{
    public sealed class RendererContext
    {
        private SKRect _clipRect = SKRect.Empty;
        private bool _hasSavedState;

        private readonly SKRoundRect _roundRect = new();
        private readonly SKPoint[] _radii = new SKPoint[4];

        internal SKCanvas SKCanvas { get; private set; }

        internal void BeginFrame(SKCanvas canvas)
        {
            SKCanvas = canvas;
            _clipRect = SKRect.Empty;
            _hasSavedState = false;
        }

        internal void EndFrame()
        {
            if (_hasSavedState)
            {
                SKCanvas.Restore();
                _hasSavedState = false;
            }

            _clipRect = SKRect.Empty;
        }

        public void Clear(Color color)
        {
            SKCanvas.Clear(color.SKColor);
        }

        public void SetClip(float x, float y, float width, float height)
        {
            var clipRect = new SKRect(x, y, x + width, y + height);

            if (_hasSavedState && clipRect == _clipRect)
            {
                return;
            }

            if (_hasSavedState)
            {
                SKCanvas.Restore();
            }

            SKCanvas.Save();
            SKCanvas.ClipRect(clipRect, SKClipOperation.Intersect, false);

            _clipRect = clipRect;
            _hasSavedState = true;
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

        internal void DrawText(string text, float x, float top, string fontFamily, float fontSize, Brush brush)
        {
            SKFont font = FontCache.Get(fontFamily, fontSize);

            SKCanvas.DrawText(text, x, top - font.Metrics.Ascent, SKTextAlign.Left, font, brush.SKPaint);
        }

        internal void FillRoundRectangle(float x, float y, float width, float height,
            CornerRadiusStyleDescriptor radius, Brush brush)
        {
            SKCanvas.DrawRoundRect(BuildRoundRect(x, y, width, height, radius), brush.SKPaint);
        }

        internal void DrawRoundRectangle(float x, float y, float width, float height,
            CornerRadiusStyleDescriptor radius, Pen pen)
        {
            SKCanvas.DrawRoundRect(BuildRoundRect(x, y, width, height, radius), pen.SKPaint);
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
            SKCanvas.DrawRect(x, y, width, height, brush.SKPaint);
        }
    }
}
