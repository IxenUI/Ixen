using Ixen.Core;
using Ixen.Platform.Mac.NativeApi;
using System;
using System.Runtime.InteropServices;

namespace Ixen.Platform.Mac
{
    public class MacClipboard : IClipboard
    {
        public string GetText()
        {
            IntPtr text = MacApi.GetClipboardText();

            return text == IntPtr.Zero ? null : Marshal.PtrToStringUTF8(text);
        }

        public void SetText(string text) => MacApi.SetClipboardText(text ?? string.Empty);
    }
}
