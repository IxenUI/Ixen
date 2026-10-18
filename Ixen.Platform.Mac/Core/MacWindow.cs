using Ixen.Core;
using Ixen.Core.Input;
using Ixen.Core.Visual;
using Ixen.Core.Visual.Styles.Descriptors;
using Ixen.Platform.Mac.Accessibility;
using Ixen.Platform.Mac.NativeApi;
using SkiaSharp;
using System;
using System.IO;

namespace Ixen.Platform.Mac
{
    internal class MacWindow : IDisposable
    {
        private const float TEXT_RECT_WIDTH = 1f;
        private const float DEFAULT_DPI = 96f;
        private const float WHEEL_DELTA = 120f;

        private IntPtr _windowPtr;

        private readonly PixelBuffer _pixelBuffer = new PixelBuffer();
        private SKSurface _surface;
        private IntPtr _pixels;
        private int _width;
        private int _height;

        private readonly MacCursorImages _cursorImages = new();
        private readonly MacApi.OnPaintCallBack _onPaint;
        private readonly MacApi.OnPointerCallBack _onPointer;
        private readonly MacApi.OnKeyCallBack _onKey;
        private readonly MacApi.OnImeCallBack _onIme;
        private readonly MacApi.OnWheelCallBack _onWheel;
        private readonly MacApi.OnDropCallBack _onDrop;
        private readonly MacApi.OnSuspendCallBack _onSuspend;
        private readonly MacApi.OnPreferencesCallBack _onPreferences;
        private readonly MacApi.OnDragCallBack _onDrag;

        private readonly IxenSurface _ixenSurface;
        private readonly IxenHost _host;
        private readonly MacAccessibility _accessibility;

        public MacWindow(IxenSurface ixenSurface)
        {
            _ixenSurface = ixenSurface;
            ReadPreferences();
            _ixenSurface.PreservesFrame = true;
            _ixenSurface.AcceleratorModifier = KeyModifiers.Meta;

            _host = new IxenHost(ixenSurface, RequestRepaint, new MacScheduler(), new MacClipboard(),
                SetCursor, new MacImageSource(), null, CanPresent);

            IxenSynchronizationContext.Install(ixenSurface);

            _accessibility = new MacAccessibility(ixenSurface, () => _windowPtr);

            _onPaint = OnPaint;
            _onPointer = OnPointer;
            _onKey = OnKey;
            _onIme = OnIme;
            _onWheel = OnWheel;
            _onDrop = OnDrop;
            _onSuspend = OnSuspend;
            _onPreferences = OnPreferences;
            _onDrag = OnDrag;

            _windowPtr = MacApi.CreateWindow(_ixenSurface.InitOptions.Title,
                _ixenSurface.InitOptions.Width, _ixenSurface.InitOptions.Height);

            if (_windowPtr == IntPtr.Zero)
            {
                throw new Exception("Could not initialize the macOS window");
            }

            SetIcon(_ixenSurface.InitOptions.Icon);
        }

        private static void SetIcon(string name)
        {
            byte[] bytes = ReadIcon(name);

            if (bytes != null)
            {
                MacApi.SetApplicationIcon(bytes, bytes.Length);
            }
        }

        private static byte[] ReadIcon(string name)
        {
            try
            {
                using (Stream stream = new MacImageSource().Open(name))
                {
                    if (stream == null)
                    {
                        return null;
                    }

                    using (var copy = new MemoryStream())
                    {
                        stream.CopyTo(copy);

                        return copy.ToArray();
                    }
                }
            }
            catch (IOException)
            {
                return null;
            }
        }

        public int Show()
        {
            MacApi.RegisterPaintCallBack(_windowPtr, _onPaint);
            MacApi.RegisterPointerCallBack(_windowPtr, _onPointer);
            MacApi.RegisterKeyCallBack(_windowPtr, _onKey);
            MacApi.RegisterImeCallBack(_windowPtr, _onIme);
            MacApi.RegisterWheelCallBack(_windowPtr, _onWheel);
            MacApi.RegisterDropCallBack(_windowPtr, _onDrop);
            MacApi.RegisterSuspendCallBack(_windowPtr, _onSuspend);
            MacApi.RegisterPreferencesCallBack(_windowPtr, _onPreferences);
            MacApi.RegisterDragCallBack(_windowPtr, _onDrag);

            _accessibility.Register();

            return MacApi.ShowWindow(_windowPtr);
        }

        private void OnPaint(int width, int height)
        {
            if (width <= 0 || height <= 0)
            {
                return;
            }

            _ixenSurface.Presentable = CanPresent();
            _ixenSurface.Scale = MacApi.GetWindowDpi(_windowPtr) / DEFAULT_DPI;

            _pixelBuffer.EnsureAlloc(width, height);

            SKSurface surface = Surface(width, height);

            if (surface == null)
            {
                return;
            }

            _host.Paint(surface.Canvas, width, height);

            MacApi.SetWindowPixelsBuffer(_windowPtr, _pixelBuffer.Ptr, width, height,
                _pixelBuffer.RowBytes);

            _accessibility.Sync();
            SyncTextRect();

            SyncAcceptsFiles();

            _paints++;

            if (_capturePath != null && _paints >= _capturePaints)
            {
                Capture();
            }
            else if (_capturePath != null)
            {
                RequestRepaint();
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

            MacApi.CloseWindow(_windowPtr);
        }

        private SKSurface Surface(int width, int height)
        {
            if (_surface != null && width == _width && height == _height
                && _pixelBuffer.Ptr == _pixels)
            {
                return _surface;
            }

            _surface?.Dispose();

            var info = new SKImageInfo(width, height, SKColorType.Rgba8888, SKAlphaType.Premul);

            _surface = SKSurface.Create(info, _pixelBuffer.Ptr, _pixelBuffer.RowBytes);
            _pixels = _pixelBuffer.Ptr;
            _width = width;
            _height = height;

            return _surface;
        }

        private void OnPointer(int kind, int x, int y, int button)
        {
            if (_capturePath != null)
            {
                return;
            }

            switch ((MacPointerKind)kind)
            {
                case MacPointerKind.Move:
                    _host.PointerMove(x, y);
                    break;

                case MacPointerKind.Down:
                    _host.PointerDown(x, y, ToButton(button));
                    break;

                case MacPointerKind.Up:
                    _host.PointerUp(x, y, ToButton(button));
                    break;

                case MacPointerKind.Leave:
                    _host.PointerLeave();
                    break;

                case MacPointerKind.CaptureLost:
                    _host.PointerCaptureLost();
                    break;
            }
        }

        private static PointerButton ToButton(int button)
        {
            switch ((MacPointerButton)button)
            {
                case MacPointerButton.Left:
                    return PointerButton.Left;

                case MacPointerButton.Middle:
                    return PointerButton.Middle;

                case MacPointerButton.Right:
                    return PointerButton.Right;

                default:
                    return PointerButton.None;
            }
        }

        private void OnKey(int kind, int keyCode, int modifiers, int repeat)
        {
            switch ((MacKeyKind)kind)
            {
                case MacKeyKind.Down:
                    _host.KeyDown(MacKeys.ToKey(keyCode), MacKeys.ToModifiers(modifiers), repeat != 0);
                    break;

                case MacKeyKind.Up:
                    _host.KeyUp(MacKeys.ToKey(keyCode), MacKeys.ToModifiers(modifiers));
                    break;

                case MacKeyKind.Char:
                    _host.TextInput(((char)keyCode).ToString());
                    break;
            }
        }

        private void OnWheel(int x, int y, int deltaX, int deltaY, int modifiers)
            => _host.PointerWheel(x, y, deltaX / WHEEL_DELTA, deltaY / WHEEL_DELTA,
                MacKeys.ToModifiers(modifiers));

        private void OnIme(int kind, string text, int caret)
        {
            switch ((MacImeKind)kind)
            {
                case MacImeKind.Update:
                    _host.Composition(text, caret);
                    break;

                case MacImeKind.Commit:
                    _host.CommitComposition(text);
                    break;

                case MacImeKind.Cancel:
                    _host.CancelComposition();
                    break;

                case MacImeKind.Finish:
                    _host.FinishComposition();
                    break;
            }
        }

        private static readonly char[] _dropSeparator = { '\n' };

        private void OnSuspend() => _host.ReleaseCaches();

        private bool _hasTextRect;
        private float _textX;
        private float _textY;
        private float _textHeight;

        private void SyncTextRect()
        {
            TextField field = _ixenSurface.FocusedElement as TextField;
            float x = 0;
            float y = 0;
            float height = 0;

            if (field != null)
            {
                field.CaretBounds(field.IsComposing ? field.CompositionStart : field.DisplayCaret,
                    out x, out y, out height);
            }

            bool has = field != null;

            if (has == _hasTextRect && x == _textX && y == _textY && height == _textHeight)
            {
                return;
            }

            _hasTextRect = has;
            _textX = x;
            _textY = y;
            _textHeight = height;

            MacApi.SetTextInputRect(_windowPtr, has ? 1 : 0, x, y, TEXT_RECT_WIDTH, height);
        }

        private void OnPreferences()
        {
            ReadPreferences();
            RequestRepaint();
        }

        private void ReadPreferences()
        {
            _ixenSurface.ReducedMotion = MacApi.PrefersReducedMotion() != 0;
            _ixenSurface.HighContrast = MacApi.PrefersHighContrast() != 0;
            _ixenSurface.SystemColors = MacPreferences.SystemColors();
        }

        private void OnDrop(int x, int y, string paths)
        {
            if (string.IsNullOrEmpty(paths))
            {
                return;
            }

            _host.PointerDrop(x, y, paths.Split(_dropSeparator));
        }

        private int OnDrag(int kind, int x, int y, string paths)
        {
            if ((MacDragKind)kind == MacDragKind.Leave)
            {
                _host.PointerDragLeave();
                return 0;
            }

            if (string.IsNullOrEmpty(paths))
            {
                return 0;
            }

            return _host.PointerDragOver(x, y, paths.Split(_dropSeparator)) ? 1 : 0;
        }

        private bool _acceptsFiles;

        private void SyncAcceptsFiles()
        {
            bool wanted = _ixenSurface.AcceptsDrops;

            if (wanted == _acceptsFiles)
            {
                return;
            }

            _acceptsFiles = wanted;

            MacApi.SetWindowAcceptsFiles(_windowPtr, wanted ? 1 : 0);
        }

        private void RequestRepaint() => MacApi.InvalidateWindow(_windowPtr);

        private bool CanPresent() => MacApi.IsWindowPresentable(_windowPtr) != 0;

        private void SetCursor(CursorKind kind, CursorImage image)
        {
            byte[] bytes = _cursorImages.Get(image);

            if (bytes != null && bytes.Length > 0)
            {
                MacApi.SetWindowCursorImage(_windowPtr, bytes, bytes.Length,
                    image.HotspotX, image.HotspotY);

                return;
            }

            MacApi.SetWindowCursor(_windowPtr, MacCursors.ToNative(kind));
        }

        public void Dispose()
        {
            _cursorImages.Clear();
            _surface?.Dispose();
            _surface = null;
            _pixelBuffer.Dispose();

            if (_windowPtr != IntPtr.Zero)
            {
                MacApi.DestroyWindow(_windowPtr);
                _windowPtr = IntPtr.Zero;
            }
        }
    }
}
