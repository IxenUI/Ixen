using Ixen.Core.Language.Base;
using System;
using System.Collections.Generic;
using System.Text;

namespace Ixen.Core.Language.Xnl
{
    internal class XnlTokenizer : BaseTokenizer<XnlToken, XnlTokenType, XnlTokenErrorType>
    {
        private bool _expectElementName = false;

        private bool _expectElementTypeName = false;
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

        private int _contentLevel;

        public XnlTokenizer(string source)
            : base(source)
        { }

        public XnlTokenizer(SourceContent source)
            : base(source)
        { }

        public override List<XnlToken> Tokenize()
        {
            _tokens = new();

            ResetPosition();
            SetStatesFlags(XnlTokenType.None);
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

        protected override XnlTokenType GetCommentType() => XnlTokenType.Comment;

        private void ReadTokens()
        {
            _errorOccured = false;

            while (PeekChar() != '\0')
            {
                if (_expectElementName && ReadElementName())
                {
                    SetStatesFlags(XnlTokenType.ElementName);
                    continue;
                }

                if (_expectElementTypeBegin && ReadElementTypeBegin())
                {
                    SetStatesFlags(XnlTokenType.ElementTypeBegin);
                    continue;
                }

                if (_expectElementTypeEnd && ReadElementTypeEnd())
                {
                    SetStatesFlags(XnlTokenType.ElementTypeEnd);
                    continue;
                }

                if (_expectElementTypeName && ReadElementTypeName())
                {
                    SetStatesFlags(XnlTokenType.ElementTypeName);
                    continue;
                }

                if (_expectChildrenBegin && ReadChildrenBegin())
                {
                    SetStatesFlags(XnlTokenType.ChildrenBegin);
                    continue;
                }

                if (_expectChildrenEnd && ReadChildrenEnd())
                {
                    SetStatesFlags(XnlTokenType.ChildrenEnd);
                    continue;
                }

                if (_expectPropertiesBegin && ReadPropertiesBegin())
                {
                    SetStatesFlags(XnlTokenType.PropertiesBegin);
                    continue;
                }

                if (_expectPropertiesEnd && ReadPropertiesEnd())
                {
                    SetStatesFlags(XnlTokenType.PropertiesEnd);
                    continue;
                }

                if (_expectPropertyName && ReadPropertyName())
                {
                    SetStatesFlags(XnlTokenType.PropertyName);
                    continue;
                }

                if (_expectPropertyEqual && ReadPropertyEqual())
                {
                    SetStatesFlags(XnlTokenType.PropertyEqual);
                    continue;
                }

                if (_expectPropertyValueBegin && ReadPropertyValueBegin())
                {
                    SetStatesFlags(XnlTokenType.PropertyValueBegin);
                    continue;
                }

                if (_expectPropertyValueEnd && ReadPropertyValueEnd())
                {
                    SetStatesFlags(XnlTokenType.PropertyValueEnd);
                    continue;
                }

                if (_expectPropertyValue && ReadPropertyValue())
                {
                    SetStatesFlags(XnlTokenType.PropertyValue);
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
            _expectElementName = false;
            _expectElementTypeName = false;
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
        }

        private void SetStatesFlags(XnlTokenType lastType)
        {
            ResetStatesFlags();

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
                    _expectElementTypeName = true;
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
                    _contentLevel++;
                    break;

                case XnlTokenType.ChildrenEnd:
                    _contentLevel--;
                    _expectElementName = true;
                    _expectElementTypeBegin = true;
                    _expectPropertiesBegin = true;
                    _expectChildrenEnd = true;
                    break;
            }
        }

        private bool ReadElementTypeBegin() => ReadCharToken(XnlTokenType.ElementTypeBegin, '<');
        private bool ReadElementTypeEnd() => ReadCharToken(XnlTokenType.ElementTypeEnd, '>');
        private bool ReadPropertiesBegin() => ReadCharToken(XnlTokenType.PropertiesBegin, '{');
        private bool ReadPropertiesEnd() => ReadCharToken(XnlTokenType.PropertiesEnd, '}');
        private bool ReadChildrenBegin() => ReadCharToken(XnlTokenType.ChildrenBegin, '[');
        private bool ReadChildrenEnd() => ReadCharToken(XnlTokenType.ChildrenEnd, ']');
        private bool ReadPropertyValueBegin() => ReadCharToken(XnlTokenType.PropertyValueBegin, '"');
        private bool ReadPropertyValueEnd() => ReadCharToken(XnlTokenType.PropertyValueEnd, '"');
        private bool ReadPropertyEqual() => ReadCharToken(XnlTokenType.PropertyEqual, ':');

        private bool ReadElementName()
        {
            int index = _index;
            char c = PeekNonSpaceChar();

            if (char.IsLetter(c) || c == '_')
            {
                int tokenIndex = _peekIndex;
                var sb = new StringBuilder();
                sb.Append(c);
                MoveCursor();

                while (true)
                {
                    c = PeekChar();
                    if (char.IsLetterOrDigit(c) || c == '_')
                    {
                        sb.Append(c);
                        MoveCursor();
                        continue;
                    }

                    break;
                }

                c = PeekNonSpaceChar();

                if ((c == '{' || c == '<') && (sb.Length >= 1))
                {
                    AddToken(tokenIndex, XnlTokenType.ElementName, sb.ToString());
                    return true;
                }
            }

            _index = index;
            return false;
        }

        private bool ReadElementTypeName()
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
                    if (char.IsLetterOrDigit(c))
                    {
                        sb.Append(c);
                        MoveCursor();
                        continue;
                    }

                    break;
                }

                c = PeekNonSpaceChar();

                if (c == '>' && sb.Length >= 1)
                {
                    AddToken(tokenIndex, XnlTokenType.ElementTypeName, sb.ToString());
                    return true;
                }
            }

            _index = index;
            return false;
        }

        private bool ReadPropertyName()
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
                    AddToken(tokenIndex, XnlTokenType.PropertyName, sb.ToString());
                    return true;
                }
            }

            _index = index;
            return false;
        }

        private bool ReadPropertyValue()
        {
            int index = _index;
            char c = PeekNonSpaceChar();

            if (c != '"')
            {
                int tokenIndex = _peekIndex;
                var sb = new StringBuilder();
                sb.Append(c);
                MoveCursor();

                while (true)
                {
                    c = PeekChar();
                    if (c != '"')
                    {
                        sb.Append(c);
                        MoveCursor();
                        continue;
                    }

                    break;
                }

                if (sb.Length >= 1)
                {
                    AddToken(tokenIndex, XnlTokenType.PropertyValue, sb.ToString());
                    return true;
                }
            }

            _index = index;
            return false;
        }
    }
}
