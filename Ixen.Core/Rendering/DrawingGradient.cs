using SkiaSharp;

namespace Ixen.Core.Rendering
{
    public sealed class DrawingGradient
    {
        private SKShader _shader;
        private float _width;
        private float _height;

        public DrawingGradient(Color from, Color to, bool vertical = true)
        {
            From = from;
            To = to;
            Vertical = vertical;
        }

        public Color From { get; }

        public Color To { get; }

        public bool Vertical { get; }

        internal SKShader ShaderFor(float width, float height)
        {
            if (_shader != null && _width == width && _height == height)
            {
                return _shader;
            }

            _shader?.Dispose();
            _width = width;
            _height = height;
            _shader = SKShader.CreateLinearGradient(
                new SKPoint(0, 0),
                Vertical ? new SKPoint(0, height) : new SKPoint(width, 0),
                new[] { From.SKColor, To.SKColor },
                SKShaderTileMode.Clamp);

            return _shader;
        }
    }
}
