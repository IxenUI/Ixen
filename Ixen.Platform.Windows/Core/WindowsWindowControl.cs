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
        private readonly WindowsTray _tray;
        private readonly Action<bool> _visibility;
        private bool _maximized;
        private bool _topmost;
        private bool _visible = true;

        internal WindowsWindowControl(Func<IntPtr> handle, Func<WindowPlacement> placement, bool extends,
            WindowsTaskbar taskbar, WindowsTray tray, Action<bool> visibility)
        {
            _handle = handle;
            _placement = placement;
            _taskbar = taskbar;
            _tray = tray;
            _visibility = visibility;
            ExtendsIntoTitleBar = extends;
        }

        public TrayIcon Tray
        {
            get => _tray.Icon;
            set => _tray.Icon = value;
        }

        public bool Notify(string title, string message) => _tray.Notify(title, message);

        public bool IsVisible => IsWindowVisible(_handle());

        public void Hide()
        {
            ShowWindow(_handle(), SW_HIDE);
            _visibility(false);
            Sync();
        }

        public void Show()
        {
            IntPtr window = _handle();

            ShowWindow(window, IsIconic(window) ? SW_RESTORE : SW_SHOW);
            SetForegroundWindow(window);
            _visibility(true);
            Sync();
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
            bool visible = IsVisible;

            if (maximized == _maximized && topmost == _topmost && visible == _visible)
            {
                return;
            }

            _maximized = maximized;
            _topmost = topmost;
            _visible = visible;

            Changed?.Invoke(this, EventArgs.Empty);
        }

        private const int SW_HIDE = 0;
        private const int SW_SHOW = 5;
        private const int GWL_EXSTYLE = -20;

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool IsWindowVisible(IntPtr window);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool IsIconic(IntPtr window);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool SetForegroundWindow(IntPtr window);
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
