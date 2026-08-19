using Android.Content;
using Ixen.Core;

namespace Ixen.View.Android
{
    internal class AndroidClipboard : IClipboard
    {
        private readonly Context _context;
        private readonly ClipboardManager _manager;

        internal AndroidClipboard(Context context)
        {
            _context = context;
            _manager = context?.GetSystemService(Context.ClipboardService) as ClipboardManager;
        }

        public string GetText()
        {
            ClipData data = _manager?.PrimaryClip;

            if (data == null || data.ItemCount == 0)
            {
                return null;
            }

            ClipData.Item item = data.GetItemAt(0);

            if (item == null)
            {
                return null;
            }

            return item.Text ?? item.CoerceToText(_context);
        }

        public void SetText(string text)
        {
            if (_manager == null)
            {
                return;
            }

            _manager.PrimaryClip = ClipData.NewPlainText("ixen", text ?? string.Empty);
        }
    }
}
