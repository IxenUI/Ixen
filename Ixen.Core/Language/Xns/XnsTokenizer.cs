using Ixen.Core.Visual.Classes;
using Ixen.Core.Language.Base;
using System;
using System.Collections.Generic;

namespace Ixen.Core.Language.Xns
{
    internal class XnsTokenizer : BaseTokenizer<XnsToken, XnsTokenType, XnsTokenErrorType>
    {
        internal const char KEYFRAMES_MARKER = '@';
        internal const string KEYFRAMES_KEYWORD = "keyframes";
        internal const string MEDIA_KEYWORD = "media";
        internal const string CONTAINER_KEYWORD = "container";
        internal const string MIXIN_KEYWORD = "mixin";
        internal const string INCLUDE_KEYWORD = "include";
        internal const char VARIABLE_MARKER = '$';

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
            _tokens = new(EstimatedTokenCount());
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
                if (_expectClassName && ReadVariable())
                {
                    SetStatesFlags(XnsTokenType.VariableValue);
                    continue;
                }

                if (_expectStyleName && ReadInclude())
                {
                    SetStatesFlags(XnsTokenType.IncludeName);
                    continue;
                }

                if (_expectClassName && ReadMixin())
                {
                    SetStatesFlags(XnsTokenType.MixinName);
                    continue;
                }

                if (_expectClassName && ReadMedia())
                {
                    SetStatesFlags(XnsTokenType.MediaQuery);
                    continue;
                }

                if (_expectClassName && ReadContainer())
                {
                    SetStatesFlags(XnsTokenType.ContainerQuery);
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
                case XnsTokenType.ContainerQuery:
                case XnsTokenType.MixinName:
                    _expectContentBegin = true;
                    break;

                case XnsTokenType.IncludeName:
                    _expectStyleName = true;
                    _expectClassName = true;
                    _expectContentEnd = true;
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

                case XnsTokenType.VariableValue:
                    _expectClassName = true;
                    break;
            }
        }

        private bool ReadAtKeyword(string keyword, out int tokenIndex)
        {
            tokenIndex = 0;

            if (PeekNonSpaceChar() != KEYFRAMES_MARKER)
            {
                return false;
            }

            tokenIndex = _peekIndex;
            MoveCursor();

            int start = _index + 1;

            while (char.IsLetter(PeekChar()))
            {
                MoveCursor();
            }

            return Matches(start, _index, keyword);
        }

        private bool ReadName(out string name)
        {
            char c = PeekNonSpaceChar();

            if (!char.IsLetter(c) && c != '_')
            {
                name = null;
                return false;
            }

            int start = _peekIndex;

            MoveCursor();

            while (true)
            {
                c = PeekChar();

                if (!char.IsLetterOrDigit(c) && c != '_' && c != '-')
                {
                    break;
                }

                MoveCursor();
            }

            name = Slice(start, _index);

            return true;
        }

        private bool ReadMixin()
        {
            int index = _index;

            if (!ReadAtKeyword(MIXIN_KEYWORD, out int tokenIndex) || !ReadName(out string name))
            {
                _index = index;
                return false;
            }

            if (PeekNonSpaceChar() != '{')
            {
                _index = index;
                return false;
            }

            if (_contentLevel != 0)
            {
                AddError(LanguageErrorCode.SYNTAX,
                    $"A '{KEYFRAMES_MARKER}{MIXIN_KEYWORD}' block must be declared at the top level.",
                    tokenIndex, 1);
            }

            AddToken(tokenIndex, XnsTokenType.MixinName, name, _index - tokenIndex + 1);

            return true;
        }

        private bool ReadInclude()
        {
            int index = _index;

            if (!ReadAtKeyword(INCLUDE_KEYWORD, out int tokenIndex) || !ReadName(out string name))
            {
                _index = index;
                return false;
            }

            AddToken(tokenIndex, XnsTokenType.IncludeName, name, _index - tokenIndex + 1);

            return true;
        }

        private bool ReadVariable()
        {
            int index = _index;

            if (PeekNonSpaceChar() != VARIABLE_MARKER)
            {
                _index = index;
                return false;
            }

            int nameIndex = _peekIndex;
            MoveCursor();

            char c = PeekChar();

            if (!char.IsLetter(c) && c != '_')
            {
                _index = index;
                return false;
            }

            int nameStart = _peekIndex;

            while (char.IsLetterOrDigit(c) || c == '_' || c == '-')
            {
                MoveCursor();
                c = PeekChar();
            }

            int nameEnd = _index;

            if (PeekNonSpaceChar() != ':')
            {
                _index = index;
                return false;
            }

            MoveCursor();

            int rawIndex = _index + 1;

            while (true)
            {
                c = PeekChar();

                if (c == '\0' || c == '\r' || c == '\n' || c == '{' || c == '}')
                {
                    break;
                }

                if (c == '/' && StartsComment(_peekIndex))
                {
                    break;
                }

                MoveCursor();
            }

            int valueIndex = TrimmedStart(rawIndex, _index);
            int valueEnd = TrimmedEnd(rawIndex, _index);

            if (valueEnd < valueIndex)
            {
                _index = index;
                return false;
            }

            if (_contentLevel != 0)
            {
                AddError(LanguageErrorCode.SYNTAX,
                    $"A '{VARIABLE_MARKER}' variable must be declared at the top level.",
                    nameIndex, 1);
            }

            AddToken(nameIndex, XnsTokenType.VariableName, Slice(nameStart, nameEnd),
                nameEnd - nameStart + 2);
            AddToken(valueIndex, XnsTokenType.VariableValue, Slice(valueIndex, valueEnd));

            return true;
        }

        private bool ReadMedia()
            => ReadCondition(MEDIA_KEYWORD, XnsTokenType.MediaQuery);

        private bool ReadContainer()
            => ReadCondition(CONTAINER_KEYWORD, XnsTokenType.ContainerQuery);

        private bool ReadCondition(string keyword, XnsTokenType type)
        {
            int index = _index;

            if (PeekNonSpaceChar() != KEYFRAMES_MARKER)
            {
                _index = index;
                return false;
            }

            int tokenIndex = _peekIndex;
            MoveCursor();

            int keywordStart = _index + 1;

            while (char.IsLetter(PeekChar()))
            {
                MoveCursor();
            }

            if (!Matches(keywordStart, _index, keyword))
            {
                _index = index;
                return false;
            }

            int rawIndex = _index + 1;

            while (true)
            {
                char c = PeekChar();

                if (c == '\0' || c == '{' || c == '}' || c == '\r' || c == '\n')
                {
                    break;
                }

                MoveCursor();
            }

            int conditionIndex = TrimmedStart(rawIndex, _index);
            int conditionEnd = TrimmedEnd(rawIndex, _index);

            if (PeekChar() != '{' || conditionEnd < conditionIndex)
            {
                _index = index;
                return false;
            }

            AddToken(tokenIndex, type, Slice(conditionIndex, conditionEnd),
                _index - tokenIndex + 1);

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

            int keywordStart = _index + 1;

            while (char.IsLetter(PeekChar()))
            {
                MoveCursor();
            }

            if (!Matches(keywordStart, _index, KEYFRAMES_KEYWORD))
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

            int nameStart = _peekIndex;

            MoveCursor();

            while (true)
            {
                c = PeekChar();

                if (char.IsLetterOrDigit(c) || c == '_' || c == '-')
                {
                    MoveCursor();
                    continue;
                }

                break;
            }

            int nameEnd = _index;

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

            AddToken(tokenIndex, XnsTokenType.ClassName, KEYFRAMES_MARKER + Slice(nameStart, nameEnd),
                _index - tokenIndex + 1);

            return true;
        }

        private bool ReadSelectorEntry()
        {
            int index = _index;
            char c = PeekNonSpaceChar();

            if (c == StyleScope.IMMEDIATE && _contentLevel > 0)
            {
                MoveCursor();
                c = PeekNonSpaceChar();
            }

            if (!char.IsLetterOrDigit(c) && c != '.' && c != '#' && c != '_')
            {
                _index = index;
                return false;
            }

            MoveCursor();

            while (true)
            {
                c = PeekChar();

                if (char.IsLetterOrDigit(c) || c == '_' || c == '-' || c == ':' || c == '%'
                    || c == '(' || c == ')')
                {
                    MoveCursor();
                    continue;
                }

                break;
            }

            return true;
        }

        private bool ReadClassName()
        {
            int index = _index;

            PeekNonSpaceChar();

            int tokenIndex = _peekIndex;

            if (!ReadSelectorEntry())
            {
                _index = index;
                return false;
            }

            int nameEnd = _index;
            char c = PeekNonSpaceChar();

            while (c == StyleScope.SELECTOR_SEPARATOR)
            {
                MoveCursor();

                if (!ReadSelectorEntry())
                {
                    c = PeekNonSpaceChar();

                    AddError(LanguageErrorCode.SYNTAX,
                        "A selector list needs a selector after each comma.", _peekIndex, 1);

                    break;
                }

                nameEnd = _index;
                c = PeekNonSpaceChar();
            }

            if (c == '{')
            {
                AddToken(tokenIndex, XnsTokenType.ClassName, Compact(tokenIndex, nameEnd),
                    nameEnd - tokenIndex + 1);

                return true;
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

                MoveCursor();

                while (true)
                {
                    c = PeekChar();
                    if (char.IsLetter(c) || c == '-')
                    {
                        MoveCursor();
                        continue;
                    }

                    break;
                }

                int nameEnd = _index;

                c = PeekNonSpaceChar();

                if (c == ':')
                {
                    AddToken(tokenIndex, XnsTokenType.StyleName, Slice(tokenIndex, nameEnd));
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

            if (char.IsLetterOrDigit(c) || c == '#' || c == '?' || c == '_' || c == '$' || c == '-')
            {
                int tokenIndex = _peekIndex;

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
                        || c == '-' || c == '_' || c == '/' || c == '$' || c == '(' || c == ')' || c == '+'
                        || c == ',' || c == ' ' || c == '\t')
                    {
                        MoveCursor();
                        continue;
                    }

                    break;
                }

                int valueEnd = TrimmedEnd(tokenIndex, _index);

                if (valueEnd >= tokenIndex)
                {
                    AddToken(tokenIndex, XnsTokenType.StyleValue, Slice(tokenIndex, valueEnd));
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
