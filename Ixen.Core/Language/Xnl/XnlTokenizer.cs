using Ixen.Core.Language.Base;
using Ixen.Core.Language.Xns;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Ixen.Core.Language.Xnl
{
    internal class XnlTokenizer : BaseTokenizer
    {
        private List<XnlToken> _tokens;

        private bool _expectElementName = false;

        private bool _expectElementType = false;
        private bool _expectElementTypeEqual = false;

        private bool _expectContentBegin = false;
        private bool _expectContentEnd = false;

        private bool _expectParamsBegin = false;
        private bool _expectParamsEnd = false;
        private bool _expectParamName = false;
        private bool _expectParamEqual = false;
        private bool _expectParamValueBegin = false;
        private bool _expectParamValue = false;

        private bool _identifier;
        private bool _content;

        public XnlTokenizer(string source)
            : base(source)
        { }

        public XnlTokenizer(SourceContent source)
            : base(source)
        { }

        public IEnumerable<XnlToken> GetTokens() => _tokens;
        public IEnumerable<XnlToken> GetTokens(int indexFrom, int indexTo) => _tokens.Where(t => t.Index >= indexFrom && t.Index <= indexTo);

        public List<XnlToken> Tokenize()
        {
            _tokens = new();

            ResetPosition();
            ResetStatesFlags(XnlTokenType.None);
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

        private void AddToken(XnlTokenType type, string content)
            => _tokens.Add(new XnlToken
            {
                Index = _index - content.Length + 1,
                Content = content,
                Type = type,
                ErrorType = XnlTokenErrorType.None
            });

        private void AddErrorToken(XnlTokenErrorType type, string content, string message = null)
            => _tokens.Add(new XnlToken
            {
                Index = _index - content.Length + 1,
                Content = content,
                Message = message,
                Type = XnlTokenType.Error,
                ErrorType = type
            });

        private void ReadTokens()
        {
            char c;
            //char c2;
            var sb = new StringBuilder();

            _errorOccured = false;

            while ((c = PeekChar()) != '\0' && !_errorOccured)
            {
                if (_identifier)
                {
                    if (char.IsLetterOrDigit(c) || c == '_')
                    {
                        sb.Append(c);
                        MoveCursor();
                        continue;
                    }
                    else
                    {
                        if (_expectElementName)
                        {
                            AddToken(XnlTokenType.ElementName, sb.ToString());
                            sb.Clear();
                            ResetStatesFlags(XnlTokenType.ElementName);
                            continue;
                        }

                        if (_expectElementType)
                        {
                            AddToken(XnlTokenType.ElementTypeName, sb.ToString());
                            sb.Clear();
                            ResetStatesFlags(XnlTokenType.ElementTypeName);
                            continue;
                        }

                        if (_expectParamName)
                        {
                            AddToken(XnlTokenType.ParamName, sb.ToString());
                            sb.Clear();
                            ResetStatesFlags(XnlTokenType.ParamName);
                            continue;
                        }
                    }
                }

                if (_content)
                {
                    if (c != '"')
                    {
                        sb.Append(c);
                        MoveCursor();
                        continue;
                    }
                    else
                    {
                        if (_expectParamValue)
                        {
                            AddToken(XnlTokenType.ParamValue, sb.ToString());
                            sb.Clear();
                            ResetStatesFlags(XnlTokenType.ParamValue);
                            MoveCursor();
                            continue;
                        }
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
                        // TODO : error for non onelined contents
                        MoveCursor();
                        break;

                    case ':':
                        if (!_expectElementTypeEqual)
                        {
                            AddErrorToken(XnlTokenErrorType.UnexpectedChar, ":");
                            MoveCursor();
                            break;
                        }

                        AddToken(XnlTokenType.ElementTypeEquals, ":");
                        ResetStatesFlags(XnlTokenType.ElementTypeEquals);
                        MoveCursor();
                        break;

                    case '=':
                        if (!_expectParamEqual)
                        {
                            AddErrorToken(XnlTokenErrorType.UnexpectedChar, "=");
                            MoveCursor();
                            break;
                        }

                        AddToken(XnlTokenType.ParamEquals, "=");
                        ResetStatesFlags(XnlTokenType.ParamEquals);
                        MoveCursor();
                        break;

                    case '(':
                        if (!_expectParamsBegin)
                        {
                            AddErrorToken(XnlTokenErrorType.UnexpectedChar, "(");
                            MoveCursor();
                            break;
                        }

                        AddToken(XnlTokenType.BeginParams, "(");
                        ResetStatesFlags(XnlTokenType.BeginParams);
                        MoveCursor();
                        break;

                    case ')':
                        if (!_expectParamsEnd)
                        {
                            AddErrorToken(XnlTokenErrorType.UnexpectedChar, ")");
                            MoveCursor();
                            break;
                        }

                        AddToken(XnlTokenType.EndParams, ")");
                        ResetStatesFlags(XnlTokenType.EndParams);
                        MoveCursor();
                        break;

                    case '{':
                        if (!_expectContentBegin)
                        {
                            AddErrorToken(XnlTokenErrorType.UnexpectedChar, "{");
                            MoveCursor();
                            break;
                        }

                        AddToken(XnlTokenType.BeginContent, "{");
                        ResetStatesFlags(XnlTokenType.BeginContent);
                        MoveCursor();
                        break;

                    case '}':
                        if (!_expectContentEnd)
                        {
                            AddErrorToken(XnlTokenErrorType.UnexpectedChar, "}");
                            MoveCursor();
                            break;
                        }

                        AddToken(XnlTokenType.EndContent, "}");
                        ResetStatesFlags(XnlTokenType.EndContent);
                        MoveCursor();
                        break;

                    case '"':
                        if (!_expectParamValueBegin)
                        {
                            AddErrorToken(XnlTokenErrorType.UnexpectedChar, "\"");
                            MoveCursor();
                            break;
                        }


                        ResetStatesFlags(XnlTokenType.ParamValueBegin);
                        MoveCursor();
                        break;

                    default:
                        if (_expectElementName || _expectElementType || _expectParamName)
                        {
                            if (_identifier)
                            {
                                GenerateError(sb);
                                break;
                            }

                            _identifier = true;
                            continue;
                        }

                        if (_expectParamValue)
                        {
                            if (_content)
                            {
                                GenerateError(sb);
                                break;
                            }

                            _content = true;
                            continue;
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

        private void ResetStatesFlags(XnlTokenType lastType)
        {
            _expectElementName = false;
            _expectElementType = false;
            _expectElementTypeEqual = false;
            _expectContentBegin = false;
            _expectContentEnd = false;
            _expectParamsBegin = false;
            _expectParamsEnd = false;
            _expectParamName = false;
            _expectParamEqual = false;
            _expectParamValueBegin = false;
            _expectParamValue = false;

            _identifier = false;
            _content = false;

            switch (lastType)
            {
                case XnlTokenType.None:
                    _expectElementName = true;
                    break;

                case XnlTokenType.ElementName:
                    _expectElementTypeEqual = true;
                    break;

                case XnlTokenType.ElementTypeEquals:
                    _expectElementType = true;
                    _expectContentBegin = true;
                    break;

                case XnlTokenType.ElementTypeName:
                    _expectParamsBegin = true;
                    _expectContentBegin = true;
                    break;

                case XnlTokenType.BeginParams:
                    _expectParamName = true;
                    break;

                case XnlTokenType.EndParams:
                    _expectElementName = true;
                    _expectContentBegin = true;
                    _expectContentEnd = true;
                    break;

                case XnlTokenType.ParamName:
                    _expectParamEqual = true;
                    break;

                case XnlTokenType.ParamEquals:
                    _expectParamValueBegin = true;
                    break;

                case XnlTokenType.ParamValueBegin:
                    _expectParamValue = true;
                    break;

                case XnlTokenType.ParamValue:
                    _expectParamName = true;
                    _expectParamsEnd = true;
                    break;

                case XnlTokenType.BeginContent:
                    _expectElementName = true;
                    _expectContentEnd = true;
                    break;

                case XnlTokenType.EndContent:
                    _expectElementName = true;
                    _expectContentEnd = true;
                    break;
            }
        }
    }
}
