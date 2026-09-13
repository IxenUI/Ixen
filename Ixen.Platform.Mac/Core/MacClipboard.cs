using Ixen.Core;
using Ixen.Platform.Mac.NativeApi;

namespace Ixen.Platform.Mac
{
    public class MacClipboard : IClipboard
    {
        public string GetText() => MacApi.GetClipboardText();

        public void SetText(string text) => MacApi.SetClipboardText(text ?? string.Empty);
    }
}
