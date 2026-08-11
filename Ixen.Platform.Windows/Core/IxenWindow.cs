using Ixen.Core;
using Ixen.Core.Input;
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

        private readonly WindowApi.OnPaintCallBack _onPaint;
        private readonly WindowApi.OnPointerCallBack _onPointer;

        private SKImageInfo _skImageInfo;
        private SKSurface _skSurface;
        private IxenSurface _ixenSurface;
        private readonly IxenHost _host;

        public IxenWindow(IxenSurface ixenSurface)
        {
            _pixelBuffer = new PixelBuffer();
            _ixenSurface = ixenSurface;
            _host = new IxenHost(ixenSurface, RequestRepaint);
            _onPaint = OnPaint;
            _onPointer = OnPointer;
            _windowPtr = WindowApi.CreateWindow(_ixenSurface.InitOptions.Title, _ixenSurface.InitOptions.Width, _ixenSurface.InitOptions.Height);

            if (_windowPtr == IntPtr.Zero)
            {
                throw new Exception("Could not initialize WIN32 Window");
            }
        }

        public int Show()
        {
            WindowApi.RegisterPaintCallBack(_windowPtr, _onPaint);
            WindowApi.RegisterPointerCallBack(_windowPtr, _onPointer);

            return WindowApi.ShowWindow(_windowPtr);
        }

        private void RequestRepaint()
            => WindowApi.InvalidateWindow(_windowPtr);

        private void OnPointer(int kind, int x, int y, int button)
        {
            switch ((NativePointerKind)kind)
            {
                case NativePointerKind.Move:
                    _host.PointerMove(x, y);
                    break;

                case NativePointerKind.Down:
                    _host.PointerDown(x, y, ToButton(button));
                    break;

                case NativePointerKind.Up:
                    _host.PointerUp(x, y, ToButton(button));
                    break;

                case NativePointerKind.Leave:
                    _host.PointerLeave();
                    break;

                case NativePointerKind.CaptureLost:
                    _host.PointerCaptureLost();
                    break;
            }
        }

        private static PointerButton ToButton(int button)
        {
            switch ((NativePointerButton)button)
            {
                case NativePointerButton.Left:
                    return PointerButton.Left;

                case NativePointerButton.Middle:
                    return PointerButton.Middle;

                case NativePointerButton.Right:
                    return PointerButton.Right;

                default:
                    return PointerButton.None;
            }
        }

        private const float DEFAULT_DPI = 96f;

        private void OnPaint(int width, int height)
        {
            // Re-read on every paint so moving the window between monitors of different
            // density needs no extra callback: WM_DPICHANGED already forces a repaint.
            _ixenSurface.Scale = WindowApi.GetWindowDpi(_windowPtr) / DEFAULT_DPI;

            _pixelBuffer.EnsureAlloc(width, height);
            _skImageInfo = new SKImageInfo(width, height, SKColorType.Bgra8888, SKAlphaType.Premul);
            _painted = false;

            try
            {
                using(_skSurface = SKSurface.Create(_skImageInfo, _pixelBuffer.Ptr, _pixelBuffer.RowBytes))
                {
                    if (_skSurface != null)
                    {
                        _host.Paint(_skSurface.Canvas, width, height);
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
