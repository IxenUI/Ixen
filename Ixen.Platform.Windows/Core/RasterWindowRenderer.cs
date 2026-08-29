using Ixen.Platform.Windows.NativeApi;
using SkiaSharp;
using System;

namespace Ixen.Platform.Windows
{
    internal class RasterWindowRenderer : WindowRenderer
    {
        private readonly PixelBuffer _pixelBuffer = new PixelBuffer();

        public RasterWindowRenderer(IntPtr window)
            : base(window)
        {
        }

        internal override string Backend => "raster";

        internal override bool PreservesFrame => true;

        internal override void Paint(int width, int height, Action<SKCanvas> render)
        {
            _pixelBuffer.EnsureAlloc(width, height);

            var info = new SKImageInfo(width, height, SKColorType.Bgra8888, SKAlphaType.Premul);
            bool painted = false;

            try
            {
                using (SKSurface surface = SKSurface.Create(info, _pixelBuffer.Ptr, _pixelBuffer.RowBytes))
                {
                    if (surface != null)
                    {
                        render(surface.Canvas);
                        painted = true;
                    }
                }
            }
            finally
            {
                if (painted)
                {
                    WindowApi.SetWindowPixelsBuffer(Window, _pixelBuffer.Ptr);
                }
            }
        }

        public override void Dispose()
        {
            _pixelBuffer.Dispose();
        }
    }
}
