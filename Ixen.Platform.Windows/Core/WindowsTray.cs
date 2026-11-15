using Ixen.Core;
using Ixen.Core.Components;
using Ixen.Platform.Windows.NativeApi;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace Ixen.Platform.Windows
{
    internal sealed class WindowsTray
    {
        private const uint MF_STRING = 0x0000;
        private const uint MF_GRAYED = 0x0001;
        private const uint MF_SEPARATOR = 0x0800;
        private const uint TPM_RIGHTBUTTON = 0x0002;
        private const uint TPM_NONOTIFY = 0x0080;
        private const uint TPM_RETURNCMD = 0x0100;
        private const uint WM_NULL = 0x0000;

        private readonly IntPtr _windowPtr;
        private readonly WindowsIconCache _icons;
        private readonly Action _show;
        private readonly EventHandler _changed;

        private TrayIcon _tray;
        private bool _disposed;

        internal WindowsTray(IntPtr windowPtr, WindowsIconCache icons, Action show)
        {
            _windowPtr = windowPtr;
            _icons = icons;
            _show = show;
            _changed = (sender, e) => Push();
        }

        internal TrayIcon Icon
        {
            get => _tray;
            set
            {
                if (_tray == value)
                {
                    return;
                }

                if (_tray != null)
                {
                    _tray.Changed -= _changed;
                }

                _tray = value;

                if (_tray != null)
                {
                    _tray.Changed += _changed;
                }

                Push();
            }
        }

        internal bool Notify(string title, string message)
            => !_disposed && WindowApi.NotifyTray(_windowPtr, title ?? string.Empty, message ?? string.Empty) != 0;

        internal void DpiChanged() => Push();

        internal void Push()
        {
            if (_disposed)
            {
                return;
            }

            if (_tray == null)
            {
                WindowApi.SetTray(_windowPtr, 0, IntPtr.Zero, null);
                return;
            }

            IntPtr icon = _icons.Get(_tray.Icon, WindowsIconCache.SmallSize(_windowPtr));

            WindowApi.SetTray(_windowPtr, 1, icon, _tray.Tooltip ?? string.Empty);
        }

        internal void Raised(int kind, int x, int y)
        {
            TrayIcon tray = _tray;

            if (tray == null)
            {
                return;
            }

            if ((NativeTrayKind)kind == NativeTrayKind.Select)
            {
                if (tray.Command != null)
                {
                    tray.Command.Execute();
                }
                else
                {
                    _show();
                }

                return;
            }

            if ((NativeTrayKind)kind == NativeTrayKind.Menu)
            {
                Choose(tray.Menu, x, y)?.Execute();
            }
        }

        private Command Choose(IReadOnlyList<Command> menu, int x, int y)
        {
            if (menu == null || menu.Count == 0)
            {
                return null;
            }

            IntPtr window = WindowApi.GetWindowHandle(_windowPtr);
            IntPtr popup = CreatePopupMenu();

            try
            {
                for (int index = 0; index < menu.Count; index++)
                {
                    Command command = menu[index];

                    if (command == null)
                    {
                        AppendMenuW(popup, MF_SEPARATOR, UIntPtr.Zero, null);
                        continue;
                    }

                    uint flags = MF_STRING | (command.CanExecute(null) ? 0 : MF_GRAYED);

                    AppendMenuW(popup, flags, (UIntPtr)(uint)(index + 1), command.Label ?? string.Empty);
                }

                SetForegroundWindow(window);

                int picked = TrackPopupMenuEx(popup, TPM_RETURNCMD | TPM_RIGHTBUTTON | TPM_NONOTIFY, x, y, window,
                    IntPtr.Zero);

                PostMessage(window, WM_NULL, IntPtr.Zero, IntPtr.Zero);

                return picked > 0 && picked <= menu.Count ? menu[picked - 1] : null;
            }
            finally
            {
                DestroyMenu(popup);
            }
        }

        internal void Dispose()
        {
            _disposed = true;

            if (_tray != null)
            {
                _tray.Changed -= _changed;
            }
        }

        [DllImport("user32.dll")]
        private static extern IntPtr CreatePopupMenu();

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool AppendMenuW(IntPtr menu, uint flags, UIntPtr id, string text);

        [DllImport("user32.dll")]
        private static extern int TrackPopupMenuEx(IntPtr menu, uint flags, int x, int y, IntPtr window, IntPtr parameters);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool DestroyMenu(IntPtr menu);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool SetForegroundWindow(IntPtr window);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool PostMessage(IntPtr window, uint message, IntPtr wParam, IntPtr lParam);
    }
}
