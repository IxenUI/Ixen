using Ixen.Core;
using Ixen.Platform.Linux.NativeApi;
using System;
using System.Runtime.InteropServices;

namespace Ixen.Platform.Linux
{
    public class LinuxClipboard : IClipboard
    {
        private readonly Func<IntPtr> _window;

        public LinuxClipboard(Func<IntPtr> window)
        {
            _window = window;
        }

        public string GetText()
        {
            IntPtr window = _window();

            if (window == IntPtr.Zero)
            {
                return null;
            }

            IntPtr text = LinuxApi.GetClipboardText(window);

            return text == IntPtr.Zero ? null : Marshal.PtrToStringUTF8(text);
        }

        public void SetText(string text)
        {
            IntPtr window = _window();

            if (window == IntPtr.Zero)
            {
                return;
            }

            LinuxApi.SetClipboardText(window, text ?? string.Empty);
        }
    }
}
