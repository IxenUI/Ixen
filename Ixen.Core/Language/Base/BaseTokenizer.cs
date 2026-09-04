using System.Collections.Generic;
using System.Linq;

namespace Ixen.Core.Language.Base
{
    internal abstract class BaseTokenizer
    {
        private const int CHARS_PER_TOKEN = 8;
        private const int CHAR_TEXTS = 128;

        private static readonly string[] _charTexts = new string[CHAR_TEXTS];

        protected SourceContent _source;

        protected int _index = -1;
        protected int _peekIndex = -1;
        protected List<LanguageError> _diagnostics = new();

        public IReadOnlyList<LanguageError> Diagnostics => _diagnostics;
        public bool HasErrors => _diagnostics.Any(d => d.Severity == LanguageErrorSeverity.Error);

        protected void AddError(string code, string message, int index, int length)
            => _diagnostics.Add(new LanguageError(code, message, index, length));

        protected void ReportUnexpectedCharacter()
        {
            char c = PeekNonSpaceChar();

            if (c == '\0')
            {
                return;
            }

            AddError(LanguageErrorCode.SYNTAX, $"Unexpected character '{c}'.", _peekIndex, 1);
        }

        protected void ReportUnclosedBlock()
            => AddError(LanguageErrorCode.SYNTAX, "Unexpected end of file: a block is not closed.",
                _source.Content.Length, 0);

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

        protected int EstimatedTokenCount()
        {
            return (_source.Content.Length / CHARS_PER_TOKEN) + 1;
        }

        protected string Slice(int start, int end)
        {
            return _source.Content.Substring(start, end - start + 1);
        }

        protected bool Matches(int start, int end, string keyword)
        {
            if (end - start + 1 != keyword.Length)
            {
                return false;
            }

            string content = _source.Content;

            for (int i = 0; i < keyword.Length; i++)
            {
                if (content[start + i] != keyword[i])
                {
                    return false;
                }
            }

            return true;
        }

        protected int TrimmedStart(int start, int end)
        {
            string content = _source.Content;

            while (start <= end && char.IsWhiteSpace(content[start]))
            {
                start++;
            }

            return start;
        }

        protected int TrimmedEnd(int start, int end)
        {
            string content = _source.Content;

            while (end >= start && char.IsWhiteSpace(content[end]))
            {
                end--;
            }

            return end;
        }

        protected string Trimmed(int start, int end)
        {
            int from = TrimmedStart(start, end);
            int to = TrimmedEnd(start, end);

            return to < from ? string.Empty : Slice(from, to);
        }

        protected static string TextOf(char c)
        {
            if (c >= CHAR_TEXTS)
            {
                return c.ToString();
            }

            return _charTexts[c] ?? (_charTexts[c] = c.ToString());
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
            t => (t.Index >= indexFrom || t.Index + t.Length >= indexFrom)
               && t.Index <= indexTo
        );

        public abstract List<TToken> Tokenize();

        protected void AddToken(int index, TTokenType type, string content)
            => AddToken(index, type, content, content?.Length ?? 0);

        protected void AddToken(int index, TTokenType type, string content, int length)
            => _tokens.Add(new TToken
            {
                Index = index,
                Content = content,
                Length = length,
                Type = type
            });

        protected void AddErrorToken(int index, TTokenErrorType type, string content, string message = null)
            => _tokens.Add(new TToken
            {
                Index = index,
                Content = content,
                Length = content?.Length ?? 0,
                Message = message,
                ErrorType = type
            });

        protected bool ReadCharToken(TTokenType type, char expectedChar)
        {
            int index = _index;
            char c = PeekNonSpaceChar();

            if (c == expectedChar)
            {
                AddToken(_peekIndex, type, TextOf(expectedChar));
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
                MoveCursor();

                c = PeekChar();
                if ((c == '/' && ReadLineComment(tokenIndex))
                 || (c == '*' && ReadMultiLinesComment(tokenIndex)))
                {
                    return true;
                }
            }

            _index = index;
            return false;
        }

        private bool ReadLineComment(int tokenIndex)
        {
            MoveCursor();

            while (true)
            {
                char c = PeekChar();
                if (c == '\0' || c == '\r' || c == '\n')
                {
                    break;
                }

                MoveCursor();
                continue;
            }

            if (_index - tokenIndex + 1 >= 2)
            {
                AddToken(tokenIndex, GetCommentType(), Slice(tokenIndex, _index));
                return true;
            }

            return false;
        }

        private bool ReadMultiLinesComment(int tokenIndex)
        {
            MoveCursor();

            while (true)
            {
                char c = PeekChar();

                if (c == '\0')
                {
                    break;
                }

                if (c != '*')
                {
                    MoveCursor();
                    continue;
                }
                else
                {
                    MoveCursor();
                    c = PeekChar();

                    if (c != '/')
                    {
                        continue;
                    }

                    MoveCursor();
                }

                break;
            }

            if (_index - tokenIndex + 1 >= 4)
            {
                AddToken(tokenIndex, GetCommentType(), Slice(tokenIndex, _index));
                return true;
            }

            return false;
        }
    }
}
