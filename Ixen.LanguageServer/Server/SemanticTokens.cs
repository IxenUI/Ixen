using System.Collections.Generic;
using Ixen.Core.Language.Xnl;
using Ixen.Core.Language.Xns;

namespace Ixen.LanguageServer.Server
{
    internal static class SemanticTokens
    {
        public const int TYPE = 0;
        public const int VARIABLE = 1;
        public const int PROPERTY = 2;
        public const int STRING = 3;
        public const int COMMENT = 4;
        public const int MACRO = 5;
        public const int FUNCTION = 6;
        public const int OPERATOR = 7;

        public static readonly string[] Legend =
        {
            "type",
            "variable",
            "property",
            "string",
            "comment",
            "macro",
            "function",
            "operator"
        };

        public static readonly string[] Modifiers = new string[0];

        public static IReadOnlyList<int> Build(string path, string text, LineIndex lines)
        {
            List<int> data = new List<int>();
            Cursor cursor = new Cursor(lines, data);

            if (Uris.IsXns(path))
            {
                XnsSource source = new XnsSource(text ?? string.Empty);

                foreach (XnsToken token in source.Tokenize())
                {
                    cursor.Add(token.Index, token.Length, KindOf(token));
                }
            }
            else if (Uris.IsXnl(path))
            {
                XnlSource source = new XnlSource(text ?? string.Empty);

                foreach (XnlToken token in source.Tokenize())
                {
                    cursor.Add(token.Index, token.Length, KindOf(token));
                }
            }

            return data;
        }

        private static int KindOf(XnsToken token)
        {
            if (token.IsError)
            {
                return -1;
            }

            switch (token.Type)
            {
                case XnsTokenType.ClassName:
                    return TYPE;
                case XnsTokenType.MediaQuery:
                case XnsTokenType.ContainerQuery:
                    return MACRO;
                case XnsTokenType.MixinName:
                case XnsTokenType.IncludeName:
                    return FUNCTION;
                case XnsTokenType.VariableName:
                    return VARIABLE;
                case XnsTokenType.StyleName:
                    return PROPERTY;
                case XnsTokenType.VariableValue:
                case XnsTokenType.StyleValue:
                case XnsTokenType.StyleSizeValue:
                case XnsTokenType.StyleColorValue:
                    return STRING;
                case XnsTokenType.Comment:
                    return COMMENT;
                default:
                    return -1;
            }
        }

        private static int KindOf(XnlToken token)
        {
            if (token.IsError)
            {
                return -1;
            }

            switch (token.Type)
            {
                case XnlTokenType.ElementName:
                    return VARIABLE;
                case XnlTokenType.ElementTypeName:
                    return TYPE;
                case XnlTokenType.ElementTypeBegin:
                case XnlTokenType.ElementTypeEnd:
                    return OPERATOR;
                case XnlTokenType.PropertyName:
                    return PROPERTY;
                case XnlTokenType.PropertyValue:
                case XnlTokenType.PropertyValueBegin:
                case XnlTokenType.PropertyValueEnd:
                    return STRING;
                case XnlTokenType.CodeRegionBegin:
                case XnlTokenType.CodeRegionEnd:
                case XnlTokenType.CodeStatement:
                    return MACRO;
                case XnlTokenType.Comment:
                    return COMMENT;
                default:
                    return -1;
            }
        }

        private sealed class Cursor
        {
            private readonly LineIndex _lines;
            private readonly List<int> _data;

            private int _line;
            private int _character;
            private bool _started;

            public Cursor(LineIndex lines, List<int> data)
            {
                _lines = lines;
                _data = data;
            }

            public void Add(int index, int length, int kind)
            {
                if (kind < 0 || length <= 0 || index < 0 || index >= _lines.Length)
                {
                    return;
                }

                int end = index + length;

                if (end > _lines.Length)
                {
                    end = _lines.Length;
                }

                int line = _lines.LineOf(index);

                while (line < _lines.Count)
                {
                    int lineStart = _lines.StartOf(line);
                    int lineEnd = _lines.EndOf(line);
                    int from = index > lineStart ? index : lineStart;
                    int to = end < lineEnd ? end : lineEnd;

                    if (to > from)
                    {
                        Emit(line, from - lineStart, to - from, kind);
                    }

                    if (line + 1 >= _lines.Count || end <= _lines.StartOf(line + 1))
                    {
                        return;
                    }

                    line++;
                }
            }

            private void Emit(int line, int character, int length, int kind)
            {
                if (_started && (line < _line || (line == _line && character < _character)))
                {
                    return;
                }

                _data.Add(_started ? line - _line : line);
                _data.Add(_started && line == _line ? character - _character : character);
                _data.Add(length);
                _data.Add(kind);
                _data.Add(0);

                _line = line;
                _character = character;
                _started = true;
            }
        }
    }
}
