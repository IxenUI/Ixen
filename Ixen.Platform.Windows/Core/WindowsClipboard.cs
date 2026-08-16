using Ixen.Core;
using System;
using System.Runtime.InteropServices;

namespace Ixen.Platform.Windows
{
    internal sealed class WindowsClipboard : IClipboard
    {
        private const uint CF_UNICODETEXT = 13;
        private const uint GMEM_MOVEABLE = 0x0002;

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool OpenClipboard(IntPtr owner);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool CloseClipboard();

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool EmptyClipboard();

        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr GetClipboardData(uint format);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr SetClipboardData(uint format, IntPtr data);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool IsClipboardFormatAvailable(uint format);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr GlobalAlloc(uint flags, UIntPtr bytes);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr GlobalFree(IntPtr handle);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr GlobalLock(IntPtr handle);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool GlobalUnlock(IntPtr handle);

        public string GetText()
        {
            if (!IsClipboardFormatAvailable(CF_UNICODETEXT) || !OpenClipboard(IntPtr.Zero))
            {
                return null;
            }

            try
            {
                IntPtr handle = GetClipboardData(CF_UNICODETEXT);

                if (handle == IntPtr.Zero)
                {
                    return null;
                }

                IntPtr pointer = GlobalLock(handle);

                if (pointer == IntPtr.Zero)
                {
                    return null;
                }

                try
                {
                    return Marshal.PtrToStringUni(pointer);
                }
                finally
                {
                    GlobalUnlock(handle);
                }
            }
            finally
            {
                CloseClipboard();
            }
        }

        public void SetText(string text)
        {
            if (string.IsNullOrEmpty(text) || !OpenClipboard(IntPtr.Zero))
            {
                return;
            }

            IntPtr handle = IntPtr.Zero;

            try
            {
                EmptyClipboard();

                var bytes = (UIntPtr)((text.Length + 1) * 2);
                handle = GlobalAlloc(GMEM_MOVEABLE, bytes);

                if (handle == IntPtr.Zero)
                {
                    return;
                }

                IntPtr pointer = GlobalLock(handle);

                if (pointer == IntPtr.Zero)
                {
                    return;
                }

                try
                {
                    Marshal.Copy(text.ToCharArray(), 0, pointer, text.Length);
                    Marshal.WriteInt16(pointer, text.Length * 2, 0);
                }
                finally
                {
                    GlobalUnlock(handle);
                }

                if (SetClipboardData(CF_UNICODETEXT, handle) != IntPtr.Zero)
                {
                    handle = IntPtr.Zero;
                }
            }
            finally
            {
                if (handle != IntPtr.Zero)
                {
                    GlobalFree(handle);
                }

                CloseClipboard();
            }
        }
    }
}
