using Ixen.Core.Language.Base;
using System;
using System.Collections.Generic;
using System.Text;

namespace Ixen.Core.Language.Xns
{
    internal class XnsTokenizer : BaseTokenizer<XnsToken, XnsTokenType, XnsTokenErrorType>
    {
        internal const char KEYFRAMES_MARKER = '@';
        internal const string KEYFRAMES_KEYWORD = "keyframes";
        internal const string MEDIA_KEYWORD = "media";

        private bool _expectClassName = false;

        private bool _expectContentBegin = false;
        private bool _expectContentEnd = false;

        private bool _expectStyleName = false;
        private bool _expectStyleEquals = false;
        private bool _expectStyleValue = false;

        private int _contentLevel;

        public XnsTokenizer(string source)
            : base(source)
        { }

        public XnsTokenizer(SourceContent source)
            : base(source)
        { }


        public override List<XnsToken> Tokenize()
        {
            _tokens = new();
            _diagnostics.Clear();

            ResetPosition();
            SetStatesFlags(XnsTokenType.None);

            try
            {
                ReadTokens();
            }
            catch (Exception ex)
            {
                AddError(LanguageErrorCode.SYNTAX, $"Tokenizer failure: {ex.Message}", _index, 0);
            }

            if (_contentLevel != 0)
            {
                ReportUnclosedBlock();
            }

            return _tokens;
        }

        private void ReadTokens()
        {

            while (PeekChar() != '\0')
            {
                if (_expectClassName && ReadMedia())
                {
                    SetStatesFlags(XnsTokenType.MediaQuery);
                    continue;
                }

                if (_expectClassName && ReadKeyframes())
                {
                    SetStatesFlags(XnsTokenType.ClassName);
                    continue;
                }

                if (_expectClassName && ReadClassName())
                {
                    SetStatesFlags(XnsTokenType.ClassName);
                    continue;
                }

                if (_expectContentBegin && ReadContentBegin())
                {
                    SetStatesFlags(XnsTokenType.BeginClassContent);
                    continue;
                }

                if (_expectContentEnd && ReadContentEnd())
                {
                    SetStatesFlags(XnsTokenType.EndClassContent);
                    continue;
                }

                if (_expectStyleName && ReadStyleName())
                {
                    SetStatesFlags(XnsTokenType.StyleName);
                    continue;
                }

                if (_expectStyleEquals && ReadStyleEquals())
                {
                    SetStatesFlags(XnsTokenType.StyleEquals);
                    continue;
                }

                if (_expectStyleValue && ReadStyleValue())
                {
                    SetStatesFlags(XnsTokenType.StyleValue);
                    continue;
                }

                if (ReadComment())
                {
                    continue;
                }

                ReportUnexpectedCharacter();
                break;
            }
        }

        private void ResetStatesFlags()
        {
            _expectClassName = false;
            _expectContentBegin = false;
            _expectContentEnd = false;
            _expectStyleName = false;
            _expectStyleEquals = false;
            _expectStyleValue = false;
        }

        private void SetStatesFlags(XnsTokenType lastType)
        {
            ResetStatesFlags();

            switch (lastType)   
            {
                case XnsTokenType.None:
                    _expectClassName = true;
                    break;

                case XnsTokenType.ClassName:
                case XnsTokenType.MediaQuery:
                    _expectContentBegin = true;
                    break;

                case XnsTokenType.BeginClassContent:
                    _expectClassName = true;
                    _expectStyleName = true;
                    _expectContentEnd = true;
                    _contentLevel++;
                    break;

                case XnsTokenType.EndClassContent:
                    _contentLevel--;
                    _expectClassName = true;
                    
                    if (_contentLevel > 0)
                    {
                        _expectStyleName = true;
                        _expectContentEnd = true;
                    }
                    
                    break;

                case XnsTokenType.StyleName:
                    _expectStyleEquals = true;
                    break;

                case XnsTokenType.StyleEquals:
                    _expectStyleValue = true;
                    break;

                case XnsTokenType.StyleValue:
                    _expectStyleName = true;
                    _expectClassName = true;
                    _expectContentEnd = true;
                    break;
            }
        }

        private bool ReadMedia()
        {
            int index = _index;

            if (PeekNonSpaceChar() != KEYFRAMES_MARKER)
            {
                _index = index;
                return false;
            }

            int tokenIndex = _peekIndex;
            MoveCursor();

            var keyword = new StringBuilder();

            while (char.IsLetter(PeekChar()))
            {
                keyword.Append(PeekChar());
                MoveCursor();
            }

            if (keyword.ToString() != MEDIA_KEYWORD)
            {
                _index = index;
                return false;
            }

            var condition = new StringBuilder();

            while (true)
            {
                char c = PeekChar();

                if (c == '\0' || c == '{' || c == '}' || c == '\r' || c == '\n')
                {
                    break;
                }

                condition.Append(c);
                MoveCursor();
            }

            string text = condition.ToString().Trim();

            if (PeekChar() != '{' || text.Length == 0)
            {
                _index = index;
                return false;
            }

            AddToken(tokenIndex, XnsTokenType.MediaQuery, text, _index - tokenIndex + 1);

            return true;
        }

        private bool ReadKeyframes()
        {
            int index = _index;

            if (PeekNonSpaceChar() != KEYFRAMES_MARKER)
            {
                _index = index;
                return false;
            }

            int tokenIndex = _peekIndex;
            MoveCursor();

            var keyword = new StringBuilder();

            while (char.IsLetter(PeekChar()))
            {
                keyword.Append(PeekChar());
                MoveCursor();
            }

            if (keyword.ToString() != KEYFRAMES_KEYWORD)
            {
                _index = index;
                return false;
            }

            char c = PeekNonSpaceChar();

            if (!char.IsLetter(c) && c != '_')
            {
                _index = index;
                return false;
            }

            var name = new StringBuilder();
            name.Append(c);
            MoveCursor();

            while (true)
            {
                c = PeekChar();

                if (char.IsLetterOrDigit(c) || c == '_' || c == '-')
                {
                    name.Append(c);
                    MoveCursor();
                    continue;
                }

                break;
            }

            if (PeekNonSpaceChar() != '{')
            {
                _index = index;
                return false;
            }

            if (_contentLevel != 0)
            {
                AddError(LanguageErrorCode.SYNTAX,
                    $"A '{KEYFRAMES_MARKER}{KEYFRAMES_KEYWORD}' block must be declared at the top level.",
                    tokenIndex, 1);
            }

            AddToken(tokenIndex, XnsTokenType.ClassName, KEYFRAMES_MARKER + name.ToString(),
                _index - tokenIndex + 1);

            return true;
        }

        private bool ReadClassName()
        {
            int index = _index;
            char c = PeekNonSpaceChar();

            if (char.IsLetterOrDigit(c) || c == '.' || c == '#' || c == '_')
            {
                int tokenIndex = _peekIndex;
                var sb = new StringBuilder();
                sb.Append(c);
                MoveCursor();

                while (true)
                {
                    c = PeekChar();
                    if (char.IsLetterOrDigit(c) || c == '_' ||  c == '-' || c == ':' || c == '%')
                    {
                        sb.Append(c);
                        MoveCursor();
                        continue;
                    }

                    break;
                }

                c = PeekNonSpaceChar();

                if (c == '{' && (sb.Length >= 1 || char.IsLetter(sb[0])))
                {
                    AddToken(tokenIndex, XnsTokenType.ClassName, sb.ToString());
                    return true;
                }
            }

            _index = index;
            return false;
        }

        private bool ReadStyleName()
        {
            int index = _index;
            char c = PeekNonSpaceChar();

            if (char.IsLetter(c))
            {
                int tokenIndex = _peekIndex;
                var sb = new StringBuilder();
                sb.Append(c);
                MoveCursor();

                while (true)
                {
                    c = PeekChar();
                    if (char.IsLetter(c) || c == '-')
                    {
                        sb.Append(c);
                        MoveCursor();
                        continue;
                    }

                    break;
                }

                c = PeekNonSpaceChar();

                if (c == ':')
                {
                    AddToken(tokenIndex, XnsTokenType.StyleName, sb.ToString());
                    return true;
                }
            }

            _index = index;
            return false;
        }

        private bool ReadStyleValue()
        {
            int index = _index;
            char c = PeekNonSpaceChar();

            if (char.IsLetterOrDigit(c) || c == '#' || c == '?' || c == '_')
            {
                int tokenIndex = _peekIndex;
                var sb = new StringBuilder();
                sb.Append(c);
                MoveCursor();

                while (true)
                {
                    c = PeekChar();

                    if ((c == ' ' || c == '\t') && StartsStyleName(_peekIndex))
                    {
                        break;
                    }

                    if (c == '/' && StartsComment(_peekIndex))
                    {
                        break;
                    }

                    if (char.IsLetterOrDigit(c) || c == '%' || c == '*' || c == '.' || c == '#' || c == '?'
                        || c == '-' || c == '_' || c == '/' || c == ' ' || c == '\t')
                    {
                        sb.Append(c);
                        MoveCursor();
                        continue;
                    }

                    break;
                }

                string value = sb.ToString().TrimEnd();

                if (value.Length >= 1)
                {
                    AddToken(tokenIndex, XnsTokenType.StyleValue, value);
                    return true;
                }
            }

            _index = index;
            return false;
        }

        private bool StartsComment(int index)
        {
            string content = _source.Content;
            int next = index + 1;

            return next < content.Length && (content[next] == '/' || content[next] == '*');
        }

        private bool StartsStyleName(int index)
        {
            string content = _source.Content;
            int cursor = index;

            while (cursor < content.Length && (content[cursor] == ' ' || content[cursor] == '\t'))
            {
                cursor++;
            }

            int nameStart = cursor;

            while (cursor < content.Length && (char.IsLetter(content[cursor]) || content[cursor] == '-'))
            {
                cursor++;
            }

            if (cursor == nameStart)
            {
                return false;
            }

            while (cursor < content.Length && (content[cursor] == ' ' || content[cursor] == '\t'))
            {
                cursor++;
            }

            return cursor < content.Length && content[cursor] == ':';
        }

        protected override XnsTokenType GetCommentType() => XnsTokenType.Comment;
        private bool ReadStyleEquals() => ReadCharToken(XnsTokenType.StyleEquals, ':');
        private bool ReadContentBegin() => ReadCharToken(XnsTokenType.BeginClassContent, '{');
        private bool ReadContentEnd() => ReadCharToken(XnsTokenType.EndClassContent, '}');
    }
}
