using CoreGraphics;
using Foundation;
using UIKit;

namespace Ixen.View.iOS
{
    internal sealed class IxenTextPosition : UITextPosition
    {
        internal IxenTextPosition(int index)
        {
            Index = index;
        }

        internal int Index { get; }
    }

    internal sealed class IxenTextRange : UITextRange
    {
        private readonly IxenTextPosition _start;
        private readonly IxenTextPosition _end;

        internal IxenTextRange(int start, int end)
        {
            if (end < start)
            {
                (start, end) = (end, start);
            }

            _start = new IxenTextPosition(start);
            _end = new IxenTextPosition(end);
        }

        internal int StartIndex => _start.Index;

        internal int EndIndex => _end.Index;

        internal int Length => _end.Index - _start.Index;

        public override UITextPosition Start => _start;

        public override UITextPosition End => _end;

        public override bool IsEmpty => _start.Index == _end.Index;
    }

    internal sealed class IxenSelectionRect : UITextSelectionRect
    {
        private readonly CGRect _rect;
        private readonly bool _containsStart;
        private readonly bool _containsEnd;

        internal IxenSelectionRect(CGRect rect, bool containsStart, bool containsEnd)
        {
            _rect = rect;
            _containsStart = containsStart;
            _containsEnd = containsEnd;
        }

        public override CGRect Rect => _rect;

        public override NSWritingDirection WritingDirection => NSWritingDirection.LeftToRight;

        public override bool ContainsStart => _containsStart;

        public override bool ContainsEnd => _containsEnd;

        public override bool IsVertical => false;
    }
}
