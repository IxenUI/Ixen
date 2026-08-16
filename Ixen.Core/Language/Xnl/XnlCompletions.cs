using System;
using System.Collections.Generic;

namespace Ixen.Core.Language.Xnl
{
    internal static class XnlCompletions
    {
        private enum ScanState
        {
            Text,
            LineComment,
            BlockComment,
            Value
        }

        internal static IReadOnlyList<string> ElementTypes => XnlTypes.Names;

        internal static XnlCompletionContext At(string content, int position)
        {
            if (content == null || position < 0 || position > content.Length)
            {
                return XnlCompletionContext.None;
            }

            int spanStart = WordStart(content, position);
            int length = position - spanStart;

            ScanState state = Scan(content, spanStart, out bool inBlock, out string typeName);

            if (state == ScanState.LineComment || state == ScanState.BlockComment)
            {
                return XnlCompletionContext.None;
            }

            if (state == ScanState.Value)
            {
                return ValueContext(content, spanStart, length, typeName);
            }

            if (inBlock)
            {
                return NameContext(spanStart, length, typeName);
            }

            return spanStart > 0 && content[spanStart - 1] == '<'
                ? new XnlCompletionContext(XnlCompletionKind.ElementType, null, null, spanStart, length, XnlTypes.Names)
                : XnlCompletionContext.None;
        }

        private static XnlCompletionContext NameContext(int spanStart, int length, string typeName)
        {
            Type type = XnlTypes.Find(typeName);

            IReadOnlyList<string> items = type == null
                ? XnlTypes.UniversalProperties
                : XnlTypes.PropertiesOf(type);

            return new XnlCompletionContext(XnlCompletionKind.PropertyName, typeName, null, spanStart, length, items);
        }

        private static XnlCompletionContext ValueContext(string content, int spanStart, int length, string typeName)
        {
            Type type = XnlTypes.Find(typeName);

            if (type == null)
            {
                return XnlCompletionContext.None;
            }

            string property = PropertyBefore(content, spanStart);
            IReadOnlyList<string> items = XnlTypes.ValuesOf(type, property);

            return items.Count == 0
                ? XnlCompletionContext.None
                : new XnlCompletionContext(XnlCompletionKind.PropertyValue, typeName, property, spanStart, length, items);
        }

        private static ScanState Scan(string content, int end, out bool inBlock, out string typeName)
        {
            var state = ScanState.Text;
            int index = 0;

            inBlock = false;
            typeName = null;

            while (index < end)
            {
                char c = content[index];

                switch (state)
                {
                    case ScanState.LineComment:
                        if (c == '\r' || c == '\n')
                        {
                            state = ScanState.Text;
                        }

                        index++;
                        continue;

                    case ScanState.BlockComment:
                        if (c == '*' && index + 1 < content.Length && content[index + 1] == '/')
                        {
                            state = ScanState.Text;
                            index += 2;
                            continue;
                        }

                        index++;
                        continue;

                    case ScanState.Value:
                        if (c == '"' || c == '\r' || c == '\n')
                        {
                            state = ScanState.Text;
                        }

                        index++;
                        continue;
                }

                if (c == '/' && index + 1 < content.Length && content[index + 1] == '/')
                {
                    state = ScanState.LineComment;
                    index += 2;
                    continue;
                }

                if (c == '/' && index + 1 < content.Length && content[index + 1] == '*')
                {
                    state = ScanState.BlockComment;
                    index += 2;
                    continue;
                }

                if (c == '"')
                {
                    state = ScanState.Value;
                }
                else if (c == '{')
                {
                    inBlock = true;
                    typeName = TypeBefore(content, index);
                }
                else if (c == '}')
                {
                    inBlock = false;
                    typeName = null;
                }

                index++;
            }

            return state;
        }

        private static string TypeBefore(string content, int brace)
        {
            int end = brace;

            while (end > 0 && char.IsWhiteSpace(content[end - 1]))
            {
                end--;
            }

            if (end == 0 || content[end - 1] != '>')
            {
                return null;
            }

            int open = content.LastIndexOf('<', end - 1);

            return open < 0 ? null : content.Substring(open + 1, end - open - 2);
        }

        private static string PropertyBefore(string content, int spanStart)
        {
            int cursor = spanStart - 1;

            while (cursor >= 0 && content[cursor] != ':')
            {
                if (content[cursor] == '{' || content[cursor] == '\r' || content[cursor] == '\n')
                {
                    return null;
                }

                cursor--;
            }

            if (cursor < 0)
            {
                return null;
            }

            int end = cursor;

            while (end > 0 && char.IsWhiteSpace(content[end - 1]))
            {
                end--;
            }

            int start = end;

            while (start > 0 && IsWordChar(content[start - 1]))
            {
                start--;
            }

            return start == end ? null : content.Substring(start, end - start);
        }

        private static int WordStart(string content, int position)
        {
            int cursor = position;

            while (cursor > 0 && IsWordChar(content[cursor - 1]))
            {
                cursor--;
            }

            return cursor;
        }

        private static bool IsWordChar(char c)
            => char.IsLetterOrDigit(c) || c == '-' || c == '_';
    }
}
