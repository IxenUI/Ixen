using Ixen.Core;
using Ixen.Core.Input;
using Ixen.Platform.Windows.Accessibility;
using Ixen.Platform.Windows.NativeApi;
using SkiaSharp;
using System;

namespace Ixen.Platform.Windows
{
    internal class IxenWindow : IDisposable
    {
        private IntPtr _windowPtr;

        private WindowRenderer _renderer;

        private readonly WindowApi.OnPaintCallBack _onPaint;
        private readonly WindowApi.OnPointerCallBack _onPointer;
        private readonly WindowApi.OnKeyCallBack _onKey;
        private readonly WindowApi.OnImeCallBack _onIme;
        private readonly WindowApi.OnWheelCallBack _onWheel;
        private readonly WindowApi.OnAccessibilityCallBack _onAccessibility;

        private readonly UiaBridge _accessibility;

        private IxenSurface _ixenSurface;
        private readonly IxenHost _host;

        public IxenWindow(IxenSurface ixenSurface)
        {
            _ixenSurface = ixenSurface;
            _ixenSurface.ReducedMotion = SystemPreferences.PrefersReducedMotion();
            _host = new IxenHost(ixenSurface, RequestRepaint, new MessageScheduler(), new WindowsClipboard(),
                SetCursor, new WindowsImageSource());

            IxenSynchronizationContext.Install(ixenSurface);

            _onPaint = OnPaint;
            _onPointer = OnPointer;
            _onKey = OnKey;
            _onIme = OnIme;
            _onWheel = OnWheel;
            _onAccessibility = OnAccessibility;
            _windowPtr = WindowApi.CreateWindow(_ixenSurface.InitOptions.Title, _ixenSurface.InitOptions.Width, _ixenSurface.InitOptions.Height);

            if (_windowPtr == IntPtr.Zero)
            {
                throw new Exception("Could not initialize WIN32 Window");
            }

            _renderer = ixenSurface.InitOptions.PreferGpu
                ? GpuWindowRenderer.TryCreate(_windowPtr) ?? (WindowRenderer)new RasterWindowRenderer(_windowPtr)
                : new RasterWindowRenderer(_windowPtr);

            _ixenSurface.PreservesFrame = _renderer.PreservesFrame;

            _accessibility = new UiaBridge(_ixenSurface,
                () => WindowApi.GetWindowHandle(_windowPtr),
                RequestRepaint);
        }

        public int Show()
        {
            WindowApi.RegisterPaintCallBack(_windowPtr, _onPaint);
            WindowApi.RegisterPointerCallBack(_windowPtr, _onPointer);
            WindowApi.RegisterKeyCallBack(_windowPtr, _onKey);
            WindowApi.RegisterImeCallBack(_windowPtr, _onIme);
            WindowApi.RegisterWheelCallBack(_windowPtr, _onWheel);
            WindowApi.RegisterAccessibilityCallBack(_windowPtr, _onAccessibility);

            return WindowApi.ShowWindow(_windowPtr);
        }

        private const float WHEEL_DELTA = 120f;

        private void OnWheel(int x, int y, int deltaX, int deltaY, int modifiers)
            => _host.PointerWheel(x, y, deltaX / WHEEL_DELTA, deltaY / WHEEL_DELTA,
                NativeKeys.ToModifiers(modifiers));

        private void OnIme(int kind, string text, int caret)
        {
            switch ((NativeImeKind)kind)
            {
                case NativeImeKind.Update:
                    _host.Composition(text, caret);
                    break;

                case NativeImeKind.Commit:
                    _host.CommitComposition(text);
                    break;

                case NativeImeKind.Cancel:
                    _host.CancelComposition();
                    break;
            }
        }

        private void OnKey(int kind, int keyCode, int modifiers, int repeat)
        {
            switch ((NativeKeyKind)kind)
            {
                case NativeKeyKind.Down:
                    _host.KeyDown(NativeKeys.ToKey(keyCode), NativeKeys.ToModifiers(modifiers), repeat != 0);
                    break;

                case NativeKeyKind.Up:
                    _host.KeyUp(NativeKeys.ToKey(keyCode), NativeKeys.ToModifiers(modifiers));
                    break;

                case NativeKeyKind.Char:
                    OnChar(keyCode);
                    break;
            }
        }

        private void OnChar(int keyCode)
        {
            _host.TextInput(((char)keyCode).ToString());
        }

        private void RequestRepaint()
            => WindowApi.InvalidateWindow(_windowPtr);

        private void SetCursor(Ixen.Core.Visual.Styles.Descriptors.CursorKind kind)
            => WindowApi.SetWindowCursor(_windowPtr, NativeCursors.ToNative(kind));

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
            _ixenSurface.Scale = WindowApi.GetWindowDpi(_windowPtr) / DEFAULT_DPI;

            _renderer.Paint(width, height, canvas => _host.Paint(canvas, width, height));

            _accessibility.Sync();
        }

        private IntPtr OnAccessibility(IntPtr wParam, IntPtr lParam)
            => _accessibility.Answer(wParam, lParam);

        internal string Backend => _renderer.Backend;

        public void Dispose()
        {
            _renderer.Dispose();
        }
    }
}
