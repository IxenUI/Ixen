using Ixen.Core.Language.Base;
using System;
using System.Collections.Generic;
using System.Text;

namespace Ixen.Core.Language.Xnl
{
    internal class XnlTokenizer : BaseTokenizer<XnlToken, XnlTokenType, XnlTokenErrorType>
    {
        private const char CODE_MARKER = '@';

        private bool _expectElementName = false;

        private bool _expectElementTypeName = false;
        private bool _expectElementTypeBegin = false;
        private bool _expectElementTypeEnd = false;

        private bool _expectChildrenBegin = false;
        private bool _expectChildrenEnd = false;

        private bool _expectCodeRegionBegin = false;
        private bool _expectCodeRegionEnd = false;

        private bool _expectPropertiesBegin = false;
        private bool _expectPropertiesEnd = false;

        private bool _expectPropertyName = false;
        private bool _expectPropertyEqual = false;
        private bool _expectPropertyValueBegin = false;
        private bool _expectPropertyValueEnd = false;
        private bool _expectPropertyValue = false;

        private int _contentLevel;
        private int _regionLevel;

        public XnlTokenizer(string source)
            : base(source)
        { }

        public XnlTokenizer(SourceContent source)
            : base(source)
        { }

        public override List<XnlToken> Tokenize()
        {
            _tokens = new();
            _diagnostics.Clear();

            ResetPosition();
            SetStatesFlags(XnlTokenType.None);

            try
            {
                ReadTokens();
            }
            catch (Exception ex)
            {
                AddError(LanguageErrorCode.SYNTAX, $"Tokenizer failure: {ex.Message}", _index, 0);
            }

            if (_contentLevel != 0 || _regionLevel != 0)
            {
                ReportUnclosedBlock();
            }

            return _tokens;
        }

        protected override XnlTokenType GetCommentType() => XnlTokenType.Comment;

        private void ReadTokens()
        {

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

                if (_expectCodeRegionEnd && ReadCodeRegionEnd())
                {
                    SetStatesFlags(XnlTokenType.CodeRegionEnd);
                    continue;
                }

                if (_expectCodeRegionBegin && ReadCodeRegionBegin())
                {
                    SetStatesFlags(XnlTokenType.CodeRegionBegin);
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

                ReportUnexpectedCharacter();
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
            _expectCodeRegionBegin = false;
            _expectCodeRegionEnd = false;
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
                    _expectCodeRegionBegin = true;
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
                    _expectCodeRegionBegin = true;
                    _expectCodeRegionEnd = true;
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
                    _expectCodeRegionBegin = true;
                    _contentLevel++;
                    break;

                case XnlTokenType.ChildrenEnd:
                    _contentLevel--;
                    _expectElementName = true;
                    _expectElementTypeBegin = true;
                    _expectPropertiesBegin = true;
                    _expectChildrenEnd = true;
                    _expectCodeRegionBegin = true;
                    _expectCodeRegionEnd = true;
                    break;

                case XnlTokenType.CodeRegionBegin:
                    _regionLevel++;
                    _expectElementName = true;
                    _expectElementTypeBegin = true;
                    _expectPropertiesBegin = true;
                    _expectCodeRegionBegin = true;
                    _expectCodeRegionEnd = true;
                    break;

                case XnlTokenType.CodeRegionEnd:
                    _regionLevel--;
                    _expectElementName = true;
                    _expectElementTypeBegin = true;
                    _expectPropertiesBegin = true;
                    _expectChildrenEnd = true;
                    _expectCodeRegionBegin = true;
                    _expectCodeRegionEnd = true;
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

        private bool ReadCodeRegionEnd()
        {
            int index = _index;

            if (PeekNonSpaceChar() != CODE_MARKER)
            {
                _index = index;
                return false;
            }

            int tokenIndex = _peekIndex;
            MoveCursor();

            if (PeekChar() != '}')
            {
                _index = index;
                return false;
            }

            MoveCursor();
            AddToken(tokenIndex, XnlTokenType.CodeRegionEnd, "@}");
            return true;
        }

        private bool ReadCodeRegionBegin()
        {
            int index = _index;

            if (PeekNonSpaceChar() != CODE_MARKER)
            {
                _index = index;
                return false;
            }

            int tokenIndex = _peekIndex;
            MoveCursor();

            var sb = new StringBuilder();

            while (true)
            {
                char c = PeekChar();

                if (c == '\0')
                {
                    break;
                }

                if (c == '"' || c == '\'')
                {
                    ReadCodeLiteral(sb, c);
                    continue;
                }

                if (c == '{')
                {
                    MoveCursor();
                    AddToken(tokenIndex, XnlTokenType.CodeRegionBegin, sb.ToString().Trim());
                    return true;
                }

                sb.Append(c);
                MoveCursor();
            }

            _index = index;
            return false;
        }

        private void ReadCodeLiteral(StringBuilder sb, char quote)
        {
            sb.Append(quote);
            MoveCursor();

            while (true)
            {
                char c = PeekChar();

                if (c == '\0')
                {
                    return;
                }

                sb.Append(c);
                MoveCursor();

                if (c == '\\')
                {
                    char escaped = PeekChar();

                    if (escaped != '\0')
                    {
                        sb.Append(escaped);
                        MoveCursor();
                    }

                    continue;
                }

                if (c == quote)
                {
                    return;
                }
            }
        }

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
            int tokenIndex = _index + 1;
            var sb = new StringBuilder();
            bool terminated = false;

            while (true)
            {
                char c = PeekChar();

                if (c == '"')
                {
                    terminated = true;
                    break;
                }

                if (c == '\0' || c == '\r' || c == '\n')
                {
                    break;
                }

                sb.Append(c);
                MoveCursor();
            }

            if (terminated)
            {
                AddToken(tokenIndex, XnlTokenType.PropertyValue, sb.ToString());
                return true;
            }

            _index = index;
            return false;
        }
    }
}
