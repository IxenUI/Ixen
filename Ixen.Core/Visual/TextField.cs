using Ixen.Core.Input;
using Ixen.Core.Visual.Styles.Descriptors;
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
        private string _composition = string.Empty;
        private int _compositionCaret;

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

        private int[] _lineStarts;
        private float _desiredColumn = -1;

        internal int LineCount { get; set; }
        internal float LineHeight { get; set; }
        internal bool CaretMoved { get; set; }

        public bool Multiline { get; set; }

        internal int[] EnsureLineStarts(int count)
        {
            if (_lineStarts == null || _lineStarts.Length < count)
            {
                _lineStarts = new int[count];
            }

            return _lineStarts;
        }

        internal int LineAt(int index)
        {
            if (_lineStarts == null || LineCount <= 1)
            {
                return 0;
            }

            for (int line = LineCount - 1; line > 0; line--)
            {
                if (index >= _lineStarts[line])
                {
                    return line;
                }
            }

            return 0;
        }

        internal int LineStart(int line)
        {
            if (_lineStarts == null || line <= 0)
            {
                return 0;
            }

            return line >= LineCount ? _lineStarts[LineCount - 1] : _lineStarts[line];
        }

        internal int LineEnd(int line)
        {
            if (line + 1 >= LineCount)
            {
                return Value.Length;
            }

            return Math.Max(LineStart(line), LineStart(line + 1) - 1);
        }
        internal bool CaretVisible { get; set; } = true;
        internal bool IsFocused { get; private set; }

        internal IClipboard Clipboard => Host?.Clipboard;

        public TextField()
        {
            Focusable = true;
            Styles.Cursor = new CursorStyleDescriptor { Value = CursorKind.Text };
            Styles.TextAlign = new TextAlignStyleDescriptor
            {
                Horizontal = TextAlign.Left,
                Vertical = TextVAlign.Middle
            };

            TextInput += OnTextInput;
            KeyDown += OnKeyDown;
            PointerDown += OnPointerDown;
            PointerDrag += OnPointerDrag;
            PointerDoubleClick += OnPointerDoubleClick;
            GotFocus += (sender, args) => StartBlink();
            LostFocus += (sender, args) =>
            {
                CancelComposition();
                StopBlink();
            };
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
                Host?.InvalidateVisual(this);
            });
        }

        private void StopBlink()
        {
            IsFocused = false;
            CaretVisible = true;

            _blink?.Dispose();
            _blink = null;

            Host?.InvalidateVisual(this);
        }

        private void ShowCaret()
        {
            if (CaretVisible)
            {
                return;
            }

            CaretVisible = true;
            Host?.InvalidateVisual(this);
        }

        protected internal override void OnHostChanged()
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

        internal bool IsComposing => _composition.Length > 0;

        internal int CompositionStart => _caret;

        internal int CompositionLength => _composition.Length;

        public void SetComposition(string text, int caret)
        {
            string composing = text ?? string.Empty;

            if (!IsComposing && composing.Length > 0 && SelectionLength > 0)
            {
                string value = Value;
                int start = SelectionStart;

                Replace(value.Substring(0, start) + value.Substring(start + SelectionLength), start);
            }

            _composition = composing;
            _compositionCaret = Clamp(caret, composing.Length);

            AfterComposing();
        }

        public void CommitComposition(string text)
        {
            _composition = string.Empty;
            _compositionCaret = 0;

            if (string.IsNullOrEmpty(text))
            {
                AfterComposing();
                return;
            }

            Insert(text);
            AfterComposing();
        }

        public void CancelComposition()
        {
            if (!IsComposing)
            {
                return;
            }

            _composition = string.Empty;
            _compositionCaret = 0;

            AfterComposing();
        }

        private void AfterComposing()
        {
            CaretMoved = true;

            ShowCaret();
            InvalidateLayout();
        }

        internal bool IsMasked => _passwordChar != '\0';

        internal string DisplayText
        {
            get
            {
                string value = IsMasked ? new string(_passwordChar, Value.Length) : Value;

                if (!IsComposing)
                {
                    return value;
                }

                string composing = IsMasked
                    ? new string(_passwordChar, _composition.Length)
                    : _composition;

                return value.Insert(_caret, composing);
            }
        }

        internal int DisplayCaret => _caret + _compositionCaret;

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
            _desiredColumn = -1;
            CaretMoved = true;

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

                case Key.Up:
                    if (!Multiline)
                    {
                        return;
                    }

                    MoveByLine(-1, extend);
                    break;

                case Key.Down:
                    if (!Multiline)
                    {
                        return;
                    }

                    MoveByLine(1, extend);
                    break;

                case Key.Enter:
                    if (!Multiline)
                    {
                        return;
                    }

                    Insert("\n");
                    break;

                case Key.Home:
                    MoveCaret(args.HasModifier(KeyModifiers.Control) ? 0 : LineStart(LineAt(_caret)), extend);
                    break;

                case Key.End:
                    MoveCaret(args.HasModifier(KeyModifiers.Control) ? Value.Length : LineEnd(LineAt(_caret)), extend);
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

        private void MoveByLine(int delta, bool extend)
        {
            int line = LineAt(_caret);
            int target = line + delta;

            if (target < 0 || target >= LineCount)
            {
                MoveCaret(delta < 0 ? 0 : Value.Length, extend);
                return;
            }

            float column = _desiredColumn < 0 ? OffsetAt(_caret) : _desiredColumn;
            int index = IndexInLine(target, column);

            MoveCaret(index, extend);
            _desiredColumn = column;
        }

        private int IndexInLine(int line, float column)
        {
            int from = LineStart(line);
            int to = LineEnd(line);
            int best = from;
            float bestDistance = Math.Abs(column - OffsetAt(from));

            for (int i = from + 1; i <= to; i++)
            {
                float distance = Math.Abs(column - OffsetAt(i));

                if (distance >= bestDistance)
                {
                    continue;
                }

                best = i;
                bestDistance = distance;
            }

            return best;
        }

        private void OnPointerDown(object sender, PointerEventArgs args)
        {
            int index = IndexAt(args.X, args.Y);

            Select(index, index);
            args.Handled = true;
        }

        private void OnPointerDrag(object sender, DragEventArgs args)
        {
            Select(IndexAt(args.X, args.Y), _anchor);
            args.Handled = true;
        }

        private void OnPointerDoubleClick(object sender, PointerEventArgs args)
        {
            int index = IndexAt(args.X, args.Y);

            Select(WordEnd(index), WordStart(index));
            args.Handled = true;
        }

        internal int IndexAt(float surfaceX, float surfaceY)
        {
            if (CaretOffsets == null || CaretOffsetCount == 0)
            {
                return 0;
            }

            float column = surfaceX - (X + PaddingLeft + BorderInsideLeft) + ContentOffset;

            if (!Multiline || LineCount <= 1 || LineHeight <= 0)
            {
                return IndexInLine(0, column);
            }

            float local = surfaceY - (Y + PaddingTop + BorderInsideTop) + ScrollY;
            int line = (int)(local / LineHeight);

            if (line < 0)
            {
                line = 0;
            }
            else if (line >= LineCount)
            {
                line = LineCount - 1;
            }

            return IndexInLine(line, column);
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
