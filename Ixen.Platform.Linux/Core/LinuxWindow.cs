using Ixen.Core;
using Ixen.Core.Input;
using Ixen.Core.Visual.Styles.Descriptors;
using Ixen.Platform.Linux.Accessibility;
using Ixen.Platform.Linux.NativeApi;
using SkiaSharp;
using System;
using System.IO;

namespace Ixen.Platform.Linux
{
    internal class LinuxWindow : IDisposable
    {
        private const float DEFAULT_DPI = 96f;
        private const float WHEEL_DELTA = 120f;

        private IntPtr _windowPtr;

        private readonly PixelBuffer _pixelBuffer = new PixelBuffer();
        private SKSurface _surface;
        private IntPtr _pixels;
        private int _width;
        private int _height;

        private readonly LinuxCursorImages _cursorImages = new();
        private readonly LinuxApi.OnPaintCallBack _onPaint;
        private readonly LinuxApi.OnPointerCallBack _onPointer;
        private readonly LinuxApi.OnKeyCallBack _onKey;
        private readonly LinuxApi.OnTextCallBack _onText;
        private readonly LinuxApi.OnWheelCallBack _onWheel;

        private readonly IxenSurface _ixenSurface;
        private readonly IxenHost _host;
        private readonly LinuxAccessibility _accessibility;

        public LinuxWindow(IxenSurface ixenSurface)
        {
            _ixenSurface = ixenSurface;
            _ixenSurface.ReducedMotion = LinuxApi.PrefersReducedMotion() != 0;
            _ixenSurface.PreservesFrame = true;

            _host = new IxenHost(ixenSurface, RequestRepaint, new LinuxScheduler(),
                new LinuxClipboard(() => _windowPtr), SetCursor, new LinuxImageSource(), null,
                CanPresent);

            IxenSynchronizationContext.Install(ixenSurface);

            _accessibility = new LinuxAccessibility(ixenSurface, () => _windowPtr);

            _onPaint = OnPaint;
            _onPointer = OnPointer;
            _onKey = OnKey;
            _onText = OnText;
            _onWheel = OnWheel;

            _windowPtr = LinuxApi.CreateWindow(_ixenSurface.InitOptions.Title,
                _ixenSurface.InitOptions.Width, _ixenSurface.InitOptions.Height);

            if (_windowPtr == IntPtr.Zero)
            {
                throw new Exception("Could not initialize the X11 window");
            }
        }

        public int Show()
        {
            LinuxApi.RegisterPaintCallBack(_windowPtr, _onPaint);
            LinuxApi.RegisterPointerCallBack(_windowPtr, _onPointer);
            LinuxApi.RegisterKeyCallBack(_windowPtr, _onKey);
            LinuxApi.RegisterTextCallBack(_windowPtr, _onText);
            LinuxApi.RegisterWheelCallBack(_windowPtr, _onWheel);

            _accessibility.Register();

            return LinuxApi.ShowWindow(_windowPtr);
        }

        private void OnPaint(int width, int height)
        {
            if (width <= 0 || height <= 0)
            {
                return;
            }

            _ixenSurface.Presentable = CanPresent();
            _ixenSurface.Scale = LinuxApi.GetWindowDpi(_windowPtr) / DEFAULT_DPI;

            _pixelBuffer.EnsureAlloc(width, height);

            SKSurface surface = Surface(width, height);

            if (surface == null)
            {
                return;
            }

            _host.Paint(surface.Canvas, width, height);

            LinuxApi.SetWindowPixelsBuffer(_windowPtr, _pixelBuffer.Ptr, width, height,
                _pixelBuffer.RowBytes);

            _accessibility.Sync();

            _paints++;

            if (_capturePath != null && _paints >= _capturePaints)
            {
                Capture();
            }
        }

        private string _capturePath;
        private int _capturePaints;
        private int _paints;

        internal void CaptureAfter(string path, int paints)
        {
            _capturePath = path;
            _capturePaints = paints < 1 ? 1 : paints;
        }

        private void Capture()
        {
            string path = _capturePath;

            _capturePath = null;

            using (SKImage image = _surface?.Snapshot())
            {
                if (image != null)
                {
                    using (SKData data = image.Encode(SKEncodedImageFormat.Png, 100))
                    using (FileStream file = File.Create(path))
                    {
                        data.SaveTo(file);
                    }
                }
            }

            LinuxApi.CloseWindow(_windowPtr);
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

        private void OnPointer(int kind, int x, int y, int button)
        {
            switch ((LinuxPointerKind)kind)
            {
                case LinuxPointerKind.Move:
                    _host.PointerMove(x, y);
                    break;

                case LinuxPointerKind.Down:
                    _host.PointerDown(x, y, ToButton(button));
                    break;

                case LinuxPointerKind.Up:
                    _host.PointerUp(x, y, ToButton(button));
                    break;

                case LinuxPointerKind.Leave:
                    _host.PointerLeave();
                    break;

                case LinuxPointerKind.CaptureLost:
                    _host.PointerCaptureLost();
                    break;
            }
        }

        private static PointerButton ToButton(int button)
        {
            switch ((LinuxPointerButton)button)
            {
                case LinuxPointerButton.Left:
                    return PointerButton.Left;

                case LinuxPointerButton.Middle:
                    return PointerButton.Middle;

                case LinuxPointerButton.Right:
                    return PointerButton.Right;

                default:
                    return PointerButton.None;
            }
        }

        private void OnKey(int kind, int keySymbol, int modifiers, int repeat)
        {
            switch ((LinuxKeyKind)kind)
            {
                case LinuxKeyKind.Down:
                    _host.KeyDown(LinuxKeys.ToKey(keySymbol), LinuxKeys.ToModifiers(modifiers),
                        repeat != 0);
                    break;

                case LinuxKeyKind.Up:
                    _host.KeyUp(LinuxKeys.ToKey(keySymbol), LinuxKeys.ToModifiers(modifiers));
                    break;
            }
        }

        private void OnText(string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                return;
            }

            _host.TextInput(text);
        }

        private void OnWheel(int x, int y, int deltaX, int deltaY, int modifiers)
            => _host.PointerWheel(x, y, deltaX / WHEEL_DELTA, deltaY / WHEEL_DELTA,
                LinuxKeys.ToModifiers(modifiers));

        private void RequestRepaint() => LinuxApi.InvalidateWindow(_windowPtr);

        private bool CanPresent() => LinuxApi.IsWindowPresentable(_windowPtr) != 0;

        private void SetCursor(CursorKind kind, CursorImage image)
        {
            byte[] pixels = _cursorImages.Get(image);

            if (pixels != null && pixels.Length > 0)
            {
                LinuxApi.SetWindowCursorImage(_windowPtr, pixels, image.Bitmap.Width,
                    image.Bitmap.Height, image.HotspotX, image.HotspotY);

                return;
            }

            LinuxApi.SetWindowCursor(_windowPtr, LinuxCursors.ToNative(kind));
        }

        public void Dispose()
        {
            _cursorImages.Clear();
            _surface?.Dispose();
            _surface = null;
            _pixelBuffer.Dispose();

            if (_windowPtr != IntPtr.Zero)
            {
                LinuxApi.DestroyWindow(_windowPtr);
                _windowPtr = IntPtr.Zero;
            }
        }
    }
}
