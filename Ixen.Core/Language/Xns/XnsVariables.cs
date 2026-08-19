using Ixen.Core.Language.Base;
using System.Collections.Generic;
using System.Text;

namespace Ixen.Core.Language.Xns
{
    internal class XnsVariables
    {
        private const int MAX_DEPTH = 16;

        private readonly Dictionary<string, string> _values = new Dictionary<string, string>();

        internal bool IsEmpty => _values.Count == 0;

        internal static XnsVariables Resolve(List<XnsVariable> declarations, List<LanguageError> errors)
        {
            var variables = new XnsVariables();

            if (declarations == null || declarations.Count == 0)
            {
                return variables;
            }

            var raw = new Dictionary<string, XnsVariable>();

            foreach (XnsVariable declaration in declarations)
            {
                raw[declaration.Name] = declaration;
            }

            foreach (XnsVariable declaration in declarations)
            {
                var seen = new HashSet<string> { declaration.Name };

                variables._values[declaration.Name] = XnsCalc.Evaluate(
                    Expand(declaration.Value, declaration.ValueIndex, raw, seen, 0, errors),
                    declaration.ValueIndex,
                    errors);
            }

            return variables;
        }

        private static string Expand(string text, int index, Dictionary<string, XnsVariable> raw,
            HashSet<string> seen, int depth, List<LanguageError> errors)
        {
            if (text == null || text.IndexOf(XnsTokenizer.VARIABLE_MARKER) < 0)
            {
                return text;
            }

            if (depth >= MAX_DEPTH)
            {
                errors.Add(new LanguageError(
                    LanguageErrorCode.INVALID_STYLE_VALUE,
                    $"'{text}' nests variables too deeply.",
                    index,
                    text.Length));

                return text;
            }

            var result = new StringBuilder();
            int position = 0;

            while (position < text.Length)
            {
                char c = text[position];

                if (c != XnsTokenizer.VARIABLE_MARKER)
                {
                    result.Append(c);
                    position++;
                    continue;
                }

                int start = position + 1;
                int end = start;

                while (end < text.Length
                    && (char.IsLetterOrDigit(text[end]) || text[end] == '_' || text[end] == '-'))
                {
                    end++;
                }

                string name = text.Substring(start, end - start);

                if (name.Length == 0)
                {
                    result.Append(c);
                    position++;
                    continue;
                }

                if (!raw.TryGetValue(name, out XnsVariable referenced))
                {
                    errors.Add(new LanguageError(
                        LanguageErrorCode.INVALID_STYLE_VALUE,
                        $"'{XnsTokenizer.VARIABLE_MARKER}{name}' is not a declared variable.",
                        index + position,
                        name.Length + 1));

                    position = end;
                    continue;
                }

                if (!seen.Add(name))
                {
                    errors.Add(new LanguageError(
                        LanguageErrorCode.INVALID_STYLE_VALUE,
                        $"'{XnsTokenizer.VARIABLE_MARKER}{name}' refers to itself.",
                        index + position,
                        name.Length + 1));

                    position = end;
                    continue;
                }

                result.Append(Expand(referenced.Value, referenced.ValueIndex, raw, seen, depth + 1, errors));
                seen.Remove(name);

                position = end;
            }

            return result.ToString();
        }

        internal string Substitute(string text, int index, List<LanguageError> errors)
        {
            if (text == null || text.IndexOf(XnsTokenizer.VARIABLE_MARKER) < 0)
            {
                return text;
            }

            var result = new StringBuilder();
            int position = 0;

            while (position < text.Length)
            {
                char c = text[position];

                if (c != XnsTokenizer.VARIABLE_MARKER)
                {
                    result.Append(c);
                    position++;
                    continue;
                }

                int start = position + 1;
                int end = start;

                while (end < text.Length
                    && (char.IsLetterOrDigit(text[end]) || text[end] == '_' || text[end] == '-'))
                {
                    end++;
                }

                string name = text.Substring(start, end - start);

                if (name.Length == 0)
                {
                    result.Append(c);
                    position++;
                    continue;
                }

                if (!_values.TryGetValue(name, out string value))
                {
                    errors.Add(new LanguageError(
                        LanguageErrorCode.INVALID_STYLE_VALUE,
                        $"'{XnsTokenizer.VARIABLE_MARKER}{name}' is not a declared variable.",
                        index + position,
                        name.Length + 1));

                    position = end;
                    continue;
                }

                result.Append(value);
                position = end;
            }

            return result.ToString();
        }
    }
}
