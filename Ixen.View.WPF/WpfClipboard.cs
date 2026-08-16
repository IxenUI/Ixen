using Ixen.Core;

namespace Ixen.View.WPF
{
    internal sealed class WpfClipboard : IClipboard
    {
        public string GetText()
        {
            try
            {
                return System.Windows.Clipboard.ContainsText() ? System.Windows.Clipboard.GetText() : null;
            }
            catch
            {
                return null;
            }
        }

        public void SetText(string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                return;
            }

            try
            {
                System.Windows.Clipboard.SetText(text);
            }
            catch
            {
            }
        }
    }
}
