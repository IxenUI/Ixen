using CoreGraphics;
using Foundation;
using Ixen.Core.Visual;
using System;
using UIKit;

namespace Ixen.View.iOS
{
    internal sealed partial class TextInputView
    {
        private const float CARET_WIDTH = 2f;

        private UITextInputStringTokenizer _tokenizer;
        private string _known;

        private string Document => Field?.DisplayText ?? string.Empty;

        private int Clamp(int index)
        {
            int length = Document.Length;

            return index < 0 ? 0 : index > length ? length : index;
        }

        private static int IndexOf(UITextPosition position) => (position as IxenTextPosition)?.Index ?? 0;

        [Export("inputDelegate")]
        public IUITextInputDelegate InputDelegate { get; set; }

        [Export("markedTextStyle")]
        public NSDictionary MarkedTextStyle { get; set; }

        [Export("tokenizer")]
        public IUITextInputTokenizer Tokenizer => _tokenizer ??= new UITextInputStringTokenizer(this);

        [Export("beginningOfDocument")]
        public UITextPosition BeginningOfDocument => new IxenTextPosition(0);

        [Export("endOfDocument")]
        public UITextPosition EndOfDocument => new IxenTextPosition(Document.Length);

        [Export("textInRange:")]
        public string TextInRange(UITextRange range)
        {
            if (!(range is IxenTextRange text))
            {
                return string.Empty;
            }

            string document = Document;
            int start = Clamp(text.StartIndex);
            int end = Clamp(text.EndIndex);

            return document.Substring(start, end - start);
        }

        [Export("selectedTextRange")]
        public UITextRange SelectedTextRange
        {
            get
            {
                TextField edited = Field;

                if (edited == null)
                {
                    return new IxenTextRange(0, 0);
                }

                if (edited.IsComposing)
                {
                    return new IxenTextRange(edited.DisplayCaret, edited.DisplayCaret);
                }

                return new IxenTextRange(edited.SelectionStart, edited.SelectionStart + edited.SelectionLength);
            }
            set
            {
                TextField edited = Field;

                if (edited == null || edited.IsComposing || !(value is IxenTextRange range))
                {
                    return;
                }

                edited.Select(Clamp(range.EndIndex), Clamp(range.StartIndex));
                Edited();
            }
        }

        [Export("markedTextRange")]
        public UITextRange MarkedTextRange
        {
            get
            {
                TextField edited = Field;

                if (edited == null || !edited.IsComposing)
                {
                    return null;
                }

                return new IxenTextRange(edited.CompositionStart, edited.CompositionStart + edited.CompositionLength);
            }
        }

        [Export("setMarkedText:selectedRange:")]
        public void SetMarkedText(string markedText, NSRange selectedRange)
        {
            if (Field == null)
            {
                return;
            }

            string text = markedText ?? string.Empty;
            int caret = (int)(selectedRange.Location + selectedRange.Length);

            _host.Composition(text, Math.Max(0, Math.Min(caret, text.Length)));
            Edited();
        }

        [Export("unmarkText")]
        public void UnmarkText()
        {
            if (Field == null)
            {
                return;
            }

            _host.FinishComposition();
            Edited();
        }

        [Export("replaceRange:withText:")]
        public void ReplaceText(UITextRange range, string text)
        {
            TextField edited = Field;

            if (edited == null || !(range is IxenTextRange replaced))
            {
                return;
            }

            if (edited.IsComposing)
            {
                edited.CommitComposition();
            }

            int start = Clamp(replaced.StartIndex);
            int end = Clamp(replaced.EndIndex);

            edited.Select(end, start);

            if (string.IsNullOrEmpty(text))
            {
                if (end > start)
                {
                    edited.Delete(false);
                }
            }
            else
            {
                edited.Insert(text);
            }

            Edited();
        }

        [Export("textRangeFromPosition:toPosition:")]
        public UITextRange GetTextRange(UITextPosition fromPosition, UITextPosition toPosition)
        {
            return new IxenTextRange(IndexOf(fromPosition), IndexOf(toPosition));
        }

        [Export("positionFromPosition:offset:")]
        public UITextPosition GetPosition(UITextPosition fromPosition, nint offset)
        {
            int index = IndexOf(fromPosition) + (int)offset;

            return index < 0 || index > Document.Length ? null : new IxenTextPosition(index);
        }

        [Export("positionFromPosition:inDirection:offset:")]
        public UITextPosition GetPosition(UITextPosition fromPosition, UITextLayoutDirection inDirection, nint offset)
        {
            int index = IndexOf(fromPosition);
            int steps = (int)offset;

            switch (inDirection)
            {
                case UITextLayoutDirection.Left:
                    return new IxenTextPosition(Clamp(index - steps));

                case UITextLayoutDirection.Right:
                    return new IxenTextPosition(Clamp(index + steps));

                case UITextLayoutDirection.Up:
                    return new IxenTextPosition(MoveLines(index, -steps));

                case UITextLayoutDirection.Down:
                    return new IxenTextPosition(MoveLines(index, steps));
            }

            return null;
        }

        private int MoveLines(int index, int lines)
        {
            TextField edited = Field;

            if (edited == null || !edited.Multiline || edited.LineCount <= 1)
            {
                return lines < 0 ? 0 : Document.Length;
            }

            int line = edited.LineAt(index);
            int target = line + lines;

            if (target < 0)
            {
                return 0;
            }

            if (target >= edited.LineCount)
            {
                return Document.Length;
            }

            int column = index - edited.LineStart(line);
            int start = edited.LineStart(target);
            int end = target == edited.LineCount - 1 ? Document.Length : edited.LineEnd(target);

            return Clamp(Math.Min(start + column, end));
        }

        [Export("comparePosition:toPosition:")]
        public NSComparisonResult ComparePosition(UITextPosition first, UITextPosition second)
        {
            int left = IndexOf(first);
            int right = IndexOf(second);

            return left < right ? NSComparisonResult.Ascending
                : left > right ? NSComparisonResult.Descending
                : NSComparisonResult.Same;
        }

        [Export("offsetFromPosition:toPosition:")]
        public nint GetOffsetFromPosition(UITextPosition fromPosition, UITextPosition toPosition)
        {
            return IndexOf(toPosition) - IndexOf(fromPosition);
        }

        [Export("positionWithinRange:farthestInDirection:")]
        public UITextPosition GetPositionWithinRange(UITextRange range, UITextLayoutDirection direction)
        {
            if (!(range is IxenTextRange text))
            {
                return null;
            }

            bool backwards = direction == UITextLayoutDirection.Left || direction == UITextLayoutDirection.Up;

            return new IxenTextPosition(backwards ? text.StartIndex : text.EndIndex);
        }

        [Export("characterRangeByExtendingPosition:inDirection:")]
        public UITextRange GetCharacterRange(UITextPosition byExtendingPosition, UITextLayoutDirection direction)
        {
            int index = IndexOf(byExtendingPosition);
            bool backwards = direction == UITextLayoutDirection.Left || direction == UITextLayoutDirection.Up;

            return backwards
                ? new IxenTextRange(Clamp(index - 1), index)
                : new IxenTextRange(index, Clamp(index + 1));
        }

        [Export("baseWritingDirectionForPosition:inDirection:")]
        public NSWritingDirection GetBaseWritingDirection(UITextPosition forPosition, UITextStorageDirection direction)
        {
            return NSWritingDirection.LeftToRight;
        }

        [Export("setBaseWritingDirection:forRange:")]
        public void SetBaseWritingDirectionforRange(NSWritingDirection writingDirection, UITextRange range)
        { }

        private CGRect CaretRect(TextField edited, int index)
        {
            edited.CaretBounds(index, out float x, out float y, out float height);

            return new CGRect(x, y, CARET_WIDTH, height);
        }

        [Export("caretRectForPosition:")]
        public CGRect GetCaretRectForPosition(UITextPosition position)
        {
            TextField edited = Field;

            return edited == null ? CGRect.Empty : CaretRect(edited, Clamp(IndexOf(position)));
        }

        [Export("firstRectForRange:")]
        public CGRect GetFirstRectForRange(UITextRange range)
        {
            TextField edited = Field;

            if (edited == null || !(range is IxenTextRange text))
            {
                return CGRect.Empty;
            }

            int start = Clamp(text.StartIndex);
            int end = Clamp(text.EndIndex);

            if (edited.Multiline && edited.LineCount > 1)
            {
                int line = edited.LineAt(start);

                if (line < edited.LineCount - 1)
                {
                    end = Math.Min(end, edited.LineEnd(line));
                }
            }

            CGRect first = CaretRect(edited, start);
            CGRect last = CaretRect(edited, end);

            return new CGRect(first.X, first.Y, Math.Max(CARET_WIDTH, (float)(last.X - first.X)), first.Height);
        }

        [Export("selectionRectsForRange:")]
        public UITextSelectionRect[] GetSelectionRects(UITextRange range)
        {
            if (Field == null || !(range is IxenTextRange))
            {
                return Array.Empty<UITextSelectionRect>();
            }

            return new UITextSelectionRect[] { new IxenSelectionRect(GetFirstRectForRange(range), true, true) };
        }

        [Export("closestPositionToPoint:")]
        public UITextPosition GetClosestPositionToPoint(CGPoint point)
        {
            TextField edited = Field;

            return new IxenTextPosition(edited == null ? 0 : Clamp(edited.IndexAt((float)point.X, (float)point.Y)));
        }

        [Export("closestPositionToPoint:withinRange:")]
        public UITextPosition GetClosestPositionToPoint(CGPoint point, UITextRange withinRange)
        {
            int index = IndexOf(GetClosestPositionToPoint(point));

            if (withinRange is IxenTextRange range)
            {
                index = Math.Max(range.StartIndex, Math.Min(index, range.EndIndex));
            }

            return new IxenTextPosition(index);
        }

        [Export("characterRangeAtPoint:")]
        public UITextRange GetCharacterRangeAtPoint(CGPoint point)
        {
            int index = IndexOf(GetClosestPositionToPoint(point));

            return new IxenTextRange(index, Clamp(index + 1));
        }

        private string Snapshot()
        {
            TextField edited = Field;

            if (edited == null)
            {
                return null;
            }

            return edited.DisplayText + "\u0000" + edited.SelectionStart + ":" + edited.SelectionLength + ":" + edited.DisplayCaret;
        }

        private void Edited()
        {
            _known = Snapshot();
            _repaint();
        }

        private void TellInputAboutOutsideChanges()
        {
            string now = Snapshot();

            if (now == _known)
            {
                return;
            }

            _known = now;

            IUITextInputDelegate input = InputDelegate;

            if (input == null)
            {
                return;
            }

            input.SelectionWillChange(this);
            input.TextWillChange(this);
            input.TextDidChange(this);
            input.SelectionDidChange(this);
        }
    }
}
