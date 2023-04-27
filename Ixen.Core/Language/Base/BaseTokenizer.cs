using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Ixen.Core.Language.Base
{
    internal abstract class BaseTokenizer
    {
        protected SourceContent _source;

        protected int _index = -1;
        protected int _peekIndex = -1;
        protected bool _errorOccured = false;

        public bool HasErrors { get; protected set; }

        protected BaseTokenizer(string source)
        {
            _source = new SourceContent(source);
        }

        protected BaseTokenizer(SourceContent source)
        {
            _source = source;
        }

        protected void ResetPosition()
        {
            _index = -1;
        }

        protected char PeekChar()
        {
            _peekIndex = _index;

            if (++_peekIndex >= _source.Content.Length)
            {
                return '\0';
            }

            return _source.Content[_peekIndex];
        }

        protected char PeekNonSpaceChar()
        {
            _peekIndex = _index;
            char c;

            do
            {
                if (++_peekIndex >= _source.Content.Length)
                {
                    return '\0';
                }

                c = _source.Content[_peekIndex];
            } while (char.IsWhiteSpace(c));

            return c;
        }

        protected void MoveCursor()
        {
            _index = _peekIndex;
        }
    }

    internal abstract class BaseTokenizer<TToken, TTokenType, TTokenErrorType> : BaseTokenizer
        where TToken : BaseToken<TTokenType, TTokenErrorType>, new()
        where TTokenType : struct, System.Enum
        where TTokenErrorType : struct, System.Enum
    {
        protected List<TToken> _tokens;

        public BaseTokenizer(string source)
            : base(source)
        { }

        public BaseTokenizer(SourceContent source)
            : base(source)
        { }

        public IEnumerable<TToken> GetTokens() => _tokens;
        public IEnumerable<TToken> GetTokens(int indexFrom, int indexTo) => _tokens.Where
        (
            t => (t.Index >= indexFrom || t.Index + t.Content.Length >= indexFrom)
               && t.Index <= indexTo
        );

        public abstract List<TToken> Tokenize();

        protected void AddToken(int index, TTokenType type, string content)
            => _tokens.Add(new TToken
            {
                Index = index,
                Content = content,
                Type = type
            });

        protected void AddErrorToken(int index, TTokenErrorType type, string content, string message = null)
            => _tokens.Add(new TToken
            {
                Index = index,
                Content = content,
                Message = message,
                ErrorType = type
            });

        protected bool ReadCharToken(TTokenType type, char expectedChar)
        {
            int index = _index;
            char c = PeekNonSpaceChar();

            if (c == expectedChar)
            {
                AddToken(_peekIndex, type, expectedChar.ToString());
                MoveCursor();
                return true;
            }

            _index = index;
            return false;
        }

        protected abstract TTokenType GetCommentType();

        protected bool ReadComment()
        {
            int index = _index;
            char c = PeekNonSpaceChar();

            if (c == '/')
            {
                int tokenIndex = _peekIndex;
                var sb = new StringBuilder();
                sb.Append(c);
                MoveCursor();

                c = PeekChar();
                if ((c == '/' && ReadLineComment(tokenIndex, sb))
                 || (c == '*' && ReadMultiLinesComment(tokenIndex, sb)))
                {
                    return true;
                }
            }

            _index = index;
            return false;
        }

        private bool ReadLineComment(int tokenIndex, StringBuilder sb)
        {
            char c = PeekChar();
            sb.Append(c);
            MoveCursor();

            while (true)
            {
                c = PeekChar();
                if (c == '\0' || c == '\r' || c == '\n')
                {
                    break;
                }

                sb.Append(c);
                MoveCursor();
                continue;
            }

            if (sb.Length >= 2)
            {
                AddToken(tokenIndex, GetCommentType(), sb.ToString());
                return true;
            }

            return false;
        }

        private bool ReadMultiLinesComment(int tokenIndex, StringBuilder sb)
        {
            char c = PeekChar();
            sb.Append(c);
            MoveCursor();

            while (true)
            {
                c = PeekChar();

                if (c == '\0')
                {
                    break;
                }

                if (c != '*')
                {
                    sb.Append(c);
                    MoveCursor();
                    continue;
                }
                else
                {
                    sb.Append(c);
                    MoveCursor();
                    c = PeekChar();

                    if (c != '/')
                    {
                        continue;
                    }

                    sb.Append(c);
                    MoveCursor();
                }

                break;
            }

            if (sb.Length >= 4)
            {
                AddToken(tokenIndex, GetCommentType(), sb.ToString());
                return true;
            }

            return false;
        }
    }
}
