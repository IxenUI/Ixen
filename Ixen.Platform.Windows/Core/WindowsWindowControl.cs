using Ixen.Core;
using Ixen.Core.Components;
using System;
using System.Collections.Generic;
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
        private readonly Func<WindowPlacement> _placement;
        private readonly WindowsTaskbar _taskbar;
        private bool _maximized;
        private bool _topmost;

        internal WindowsWindowControl(Func<IntPtr> handle, Func<WindowPlacement> placement, bool extends,
            WindowsTaskbar taskbar)
        {
            _handle = handle;
            _placement = placement;
            _taskbar = taskbar;
            ExtendsIntoTitleBar = extends;
        }

        public WindowPlacement Placement => _placement();

        public IReadOnlyList<Command> TaskbarButtons
        {
            get => _taskbar.Buttons;
            set => _taskbar.Buttons = value;
        }

        public void SetTaskbarProgress(TaskbarProgress state, double value) => _taskbar.SetProgress(state, value);

        public event EventHandler Changed;

        public IntPtr NativeHandle => _handle();

        public bool IsMaximized => IsZoomed(_handle());

        public bool ExtendsIntoTitleBar { get; }

        public bool Topmost
        {
            get => (GetWindowLongPtr(_handle(), GWL_EXSTYLE).ToInt64() & WS_EX_TOPMOST) != 0;
            set
            {
                if (value == Topmost)
                {
                    return;
                }

                SetWindowPos(_handle(), value ? HWND_TOPMOST : HWND_NOTOPMOST, 0, 0, 0, 0,
                    SWP_NOMOVE | SWP_NOSIZE | SWP_NOACTIVATE);

                Sync();
            }
        }

        public void Minimize() => ShowWindow(_handle(), SW_MINIMIZE);

        public void Maximize() => ShowWindow(_handle(), SW_MAXIMIZE);

        public void Restore() => ShowWindow(_handle(), SW_RESTORE);

        public void Close() => PostMessage(_handle(), WM_CLOSE, IntPtr.Zero, IntPtr.Zero);

        internal void Sync()
        {
            bool maximized = IsMaximized;
            bool topmost = Topmost;

            if (maximized == _maximized && topmost == _topmost)
            {
                return;
            }

            _maximized = maximized;
            _topmost = topmost;

            Changed?.Invoke(this, EventArgs.Empty);
        }

        private const int GWL_EXSTYLE = -20;
        private const long WS_EX_TOPMOST = 0x00000008;
        private const uint SWP_NOSIZE = 0x0001;
        private const uint SWP_NOMOVE = 0x0002;
        private const uint SWP_NOACTIVATE = 0x0010;
        private static readonly IntPtr HWND_TOPMOST = new IntPtr(-1);
        private static readonly IntPtr HWND_NOTOPMOST = new IntPtr(-2);

        [DllImport("user32.dll")]
        private static extern IntPtr GetWindowLongPtr(IntPtr window, int index);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool SetWindowPos(IntPtr window, IntPtr after, int x, int y, int width, int height,
            uint flags);

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
