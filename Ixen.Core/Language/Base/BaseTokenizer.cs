using System.Collections.Generic;

namespace Ixen.Core.Language.Base
{
    internal abstract class BaseTokenizer
    {
        protected List<string> _inputLines;

        protected int _lineNum = 0;
        protected int _nextLineNum = 0;
        protected int _lineIndex = -1;
        protected int _nextLineIndex = 0;

        protected bool _isNewLine = false;
        protected bool _errorOccured = false;

        public bool HasErrors { get; protected set; }

        protected BaseTokenizer(List<string> lines)
        {
            _inputLines = lines;
        }

        public abstract void UpdateLine(int lineNum);

        protected char PeekChar()
        {
            _nextLineNum = _lineNum;
            _nextLineIndex = _lineIndex + 1;
            _isNewLine = false;

            while (_nextLineIndex >= _inputLines[_nextLineNum].Length)
            {
                _nextLineIndex = 0;
                _nextLineNum++;
                _isNewLine = true;

                if (_nextLineNum >= _inputLines.Count)
                {
                    return '\0';
                }
            }

            return _inputLines[_nextLineNum][_nextLineIndex];
        }

        protected char PeekNonSpaceChar()
        {
            int nextLineNum = _lineNum;
            int nextLineIndex = _lineIndex;
            char c = '\0';

            do
            {
                while (++nextLineIndex >= _inputLines[nextLineNum].Length)
                {
                    nextLineIndex = 0;
                    nextLineNum++;

                    if (nextLineNum >= _inputLines.Count)
                    {
                        return '\0';
                    }
                }

                c = _inputLines[nextLineNum][nextLineIndex];
            } while (c == ' ' || c == '\t');

            return c;
        }

        protected void MoveCursor()
        {
            _lineNum = _nextLineNum;
            _lineIndex = _nextLineIndex;
        }
    }
}
