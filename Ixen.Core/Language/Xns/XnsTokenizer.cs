using Ixen.Core.Language.Base;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Ixen.Core.Language.Xns
{
    internal class XnsTokenizer : BaseTokenizer
    {
        private List<XnsToken> _tokens;

        private bool _exceptClassName = false;

        private bool _expectContentBegin = false;
        private bool _expectContentEnd = false;

        private bool _expectStyleName = false;
        private bool _expectStyleEqual = false;
        private bool _expectStyleValue = false;

        private bool _identifier;
        private bool _content;

        public XnsTokenizer(string source)
            : base(source)
        { }

        public XnsTokenizer(SourceContent source)
            : base(source)
        { }

        public IEnumerable<XnsToken> GetTokens() => _tokens;
        public IEnumerable<XnsToken> GetTokens(int indexFrom, int indexTo) => _tokens.Where(t => t.Index >= indexFrom && t.Index <= indexTo);

        public List<XnsToken> Tokenize()
        {
            _tokens = new();

            ResetPosition();
            ResetStatesFlags(XnsTokenType.None);
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

        private void AddToken(XnsTokenType type, string content, bool previewChar)
            => _tokens.Add(new XnsToken
            {
                Index = _index - content.Length + 1,
                Content = content,
                Type = type,
                ErrorType = XnsTokenErrorType.None
            });

        private void AddErrorToken(XnsTokenErrorType type, string content, bool previewChar, string message = null)
            => _tokens.Add(new XnsToken
            {
                Index = _index - content.Length + 1,
                Content = content,
                Message = message,
                Type = XnsTokenType.Error,
                ErrorType = type
            });

        private void ReadTokens()
        {
            char c;
            char c2;
            var sb = new StringBuilder();

            _errorOccured = false;

            while ((c = PeekChar()) != '\0' && !_errorOccured)
            {
                if (_identifier)
                {
                    if (char.IsLetterOrDigit(c) || _exceptClassName && (c == '_' || c == '.' || c == '#'))
                    {
                        sb.Append(c);
                        MoveCursor();
                        continue;
                    }
                    else
                    {
                        c2 = PeekNonSpaceChar();
                        if (_exceptClassName && c2 == '{')
                        {
                            AddToken(XnsTokenType.ClassName, sb.ToString(), false);
                            sb.Clear();
                            ResetStatesFlags(XnsTokenType.ClassName);
                            continue;
                        }

                        if (_expectStyleName && c2 == ':')
                        {
                            AddToken(XnsTokenType.StyleName, sb.ToString(), false);
                            sb.Clear();
                            ResetStatesFlags(XnsTokenType.StyleName);
                            continue;
                        }

                        GenerateError(sb);
                    }
                }

                if (_content)
                {
                    if (c != '\r' && c != '\n')
                    {
                        sb.Append(c);
                        MoveCursor();
                        continue;
                    }
                    else
                    {
                        if (_expectStyleValue)
                        {
                            AddToken(XnsTokenType.StyleValue, sb.ToString(), false);
                            ResetStatesFlags(XnsTokenType.StyleValue);
                            sb.Clear();

                            continue;
                        }

                        GenerateError(sb);
                    }
                }

                switch (c)
                {
                    case ' ':
                    case '\t':
                        MoveCursor();
                        break;

                    case '\r':
                    case '\n':
                        if (_expectStyleEqual || _expectStyleValue)
                        {
                            GenerateError(sb);
                            break;
                        }
                        MoveCursor();
                        break;

                    case ':':
                        if (!_expectStyleEqual)
                        {
                            AddErrorToken(XnsTokenErrorType.UnexpectedChar, ":", true);
                            MoveCursor();
                            break;
                        }

                        AddToken(XnsTokenType.StyleEquals, ":", true);
                        ResetStatesFlags(XnsTokenType.StyleEquals);
                        MoveCursor();
                        break;

                    case '{':
                        if (!_expectContentBegin)
                        {
                            AddErrorToken(XnsTokenErrorType.UnexpectedChar, "{", true);
                            MoveCursor();
                            break;
                        }

                        AddToken(XnsTokenType.BeginClassContent, "{", true);
                        ResetStatesFlags(XnsTokenType.BeginClassContent);
                        MoveCursor();
                        break;

                    case '}':
                        if (!_expectContentEnd)
                        {
                            AddErrorToken(XnsTokenErrorType.UnexpectedChar, "}", true);
                            MoveCursor();
                            break;
                        }

                        AddToken(XnsTokenType.EndClassContent, "}", true);
                        ResetStatesFlags(XnsTokenType.EndClassContent);
                        MoveCursor();
                        break;

                    default:
                        if (_exceptClassName || _expectStyleName)
                        {
                            if (_identifier)
                            {
                                GenerateError(sb);
                                break;
                            }

                            _identifier = true;
                            break;
                        }

                        if (_expectStyleValue)
                        {
                            if (_content)
                            {
                                GenerateError(sb);
                                break;
                            }

                            _content = true;
                            break;
                        }

                        GenerateError(sb);
                        break;
                }
            }
        }

        private void GenerateError(StringBuilder sb)
        {
            _errorOccured = true;
        }

        private void ResetStatesFlags(XnsTokenType lastType)
        {
            _exceptClassName = false;
            _expectContentBegin = false;
            _expectContentEnd = false;
            _expectStyleName = false;
            _expectStyleEqual = false;
            _expectStyleValue = false;

            _identifier = false;
            _content = false;

            switch (lastType)   
            {
                case XnsTokenType.None:
                    _exceptClassName = true;
                    break;

                case XnsTokenType.ClassName:
                    _expectContentBegin = true;
                    break;

                case XnsTokenType.BeginClassContent:
                    _expectStyleName = true;
                    _expectContentEnd = true;
                    break;

                case XnsTokenType.EndClassContent:
                    _exceptClassName = true;
                    _expectContentEnd = true;
                    break;

                case XnsTokenType.StyleName:
                    _expectStyleEqual = true;
                    break;

                case XnsTokenType.StyleEquals:
                    _expectStyleValue = true;
                    break;

                case XnsTokenType.StyleValue:
                    _expectStyleName = true;
                    _exceptClassName = true;
                    _expectContentEnd = true;
                    break;
            }
        }
    }
}
