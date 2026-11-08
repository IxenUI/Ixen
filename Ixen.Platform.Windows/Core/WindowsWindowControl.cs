using Ixen.Core;
using System;
using System.Runtime.InteropServices;

namespace Ixen.Platform.Windows
{
    internal sealed class WindowsWindowControl : IWindowControl
    {
        private const int SW_MAXIMIZE = 3;
        private const int SW_MINIMIZE = 6;
        private const int SW_RESTORE = 9;
        private const uint WM_CLOSE = 0x0010;

        private readonly Func<IntPtr> _handle;
        private bool _maximized;

        internal WindowsWindowControl(Func<IntPtr> handle, bool extends)
        {
            _handle = handle;
            ExtendsIntoTitleBar = extends;
        }

        public event EventHandler Changed;

        public IntPtr NativeHandle => _handle();

        public bool IsMaximized => IsZoomed(_handle());

        public bool ExtendsIntoTitleBar { get; }

        public void Minimize() => ShowWindow(_handle(), SW_MINIMIZE);

        public void Maximize() => ShowWindow(_handle(), SW_MAXIMIZE);

        public void Restore() => ShowWindow(_handle(), SW_RESTORE);

        public void Close() => PostMessage(_handle(), WM_CLOSE, IntPtr.Zero, IntPtr.Zero);

        internal void Sync()
        {
            bool maximized = IsMaximized;

            if (maximized == _maximized)
            {
                return;
            }

            _maximized = maximized;

            Changed?.Invoke(this, EventArgs.Empty);
        }

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool IsZoomed(IntPtr window);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool ShowWindow(IntPtr window, int command);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool PostMessage(IntPtr window, uint message, IntPtr wParam, IntPtr lParam);
    }
}
