using System.Collections.Generic;

namespace Ixen.LanguageServer.Server
{
    internal sealed class LineIndex
    {
        private readonly string _text;
        private readonly int[] _starts;

        public LineIndex(string text)
        {
            _text = text ?? string.Empty;

            List<int> starts = new List<int>();
            starts.Add(0);

            for (int index = 0; index < _text.Length; index++)
            {
                if (_text[index] == '\n')
                {
                    starts.Add(index + 1);
                }
            }

            _starts = starts.ToArray();
        }

        public int Count => _starts.Length;

        public int Length => _text.Length;

        public int StartOf(int line)
        {
            if (line < 0)
            {
                return 0;
            }

            return line >= _starts.Length ? _text.Length : _starts[line];
        }

        public int EndOf(int line)
        {
            if (line < 0 || line >= _starts.Length)
            {
                return _text.Length;
            }

            if (line + 1 >= _starts.Length)
            {
                return _text.Length;
            }

            int end = _starts[line + 1] - 1;

            if (end > _starts[line] && _text[end - 1] == '\r')
            {
                end--;
            }

            return end;
        }

        public int LineOf(int offset)
        {
            if (offset <= 0)
            {
                return 0;
            }

            if (offset >= _text.Length)
            {
                return _starts.Length - 1;
            }

            int low = 0;
            int high = _starts.Length - 1;

            while (low < high)
            {
                int middle = low + ((high - low + 1) / 2);

                if (_starts[middle] <= offset)
                {
                    low = middle;
                }
                else
                {
                    high = middle - 1;
                }
            }

            return low;
        }

        public void PositionOf(int offset, out int line, out int character)
        {
            if (offset < 0)
            {
                offset = 0;
            }

            if (offset > _text.Length)
            {
                offset = _text.Length;
            }

            line = LineOf(offset);
            character = offset - _starts[line];
        }

        public int OffsetOf(int line, int character)
        {
            if (line < 0)
            {
                return 0;
            }

            if (line >= _starts.Length)
            {
                return _text.Length;
            }

            int start = _starts[line];
            int end = EndOf(line);
            int offset = start + (character < 0 ? 0 : character);

            return offset > end ? end : offset;
        }
    }
}
