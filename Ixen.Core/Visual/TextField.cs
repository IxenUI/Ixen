using Ixen.Core.Input;
using System;
using System.Collections.Generic;

namespace Ixen.Core.Visual
{
    public class TextField : VisualElement
    {
        public event EventHandler<EventArgs> TextChanged;

        public const char DEFAULT_PASSWORD_CHAR = '\u25CF';

        private const int UNDO_LIMIT = 100;

        private int _caret;
        private int _anchor;

        private char _passwordChar;
        private string _placeholder;

        private readonly List<Snapshot> _undo = new List<Snapshot>();
        private readonly List<Snapshot> _redo = new List<Snapshot>();

        private bool _editing;
        private bool _coalescing;

        private struct Snapshot
        {
            internal string Text;
            internal int Caret;
            internal int Anchor;
        }

        internal float[] CaretOffsets { get; set; }
        internal int CaretOffsetCount { get; set; }
        internal float ContentOffset { get; set; }
        internal bool CaretVisible { get; set; } = true;
        internal bool IsFocused { get; private set; }

        internal IClipboard Clipboard => Host?.Clipboard;

        public TextField()
        {
            Focusable = true;

            TextInput += OnTextInput;
            KeyDown += OnKeyDown;
            PointerDown += OnPointerDown;
            PointerDrag += OnPointerDrag;
            PointerDoubleClick += OnPointerDoubleClick;
            GotFocus += (sender, args) => StartBlink();
            LostFocus += (sender, args) => StopBlink();
        }

        private const int BLINK_DELAY = 530;

        private IDisposable _blink;

        private void StartBlink()
        {
            IsFocused = true;
            ShowCaret();

            _blink = Host?.Scheduler?.Schedule(BLINK_DELAY, true, () =>
            {
                CaretVisible = !CaretVisible;
                Host?.InvalidateVisual();
            });
        }

        private void StopBlink()
        {
            IsFocused = false;
            CaretVisible = true;

            _blink?.Dispose();
            _blink = null;

            Host?.InvalidateVisual();
        }

        private void ShowCaret()
        {
            if (CaretVisible)
            {
                return;
            }

            CaretVisible = true;
            Host?.InvalidateVisual();
        }

        internal override void OnHostChanged()
        {
            if (Host == null)
            {
                StopBlink();
            }
        }

        public override string Text
        {
            get => base.Text;
            set
            {
                base.Text = value;
                ClampCaret();

                if (_editing)
                {
                    return;
                }

                _undo.Clear();
                _redo.Clear();
                _coalescing = false;
            }
        }

        public char PasswordChar
        {
            get => _passwordChar;
            set
            {
                if (_passwordChar == value)
                {
                    return;
                }

                _passwordChar = value;
                InvalidateLayout();
            }
        }

        public bool Password
        {
            get => IsMasked;
            set
            {
                if (value == IsMasked)
                {
                    return;
                }

                PasswordChar = value ? DEFAULT_PASSWORD_CHAR : '\0';
            }
        }

        public string Placeholder
        {
            get => _placeholder;
            set
            {
                if (_placeholder == value)
                {
                    return;
                }

                _placeholder = value;
                InvalidateLayout();
            }
        }

        internal bool IsMasked => _passwordChar != '\0';

        internal string DisplayText
            => IsMasked ? new string(_passwordChar, Value.Length) : Value;

        internal bool ShowsPlaceholder
            => Value.Length == 0 && !string.IsNullOrEmpty(_placeholder);

        public bool CanUndo => _undo.Count > 0;
        public bool CanRedo => _redo.Count > 0;

        public void Undo() => Step(_undo, _redo);

        public void Redo() => Step(_redo, _undo);

        private void Step(List<Snapshot> from, List<Snapshot> to)
        {
            if (from.Count == 0)
            {
                return;
            }

            Snapshot snapshot = from[from.Count - 1];
            from.RemoveAt(from.Count - 1);

            to.Add(Current());
            Apply(snapshot);

            _coalescing = false;
            TextChanged?.Invoke(this, EventArgs.Empty);
        }

        private Snapshot Current()
            => new Snapshot { Text = Value, Caret = _caret, Anchor = _anchor };

        private void Apply(Snapshot snapshot)
        {
            _editing = true;
            Text = snapshot.Text;
            _editing = false;

            _caret = Clamp(snapshot.Caret, Value.Length);
            _anchor = Clamp(snapshot.Anchor, Value.Length);
        }

        private void PushUndo(bool coalesce)
        {
            if (coalesce && _coalescing)
            {
                return;
            }

            _undo.Add(Current());
            _redo.Clear();
            _coalescing = coalesce;

            if (_undo.Count > UNDO_LIMIT)
            {
                _undo.RemoveAt(0);
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
            _coalescing = false;

            InvalidateLayout();
        }

        public void SelectAll() => Select(Value.Length, 0);

        public void Copy()
        {
            if (Clipboard != null && SelectionLength > 0 && !IsMasked)
            {
                Clipboard.SetText(SelectedText);
            }
        }

        public void Cut()
        {
            if (Clipboard == null || SelectionLength == 0 || IsMasked)
            {
                return;
            }

            Copy();
            Delete(false);
        }

        public void Paste()
        {
            string text = Clipboard?.GetText();

            if (string.IsNullOrEmpty(text))
            {
                return;
            }

            Insert(Printable(text));
        }

        private static string Printable(string text)
        {
            var builder = new System.Text.StringBuilder(text.Length);

            foreach (char c in text)
            {
                if (!char.IsControl(c))
                {
                    builder.Append(c);
                }
            }

            return builder.ToString();
        }

        public void Insert(string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                return;
            }

            string value = Value;
            int start = SelectionStart;
            int length = SelectionLength;

            Replace(value.Substring(0, start) + text + value.Substring(start + length), start + text.Length,
                text.Length == 1 && length == 0 && !char.IsWhiteSpace(text[0]));
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

        private void Replace(string value, int caret, bool coalesce = false)
        {
            PushUndo(coalesce);

            _caret = caret;
            _anchor = caret;

            _editing = true;
            Text = value;
            _editing = false;

            _caret = Clamp(caret, Value.Length);
            _anchor = _caret;

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
            ShowCaret();
            args.Handled = true;
        }

        private void OnKeyDown(object sender, KeyEventArgs args)
        {
            bool extend = args.HasModifier(KeyModifiers.Shift);

            ShowCaret();

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

                case Key.C:
                    if (!args.HasModifier(KeyModifiers.Control))
                    {
                        return;
                    }

                    Copy();
                    break;

                case Key.X:
                    if (!args.HasModifier(KeyModifiers.Control))
                    {
                        return;
                    }

                    Cut();
                    break;

                case Key.V:
                    if (!args.HasModifier(KeyModifiers.Control))
                    {
                        return;
                    }

                    Paste();
                    break;

                case Key.Z:
                    if (!args.HasModifier(KeyModifiers.Control))
                    {
                        return;
                    }

                    if (args.HasModifier(KeyModifiers.Shift))
                    {
                        Redo();
                    }
                    else
                    {
                        Undo();
                    }

                    break;

                case Key.Y:
                    if (!args.HasModifier(KeyModifiers.Control))
                    {
                        return;
                    }

                    Redo();
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
