using Ixen.Core.Language.Base;
using System;
using System.Collections.Generic;
using System.Text;

namespace Ixen.Core.Language.Xns
{
    internal class XnsTokenizer : BaseTokenizer<XnsToken, XnsTokenType, XnsTokenErrorType>
    {
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

            ResetPosition();
            SetStatesFlags(XnsTokenType.None);
            HasErrors = false;

            try
            {
                ReadTokens();
            }
            catch (Exception)
            {
                HasErrors = true;
            }

            return _tokens;
        }

        private void ReadTokens()
        {
            _errorOccured = false;

            while (PeekChar() != '\0')
            {
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

        private bool ReadClassName()
        {
            int index = _index;
            char c = PeekNonSpaceChar();

            if (char.IsLetter(c) || c == '.' || c == '#' || c == '_')
            {
                int tokenIndex = _peekIndex;
                var sb = new StringBuilder();
                sb.Append(c);
                MoveCursor();

                while (true)
                {
                    c = PeekChar();
                    if (char.IsLetterOrDigit(c) || c == '_' ||  c == '-')
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

            if (char.IsLetterOrDigit(c) || c == '#' || c == '?')
            {
                int tokenIndex = _peekIndex;
                var sb = new StringBuilder();
                sb.Append(c);
                MoveCursor();

                while (true)
                {
                    c = PeekChar();
                    if (char.IsLetterOrDigit(c) || c == '%' || c == '*' || c == ' ' || c == '\t')
                    {
                        sb.Append(c);
                        MoveCursor();
                        continue;
                    }

                    break;
                }

                if (sb.Length >= 1)
                {
                    AddToken(tokenIndex, XnsTokenType.StyleValue, sb.ToString());
                    return true;
                }
            }

            _index = index;
            return false;
        }

        protected override XnsTokenType GetCommentType() => XnsTokenType.Comment;
        private bool ReadStyleEquals() => ReadCharToken(XnsTokenType.StyleEquals, ':');
        private bool ReadContentBegin() => ReadCharToken(XnsTokenType.BeginClassContent, '{');
        private bool ReadContentEnd() => ReadCharToken(XnsTokenType.EndClassContent, '}');
    }
}
