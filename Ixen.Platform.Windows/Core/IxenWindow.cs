using Ixen.Core;
using Ixen.Platform.Windows.NativeApi;
using SkiaSharp;
using System;

namespace Ixen.Platform.Windows
{
    internal class IxenWindow : IDisposable
    {
        private IntPtr _windowPtr;

        private readonly PixelBuffer _pixelBuffer;
        private bool _painted;
        
        private SKImageInfo _skImageInfo;
        private SKSurface _skSurface;
        private IxenSurface _ixenSurface;

        public IxenWindow(IxenSurface ixenSurface)
        {
            _pixelBuffer = new PixelBuffer();
            _ixenSurface = ixenSurface;
            _windowPtr = WindowApi.CreateWindow(_ixenSurface.InitOptions.Title, _ixenSurface.InitOptions.Width, _ixenSurface.InitOptions.Height);

            if (_windowPtr == IntPtr.Zero)
            {
                throw new Exception("Could not initialize WIN32 Window");
            }
        }

        public int Show()
        {
            WindowApi.RegisterPaintCallBack(_windowPtr, OnPaint);

            return WindowApi.ShowWindow(_windowPtr);
        }

        private void OnPaint(int width, int height)
        {
            _pixelBuffer.EnsureAlloc(width, height);
            _ixenSurface.ComputeLayout(width, height);
            _skImageInfo = new SKImageInfo(width, height, SKColorType.Bgra8888, SKAlphaType.Premul);
            _painted = false;

            try
            {
                using(_skSurface = SKSurface.Create(_skImageInfo, _pixelBuffer.Ptr, _pixelBuffer.RowBytes))
                {
                    if (_skSurface != null)
                    {
                        _ixenSurface.Render(_skSurface.Canvas);
                        _painted = true;
                    }
                }
            }
            finally
            {
                if (_painted)
                {
                    WindowApi.SetWindowPixelsBuffer(_windowPtr, _pixelBuffer.Ptr);
                }
            }
        }

        public void Dispose()
        {
            _pixelBuffer.Dispose();
        }
    }
}
