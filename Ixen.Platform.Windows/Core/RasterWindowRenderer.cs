using Ixen.Platform.Windows.NativeApi;
using SkiaSharp;
using System;

namespace Ixen.Platform.Windows
{
    internal class RasterWindowRenderer : WindowRenderer
    {
        private readonly PixelBuffer _pixelBuffer = new PixelBuffer();

        private SKSurface _surface;
        private IntPtr _pixels;
        private int _width;
        private int _height;

        public RasterWindowRenderer(IntPtr window)
            : base(window)
        {
        }

        internal override string Backend => "raster";

        internal override bool PreservesFrame => true;

        internal override void Paint(int width, int height, Action<SKCanvas> render)
        {
            _pixelBuffer.EnsureAlloc(width, height);

            SKSurface surface = Surface(width, height);

            if (surface == null)
            {
                return;
            }

            render(surface.Canvas);

            WindowApi.SetWindowPixelsBuffer(Window, _pixelBuffer.Ptr);
        }

        private SKSurface Surface(int width, int height)
        {
            if (_surface != null && width == _width && height == _height
                && _pixelBuffer.Ptr == _pixels)
            {
                return _surface;
            }

            _surface?.Dispose();

            var info = new SKImageInfo(width, height, SKColorType.Bgra8888, SKAlphaType.Premul);

            _surface = SKSurface.Create(info, _pixelBuffer.Ptr, _pixelBuffer.RowBytes);
            _pixels = _pixelBuffer.Ptr;
            _width = width;
            _height = height;

            return _surface;
        }

        public override void Dispose()
        {
            _surface?.Dispose();
            _surface = null;
            _pixelBuffer.Dispose();
        }
    }
}
