using Android.Views;
using Android.Views.InputMethods;
using Ixen.Core.Input;
using Ixen.Core.Visual;
using Ixen.Platform;
using System;

namespace Ixen.View.Android
{
    internal class IxenInputConnection : BaseInputConnection
    {
        private readonly IxenView _view;
        private readonly IxenHost _host;

        internal IxenInputConnection(IxenView view, IxenHost host)
            : base(view, true)
        {
            _view = view;
            _host = host;
        }

        private TextField Field => _host.FocusedElement as TextField;

        private static int CaretFor(string text, int newCursorPosition)
        {
            int caret = newCursorPosition > 0
                ? text.Length + newCursorPosition - 1
                : newCursorPosition;

            return Math.Max(0, Math.Min(caret, text.Length));
        }

        public override bool SetComposingText(Java.Lang.ICharSequence text, int newCursorPosition)
        {
            string value = text?.ToString() ?? string.Empty;

            _host.Composition(value, CaretFor(value, newCursorPosition));

            return true;
        }

        public override bool CommitText(Java.Lang.ICharSequence text, int newCursorPosition)
        {
            _host.CommitComposition(text?.ToString() ?? string.Empty);

            return true;
        }

        public override bool FinishComposingText()
        {
            _host.FinishComposition();

            return true;
        }

        public override bool SetComposingRegion(int start, int end)
        {
            return false;
        }

        public override bool DeleteSurroundingText(int beforeLength, int afterLength)
        {
            for (int index = 0; index < beforeLength; index++)
            {
                Press(Key.Backspace);
            }

            for (int index = 0; index < afterLength; index++)
            {
                Press(Key.Delete);
            }

            return true;
        }

        private void Press(Key key)
        {
            _host.KeyDown(key, KeyModifiers.None, false);
            _host.KeyUp(key, KeyModifiers.None);
        }

        public override bool SendKeyEvent(KeyEvent e)
        {
            return _view.DispatchKeyEvent(e);
        }

        public override bool PerformEditorAction(ImeAction actionCode)
        {
            Press(Key.Enter);

            return true;
        }

        public override Java.Lang.ICharSequence GetTextBeforeCursorFormatted(int n, GetTextFlags flags)
        {
            TextField field = Field;

            if (field == null || field.IsMasked || n <= 0)
            {
                return new Java.Lang.String(string.Empty);
            }

            string value = field.Text ?? string.Empty;
            int end = Math.Max(0, Math.Min(field.CaretIndex, value.Length));
            int start = Math.Max(0, end - n);

            return new Java.Lang.String(value.Substring(start, end - start));
        }

        public override Java.Lang.ICharSequence GetTextAfterCursorFormatted(int n, GetTextFlags flags)
        {
            TextField field = Field;

            if (field == null || field.IsMasked || n <= 0)
            {
                return new Java.Lang.String(string.Empty);
            }

            string value = field.Text ?? string.Empty;
            int start = Math.Max(0, Math.Min(field.CaretIndex, value.Length));
            int length = Math.Min(n, value.Length - start);

            return new Java.Lang.String(value.Substring(start, length));
        }

        public override Java.Lang.ICharSequence GetSelectedTextFormatted(GetTextFlags flags)
        {
            TextField field = Field;

            if (field == null || field.IsMasked || field.SelectionLength == 0)
            {
                return null;
            }

            string value = field.Text ?? string.Empty;
            int start = Math.Max(0, Math.Min(field.SelectionStart, value.Length));
            int length = Math.Min(field.SelectionLength, value.Length - start);

            return new Java.Lang.String(value.Substring(start, length));
        }
    }
}
