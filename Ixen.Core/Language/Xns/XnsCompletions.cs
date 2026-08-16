using Ixen.Core.Visual.Styles;
using System.Collections.Generic;

namespace Ixen.Core.Language.Xns
{
    internal static class XnsCompletions
    {
        private static readonly string[] _styleNames = BuildStyleNames();

        internal static IReadOnlyList<string> StyleNames => _styleNames;

        internal static XnsCompletionContext At(string content, int position)
        {
            if (content == null || position < 0 || position > content.Length)
            {
                return XnsCompletionContext.None;
            }

            int spanStart = WordStart(content, position);
            int length = position - spanStart;

            if (!Scan(content, spanStart, out int depth))
            {
                return XnsCompletionContext.None;
            }

            int colon = ColonBefore(content, spanStart);

            if (colon < 0)
            {
                return depth == 0
                    ? XnsCompletionContext.None
                    : new XnsCompletionContext(XnsCompletionKind.StyleName, null, spanStart, length, _styleNames);
            }

            StyleDefinition definition = StyleDefinitions.Find(NameBefore(content, colon));

            if (definition == null)
            {
                return new XnsCompletionContext(XnsCompletionKind.State, null, spanStart, length, StyleStates.All);
            }

            if (definition.Keywords.Count == 0)
            {
                return XnsCompletionContext.None;
            }

            return new XnsCompletionContext(XnsCompletionKind.StyleValue, definition.Name, spanStart, length, definition.Keywords);
        }

        private static bool Scan(string content, int end, out int depth)
        {
            depth = 0;

            int index = 0;

            while (index < end)
            {
                char c = content[index];

                if (c == '/' && index + 1 < content.Length && content[index + 1] == '/')
                {
                    index += 2;

                    while (index < end && content[index] != '\r' && content[index] != '\n')
                    {
                        index++;
                    }

                    if (index >= end)
                    {
                        return false;
                    }

                    continue;
                }

                if (c == '/' && index + 1 < content.Length && content[index + 1] == '*')
                {
                    index += 2;

                    while (index < end && !(content[index] == '*' && index + 1 < content.Length && content[index + 1] == '/'))
                    {
                        index++;
                    }

                    if (index >= end)
                    {
                        return false;
                    }

                    index += 2;
                    continue;
                }

                if (c == '{')
                {
                    depth++;
                }
                else if (c == '}' && depth > 0)
                {
                    depth--;
                }

                index++;
            }

            return true;
        }

        private static int ColonBefore(string content, int spanStart)
        {
            int cursor = spanStart - 1;

            while (cursor >= 0)
            {
                char c = content[cursor];

                if (c == '{' || c == '}' || c == '\r' || c == '\n')
                {
                    return -1;
                }

                if (c == ':')
                {
                    return cursor;
                }

                cursor--;
            }

            return -1;
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

        private static string NameBefore(string content, int colon)
        {
            int end = colon;

            while (end > 0 && (content[end - 1] == ' ' || content[end - 1] == '\t'))
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

        private static bool IsWordChar(char c)
            => char.IsLetterOrDigit(c) || c == '-' || c == '_';

        private static string[] BuildStyleNames()
        {
            var names = new List<string>();

            foreach (StyleDefinition definition in StyleDefinitions.All)
            {
                names.Add(definition.Name);
            }

            names.Sort(string.CompareOrdinal);
            return names.ToArray();
        }
    }
}
