using Ixen.Core;
using SkiaSharp;

namespace Ixen.Rendering
{
    public sealed class Brush : Painter
    {
        public Brush(Color color, bool antialias = false)
        {
            SKPaint = new SKPaint()
            {
                IsAntialias = antialias,
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
    }
}
