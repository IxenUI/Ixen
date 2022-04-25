using SkiaSharp;

namespace Ixen.Core.Rendering
{
    public sealed class Pen : Painter
    {
        public Pen(Color color, float width, bool antialias = false)
        {
            _color = color;

            SKPaint = new SKPaint()
            {
                IsStroke = true,
                IsAntialias = antialias,
                StrokeWidth = width,
                Color = color.SKColor
            };
        }

        private Color _color;
        public Color Color
        {
            get => _color;
            set
            {
                _color = value;
                SKPaint.Color = _color.SKColor;
            }
        }

        public float Width
        {
            get => SKPaint.StrokeWidth;
            set => SKPaint.StrokeWidth = value;
        }
    }
}
