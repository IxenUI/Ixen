namespace Ixen.Core.Language.Base
{
    internal abstract class BaseTokenizer
    {
        protected string _source;

        protected int _index = -1;
        protected int _nextIndex = 0;

        protected bool _errorOccured = false;

        public bool HasErrors { get; protected set; }

        protected BaseTokenizer(string source)
        {
            _source = source;
        }

        protected BaseTokenizer(ref string source)
        {
            _source = source;
        }

        protected char PeekChar()
        {
            _nextIndex = _index + 1;

            if (_nextIndex >= _source.Length)
            {
                return '\0';
            }

            return _source[_nextIndex];
        }

        protected char PeekNonSpaceChar()
        {
            int nextIndex = _index;
            char c = '\0';

            do
            {
                if (++nextIndex >= _source.Length)
                {
                    return '\0';
                }

                c = _source[nextIndex];
            } while (char.IsWhiteSpace(c));

            return c;
        }

        protected void MoveCursor()
        {
            _index = _nextIndex;
        }
    }
}
