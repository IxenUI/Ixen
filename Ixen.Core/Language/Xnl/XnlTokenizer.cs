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
        private bool _expectElementTypeBegin = false;
        private bool _expectElementTypeEnd = false;

        private bool _expectChildrenBegin = false;
        private bool _expectChildrenEnd = false;

        private bool _expectPropertiesBegin = false;
        private bool _expectPropertiesEnd = false;

        private bool _expectPropertyName = false;
        private bool _expectPropertyEqual = false;
        private bool _expectPropertyValueBegin = false;
        private bool _expectPropertyValueEnd = false;
        private bool _expectPropertyValue = false;

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

        private void AddToken(XnlTokenType type, string content, bool previewChar)
            => _tokens.Add(new XnlToken
            {
                Index = (previewChar ? _nextIndex : _index) - content.Length + 1,
                Content = content,
                Type = type,
                ErrorType = XnlTokenErrorType.None
            });

        private void AddErrorToken(XnlTokenErrorType type, string content, bool previewChar, string message = null)
            => _tokens.Add(new XnlToken
            {
                Index = (previewChar ? _nextIndex : _index) - content.Length + 1,
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
                            AddToken(XnlTokenType.ElementName, sb.ToString(), false);
                            sb.Clear();
                            ResetStatesFlags(XnlTokenType.ElementName);
                            continue;
                        }

                        if (_expectElementType)
                        {
                            AddToken(XnlTokenType.ElementTypeName, sb.ToString(), false);
                            sb.Clear();
                            ResetStatesFlags(XnlTokenType.ElementTypeName);
                            continue;
                        }

                        if (_expectPropertyName)
                        {
                            AddToken(XnlTokenType.PropertyName, sb.ToString(), false);
                            sb.Clear();
                            ResetStatesFlags(XnlTokenType.PropertyName);
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
                        if (_expectPropertyValue)
                        {
                            AddToken(XnlTokenType.PropertyValue, sb.ToString(), false);
                            sb.Clear();
                            ResetStatesFlags(XnlTokenType.PropertyValue);
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
                        if (!_expectPropertyEqual)
                        {
                            AddErrorToken(XnlTokenErrorType.UnexpectedChar, ":", true);
                            MoveCursor();
                            break;
                        }

                        AddToken(XnlTokenType.PropertyEqual, ":", true);
                        ResetStatesFlags(XnlTokenType.PropertyEqual);
                        MoveCursor();
                        break;

                    case '<':
                        if (!_expectElementTypeBegin)
                        {
                            AddErrorToken(XnlTokenErrorType.UnexpectedChar, "<", true);
                            MoveCursor();
                            break;
                        }

                        AddToken(XnlTokenType.ElementTypeBegin, "<", true);
                        ResetStatesFlags(XnlTokenType.ElementTypeBegin);
                        MoveCursor();
                        break;

                    case '>':
                        if (!_expectElementTypeEnd)
                        {
                            AddErrorToken(XnlTokenErrorType.UnexpectedChar, ">", true);
                            MoveCursor();
                            break;
                        }

                        AddToken(XnlTokenType.ElementTypeEnd, ">", true);
                        ResetStatesFlags(XnlTokenType.ElementTypeEnd);
                        MoveCursor();
                        break;

                    case '{':
                        if (!_expectPropertiesBegin)
                        {
                            AddErrorToken(XnlTokenErrorType.UnexpectedChar, "{", true);
                            MoveCursor();
                            break;
                        }

                        AddToken(XnlTokenType.PropertiesBegin, "{", true);
                        ResetStatesFlags(XnlTokenType.PropertiesBegin);
                        MoveCursor();
                        break;

                    case '}':
                        if (!_expectPropertiesEnd)
                        {
                            AddErrorToken(XnlTokenErrorType.UnexpectedChar, "}", true);
                            MoveCursor();
                            break;
                        }

                        AddToken(XnlTokenType.PropertiesEnd, "}", true);
                        ResetStatesFlags(XnlTokenType.PropertiesEnd);
                        MoveCursor();
                        break;

                    case '[':
                        if (!_expectChildrenBegin)
                        {
                            AddErrorToken(XnlTokenErrorType.UnexpectedChar, "[", true);
                            MoveCursor();
                            break;
                        }

                        AddToken(XnlTokenType.ChildrenBegin, "[", true);
                        ResetStatesFlags(XnlTokenType.ChildrenBegin);
                        MoveCursor();
                        break;

                    case ']':
                        if (!_expectChildrenEnd)
                        {
                            AddErrorToken(XnlTokenErrorType.UnexpectedChar, "]", true);
                            MoveCursor();
                            break;
                        }

                        AddToken(XnlTokenType.ChildrenEnd, "]", true);
                        ResetStatesFlags(XnlTokenType.ChildrenEnd);
                        MoveCursor();
                        break;

                    case '"':
                        if (!_expectPropertyValueBegin && !_expectPropertyValueEnd)
                        {
                            AddErrorToken(XnlTokenErrorType.UnexpectedChar, "\"", true);
                            MoveCursor();
                            break;
                        }

                        if (_expectPropertyValueBegin)
                        {
                            AddToken(XnlTokenType.PropertyValueBegin, "\"", true);
                            ResetStatesFlags(XnlTokenType.PropertyValueBegin);
                        }
                        else if (_expectPropertyValueEnd)
                        {
                            AddToken(XnlTokenType.PropertyValueEnd, "\"", true);
                            ResetStatesFlags(XnlTokenType.PropertyValueEnd);
                        }
                        
                        MoveCursor();
                        break;

                    default:
                        if (_expectElementName || _expectElementType || _expectPropertyName)
                        {
                            if (_identifier)
                            {
                                GenerateError(sb);
                                break;
                            }

                            _identifier = true;
                            continue;
                        }

                        if (_expectPropertyValue)
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
            _expectElementTypeBegin = false;
            _expectElementTypeEnd = false;
            _expectChildrenBegin = false;
            _expectChildrenEnd = false;
            _expectPropertiesBegin = false;
            _expectPropertiesEnd = false;
            _expectPropertyName = false;
            _expectPropertyEqual = false;
            _expectPropertyValueBegin = false;
            _expectPropertyValueEnd = false;
            _expectPropertyValue = false;

            _identifier = false;
            _content = false;

            switch (lastType)
            {
                case XnlTokenType.None:
                    _expectElementName = true;
                    _expectElementTypeBegin = true;
                    _expectPropertiesBegin = true;
                    break;

                case XnlTokenType.ElementName:
                    _expectElementTypeBegin = true;
                    _expectPropertiesBegin = true;
                    break;

                case XnlTokenType.ElementTypeBegin:
                    _expectElementType = true;
                    break;

                case XnlTokenType.ElementTypeName:
                    _expectElementTypeEnd = true;
                    break;

                case XnlTokenType.ElementTypeEnd:
                    _expectPropertiesBegin = true;
                    break;

                case XnlTokenType.PropertiesBegin:
                    _expectPropertyName = true;
                    _expectPropertiesEnd = true;
                    break;

                case XnlTokenType.PropertiesEnd:
                    _expectElementName = true;
                    _expectElementTypeBegin = true;
                    _expectPropertiesBegin = true;
                    _expectChildrenBegin = true;
                    _expectChildrenEnd = true;
                    break;

                case XnlTokenType.PropertyName:
                    _expectPropertyEqual = true;
                    break;

                case XnlTokenType.PropertyEqual:
                    _expectPropertyValueBegin = true;
                    break;

                case XnlTokenType.PropertyValueBegin:
                    _expectPropertyValue = true;
                    break;

                case XnlTokenType.PropertyValue:
                    _expectPropertyValueEnd = true;
                    break;

                case XnlTokenType.PropertyValueEnd:
                    _expectPropertyName = true;
                    _expectPropertiesEnd = true;
                    break;

                case XnlTokenType.ChildrenBegin:
                    _expectElementName = true;
                    _expectElementTypeBegin = true;
                    _expectPropertiesBegin = true;
                    _expectChildrenEnd = true;
                    break;

                case XnlTokenType.ChildrenEnd:
                    _expectElementName = true;
                    _expectElementTypeBegin = true;
                    _expectPropertiesBegin = true;
                    _expectChildrenEnd = true;
                    break;
            }
        }
    }
}
