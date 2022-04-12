using SkiaSharp;

namespace Ixen.Rendering
{
    public abstract class Painter
    {
        internal SKPaint SKPaint { get; set; }

        public bool Antialisasing
        {
            get => SKPaint.IsAntialias;
            set => SKPaint.IsAntialias = value;
        }
    }
}
