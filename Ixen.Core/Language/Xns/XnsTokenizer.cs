using Ixen.Core.Language.Base;
using System;
using System.Collections.Generic;
using System.Text;

namespace Ixen.Core.Language.Xns
{
    internal class XnsTokenizer : BaseTokenizer
    {
        private Dictionary<int, List<XnsToken>> _tokensByLines;

        private bool _expectElementName = false;

        private bool _expectContentBegin = false;
        private bool _expectContentEnd = false;

        private bool _expectStyleName = false;
        private bool _expectStyleEqual = false;
        private bool _expectStyleValue = false;

        private bool _identifier;
        private bool _content;

        public XnsTokenizer(List<string> lines)
            : base(lines)
        { }

        public List<XnsToken> Tokenize()
        {
            _tokensByLines = new();

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

            return GetTokens();
        }

        private void AddToken(XnsToken token)
        {
            if (!_tokensByLines.ContainsKey(_lineNum))
            {
                _tokensByLines.Add(_lineNum, new());
            }

            _tokensByLines[_lineNum].Add(token);
        }

        private void AddToken(XnsTokenType type, string content)
            => AddToken(new XnsToken
            {
                LineNum = _lineNum,
                LineIndex = _lineIndex - content.Length + 1,
                Content = content,
                Type = type,
                ErrorType = XnsTokenErrorType.None
            });

        private void AddErrorToken(XnsTokenErrorType type, string content, string message = null)
            => AddToken(new XnsToken
            {
                LineNum = _lineNum,
                LineIndex = _lineIndex - content.Length + 1,
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
                    if (char.IsLetterOrDigit(c) || _expectElementName && (c == '_' || c == '.'))
                    {
                        sb.Append(c);
                        MoveCursor();
                        continue;
                    }
                    else
                    {
                        c2 = PeekNonSpaceChar();
                        if (_expectElementName && c2 == '{')
                        {
                            AddToken(XnsTokenType.ClassIdentifier, sb.ToString());
                            sb.Clear();
                            ResetStatesFlags(XnsTokenType.ClassIdentifier);
                            continue;
                        }

                        if (_expectStyleName && c2 == ':')
                        {
                            AddToken(XnsTokenType.StyleName, sb.ToString());
                            sb.Clear();
                            ResetStatesFlags(XnsTokenType.StyleName);
                            continue;
                        }

                        GenerateError(sb);
                    }
                }

                if (_content)
                {
                    if (!_isNewLine)
                    {
                        sb.Append(c);
                        MoveCursor();
                        continue;
                    }
                    else
                    {
                        if (_expectStyleValue)
                        {
                            AddToken(XnsTokenType.StyleValue, sb.ToString());
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

                    case ':':
                        if (!_expectStyleEqual)
                        {
                            AddErrorToken(XnsTokenErrorType.UnexpectedChar, ":");
                            MoveCursor();
                            break;
                        }

                        AddToken(XnsTokenType.StyleEquals, ":");
                        ResetStatesFlags(XnsTokenType.StyleEquals);
                        MoveCursor();
                        break;

                    case '{':
                        if (!_expectContentBegin)
                        {
                            AddErrorToken(XnsTokenErrorType.UnexpectedChar, "{");
                            MoveCursor();
                            break;
                        }

                        AddToken(XnsTokenType.BeginClassContent, "{");
                        ResetStatesFlags(XnsTokenType.BeginClassContent);
                        MoveCursor();
                        break;

                    case '}':
                        if (!_expectContentEnd)
                        {
                            AddErrorToken(XnsTokenErrorType.UnexpectedChar, "}");
                            MoveCursor();
                            break;
                        }

                        AddToken(XnsTokenType.EndClassContent, "}");
                        ResetStatesFlags(XnsTokenType.EndClassContent);
                        MoveCursor();
                        break;

                    default:
                        if (_expectElementName || _expectStyleName)
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
            _expectElementName = false;

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
                    _expectElementName = true;
                    break;

                case XnsTokenType.ClassIdentifier:
                    _expectContentBegin = true;
                    break;

                case XnsTokenType.BeginClassContent:
                    _expectStyleName = true;
                    _expectContentEnd = true;
                    break;

                case XnsTokenType.EndClassContent:
                    _expectElementName = true;
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
                    _expectElementName = true;
                    _expectContentEnd = true;
                    break;
            }
        }

        public List<XnsToken> GetTokens()
        {
            var list = new List<XnsToken>();

            foreach(var kvp in _tokensByLines)
            {
                foreach (var token in kvp.Value)
                {
                    list.Add(token);
                }
            }

            return list;
        }

        public List<XnsToken> GetTokensOfLine(int lineNum)
        {
            if (!_tokensByLines.ContainsKey(lineNum))
            {
                return new List<XnsToken>();
            }

            return _tokensByLines[lineNum];
        }

        public override void UpdateLine(int lineNum)
        {
            if (lineNum < 0)
            {
                return;
            }
            else if (lineNum == 0)
            {
                UpdateTokensOfLine(lineNum, null);
                return;
            }

            int checkLine = lineNum - 1;
            List<XnsToken> previousTokens;
            XnsToken lastToken = null;
            while (true)
            {
                previousTokens = GetTokensOfLine(checkLine);

                if (previousTokens.Count > 0)
                {
                    lastToken = previousTokens[previousTokens.Count - 1];
                    break;
                }

                if (--checkLine <= 0)
                {
                    break;
                }
            }

            UpdateTokensOfLine(lineNum, lastToken);
        }

        private void UpdateTokensOfLine(int lineNum, XnsToken lastToken)
        {
            if (lastToken == null)
            {
                ResetStatesFlags(XnsTokenType.None);
            }
            else
            {
                ResetStatesFlags(lastToken.Type);
            }

            for (int i = lineNum; i < _inputLines.Count; i++)
            {
                if (_tokensByLines.ContainsKey(i))
                {
                    _tokensByLines[i].Clear();
                }
            }

            _lineNum = lineNum;
            _lineIndex = -1;
            _isNewLine = false;

            ReadTokens();
        }
    }
}
