using Ixen.Core;
using UIKit;

namespace Ixen.View.iOS
{
    internal class IosClipboard : IClipboard
    {
        public string GetText()
        {
            return UIPasteboard.General.String;
        }

        public void SetText(string text)
        {
            UIPasteboard.General.String = text ?? string.Empty;
        }
    }
}
