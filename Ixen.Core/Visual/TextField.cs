using Ixen.Core.Input;
using System;

namespace Ixen.Core.Visual
{
    public class TextField : VisualElement
    {
        public event EventHandler<EventArgs> TextChanged;

        private int _caret;
        private int _anchor;

        internal float[] CaretOffsets { get; set; }
        internal int CaretOffsetCount { get; set; }
        internal float ContentOffset { get; set; }
        internal bool CaretVisible { get; set; } = true;
        internal bool IsFocused { get; set; }

        public TextField()
        {
            Focusable = true;

            TextInput += OnTextInput;
            KeyDown += OnKeyDown;
            PointerDown += OnPointerDown;
            PointerDrag += OnPointerDrag;
            PointerDoubleClick += OnPointerDoubleClick;
        }

        public override string Text
        {
            get => base.Text;
            set
            {
                base.Text = value;
                ClampCaret();
            }
        }

        public int CaretIndex
        {
            get => _caret;
            set => Select(value, value);
        }

        public int SelectionStart => Math.Min(_caret, _anchor);
        public int SelectionLength => Math.Abs(_caret - _anchor);

        public string SelectedText
            => SelectionLength == 0 ? string.Empty : Value.Substring(SelectionStart, SelectionLength);

        private string Value => Text ?? string.Empty;

        public void Select(int caret, int anchor)
        {
            int length = Value.Length;

            _caret = Clamp(caret, length);
            _anchor = Clamp(anchor, length);

            InvalidateLayout();
        }

        public void SelectAll() => Select(Value.Length, 0);

        public void Insert(string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                return;
            }

            string value = Value;
            int start = SelectionStart;
            int length = SelectionLength;

            Replace(value.Substring(0, start) + text + value.Substring(start + length), start + text.Length);
        }

        public void Delete(bool forward)
        {
            string value = Value;

            if (SelectionLength > 0)
            {
                int start = SelectionStart;
                Replace(value.Substring(0, start) + value.Substring(start + SelectionLength), start);
                return;
            }

            if (forward)
            {
                if (_caret < value.Length)
                {
                    Replace(value.Substring(0, _caret) + value.Substring(_caret + 1), _caret);
                }

                return;
            }

            if (_caret > 0)
            {
                Replace(value.Substring(0, _caret - 1) + value.Substring(_caret), _caret - 1);
            }
        }

        private void Replace(string value, int caret)
        {
            _caret = caret;
            _anchor = caret;

            Text = value;
            TextChanged?.Invoke(this, EventArgs.Empty);
        }

        private void ClampCaret()
        {
            int length = Value.Length;

            _caret = Clamp(_caret, length);
            _anchor = Clamp(_anchor, length);
        }

        private static int Clamp(int value, int length)
        {
            if (value < 0)
            {
                return 0;
            }

            return value > length ? length : value;
        }

        private void OnTextInput(object sender, TextInputEventArgs args)
        {
            Insert(args.Text);
            args.Handled = true;
        }

        private void OnKeyDown(object sender, KeyEventArgs args)
        {
            bool extend = args.HasModifier(KeyModifiers.Shift);

            switch (args.Key)
            {
                case Key.Backspace:
                    Delete(false);
                    break;

                case Key.Delete:
                    Delete(true);
                    break;

                case Key.Left:
                    MoveCaret(PreviousIndex(args), extend);
                    break;

                case Key.Right:
                    MoveCaret(NextIndex(args), extend);
                    break;

                case Key.Home:
                    MoveCaret(0, extend);
                    break;

                case Key.End:
                    MoveCaret(Value.Length, extend);
                    break;

                case Key.A:
                    if (!args.HasModifier(KeyModifiers.Control))
                    {
                        return;
                    }

                    SelectAll();
                    break;

                default:
                    return;
            }

            args.Handled = true;
        }

        private int PreviousIndex(KeyEventArgs args)
        {
            if (!args.HasModifier(KeyModifiers.Control))
            {
                return SelectionLength > 0 && !args.HasModifier(KeyModifiers.Shift) ? SelectionStart : _caret - 1;
            }

            return WordStart(_caret);
        }

        private int NextIndex(KeyEventArgs args)
        {
            if (!args.HasModifier(KeyModifiers.Control))
            {
                return SelectionLength > 0 && !args.HasModifier(KeyModifiers.Shift)
                    ? SelectionStart + SelectionLength
                    : _caret + 1;
            }

            return WordEnd(_caret);
        }

        private void MoveCaret(int index, bool extend)
            => Select(index, extend ? _anchor : index);

        private void OnPointerDown(object sender, PointerEventArgs args)
        {
            int index = IndexAt(args.X);

            Select(index, index);
            args.Handled = true;
        }

        private void OnPointerDrag(object sender, DragEventArgs args)
        {
            Select(IndexAt(args.X), _anchor);
            args.Handled = true;
        }

        private void OnPointerDoubleClick(object sender, PointerEventArgs args)
        {
            int index = IndexAt(args.X);

            Select(WordEnd(index), WordStart(index));
            args.Handled = true;
        }

        internal int IndexAt(float surfaceX)
        {
            float[] offsets = CaretOffsets;

            if (offsets == null || CaretOffsetCount == 0)
            {
                return 0;
            }

            float local = surfaceX - (X + PaddingLeft + BorderInsideLeft) + ContentOffset;
            int best = 0;
            float bestDistance = Math.Abs(local - offsets[0]);

            for (int i = 1; i < CaretOffsetCount; i++)
            {
                float distance = Math.Abs(local - offsets[i]);

                if (distance >= bestDistance)
                {
                    continue;
                }

                best = i;
                bestDistance = distance;
            }

            return best;
        }

        internal float OffsetAt(int index)
        {
            float[] offsets = CaretOffsets;

            if (offsets == null || CaretOffsetCount == 0)
            {
                return 0;
            }

            return offsets[Clamp(index, CaretOffsetCount - 1)];
        }

        private int WordStart(int index)
        {
            string value = Value;
            int i = Clamp(index, value.Length);

            while (i > 0 && char.IsWhiteSpace(value[i - 1]))
            {
                i--;
            }

            while (i > 0 && !char.IsWhiteSpace(value[i - 1]))
            {
                i--;
            }

            return i;
        }

        private int WordEnd(int index)
        {
            string value = Value;
            int i = Clamp(index, value.Length);

            while (i < value.Length && char.IsWhiteSpace(value[i]))
            {
                i++;
            }

            while (i < value.Length && !char.IsWhiteSpace(value[i]))
            {
                i++;
            }

            return i;
        }
    }
}
