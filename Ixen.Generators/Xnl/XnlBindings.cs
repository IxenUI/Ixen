using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using System.Collections.Generic;
using System.Text;

namespace Ixen.Generators.Xnl
{
    internal struct BindingPart
    {
        internal bool IsExpression;
        internal string Text;
    }

    internal static class XnlBindings
    {
        internal const string MODEL_PARAMETER = "model";
        internal const string MODEL_FIELD = "_model";

        internal const char TWO_WAY_OPEN = '[';
        internal const char TWO_WAY_CLOSE = ']';

        internal static bool HasBinding(string value)
            => TwoWayPath(value) != null || IsBinding(Parse(value));

        internal static string TwoWayPath(string value)
        {
            string inner = Bracketed(value);

            return inner != null && IsMemberPath(inner) ? inner : null;
        }

        internal static string Inner(string value) => Bracketed(value);

        internal static bool IsEscaped(string value)
        {
            string inner = Bracketed(value);

            return inner != null && inner.Length >= 2
                && inner[0] == TWO_WAY_OPEN && inner[inner.Length - 1] == TWO_WAY_CLOSE;
        }

        internal static string Unescaped(string value)
            => IsEscaped(value) ? Bracketed(value) : value;

        internal static List<BindingPart> PathParts(string path)
        {
            return new List<BindingPart>
            {
                new BindingPart { IsExpression = true, Text = path }
            };
        }

        private static string Bracketed(string value)
        {
            string trimmed = value == null ? null : value.Trim();

            if (trimmed == null || trimmed.Length < 3
                || trimmed[0] != TWO_WAY_OPEN || trimmed[trimmed.Length - 1] != TWO_WAY_CLOSE)
            {
                return null;
            }

            return trimmed.Substring(1, trimmed.Length - 2).Trim();
        }

        internal static bool IsMemberPath(string expression)
        {
            if (string.IsNullOrEmpty(expression))
            {
                return false;
            }

            bool expectStart = true;

            foreach (char c in expression)
            {
                if (expectStart)
                {
                    if (!IsIdentifierStart(c))
                    {
                        return false;
                    }

                    expectStart = false;
                    continue;
                }

                if (c == '.')
                {
                    expectStart = true;
                    continue;
                }

                if (!IsIdentifierPart(c))
                {
                    return false;
                }
            }

            return !expectStart;
        }

        internal static bool IsBinding(List<BindingPart> parts)
        {
            foreach (BindingPart part in parts)
            {
                if (part.IsExpression)
                {
                    return true;
                }
            }

            return false;
        }

        internal static string LiteralText(List<BindingPart> parts)
        {
            if (parts.Count == 0)
            {
                return string.Empty;
            }

            if (parts.Count == 1)
            {
                return parts[0].Text;
            }

            var sb = new StringBuilder();

            foreach (BindingPart part in parts)
            {
                sb.Append(part.Text);
            }

            return sb.ToString();
        }

        internal static List<BindingPart> Parse(string value)
        {
            var parts = new List<BindingPart>();

            if (string.IsNullOrEmpty(value))
            {
                return parts;
            }

            var literal = new StringBuilder();
            int index = 0;

            while (index < value.Length)
            {
                char c = value[index];

                if (c == '{' && index + 1 < value.Length && value[index + 1] == '{')
                {
                    literal.Append('{');
                    index += 2;
                    continue;
                }

                if (c == '}' && index + 1 < value.Length && value[index + 1] == '}')
                {
                    literal.Append('}');
                    index += 2;
                    continue;
                }

                if (c != '{')
                {
                    literal.Append(c);
                    index++;
                    continue;
                }

                int end = FindExpressionEnd(value, index + 1);

                if (end < 0)
                {
                    literal.Append(c);
                    index++;
                    continue;
                }

                if (literal.Length > 0)
                {
                    parts.Add(new BindingPart { Text = literal.ToString() });
                    literal.Clear();
                }

                parts.Add(new BindingPart
                {
                    IsExpression = true,
                    Text = value.Substring(index + 1, end - index - 1)
                });

                index = end + 1;
            }

            if (literal.Length > 0)
            {
                parts.Add(new BindingPart { Text = literal.ToString() });
            }

            return parts;
        }

        private static int FindExpressionEnd(string value, int start)
        {
            int depth = 0;

            for (int i = start; i < value.Length; i++)
            {
                char c = value[i];

                if (c == '"' || c == '\'')
                {
                    i = SkipLiteral(value, i);
                    continue;
                }

                if (c == '{')
                {
                    depth++;
                    continue;
                }

                if (c != '}')
                {
                    continue;
                }

                if (depth == 0)
                {
                    return i;
                }

                depth--;
            }

            return -1;
        }

        private static int SkipLiteral(string value, int start)
        {
            char quote = value[start];

            for (int i = start + 1; i < value.Length; i++)
            {
                if (value[i] == '\\')
                {
                    i++;
                    continue;
                }

                if (value[i] == quote)
                {
                    return i;
                }
            }

            return value.Length - 1;
        }

        internal static string BuildExpression(List<BindingPart> parts, HashSet<string> memberNames,
            string prefix = MODEL_PARAMETER)
        {
            if (parts.Count == 1 && parts[0].IsExpression)
            {
                return Qualify(parts[0].Text, memberNames, prefix);
            }

            var sb = new StringBuilder("$\"");

            foreach (BindingPart part in parts)
            {
                if (part.IsExpression)
                {
                    sb.Append('{').Append(Qualify(part.Text, memberNames, prefix)).Append('}');
                    continue;
                }

                sb.Append(SymbolDisplay.FormatLiteral(part.Text, false)
                    .Replace("{", "{{")
                    .Replace("}", "}}"));
            }

            return sb.Append('"').ToString();
        }

        internal static string Qualify(string expression, HashSet<string> memberNames,
            string prefix = MODEL_PARAMETER)
        {
            if (string.IsNullOrEmpty(expression))
            {
                return expression;
            }

            var sb = new StringBuilder();
            int index = 0;
            bool memberAccess = false;

            while (index < expression.Length)
            {
                char c = expression[index];

                if (c == '"' || c == '\'')
                {
                    int end = SkipLiteral(expression, index);
                    sb.Append(expression, index, end - index + 1);
                    index = end + 1;
                    memberAccess = false;
                    continue;
                }

                if (!IsIdentifierStart(c))
                {
                    if (c == '.')
                    {
                        memberAccess = true;
                    }
                    else if (!char.IsWhiteSpace(c))
                    {
                        memberAccess = false;
                    }

                    sb.Append(c);
                    index++;
                    continue;
                }

                int start = index;

                while (index < expression.Length && IsIdentifierPart(expression[index]))
                {
                    index++;
                }

                string identifier = expression.Substring(start, index - start);

                if (!memberAccess && memberNames.Contains(identifier))
                {
                    sb.Append(prefix).Append('.');
                }

                sb.Append(identifier);
                memberAccess = false;
            }

            return sb.ToString();
        }

        private static bool IsIdentifierStart(char c)
            => char.IsLetter(c) || c == '_';

        private static bool IsIdentifierPart(char c)
            => char.IsLetterOrDigit(c) || c == '_';

        internal static HashSet<string> MemberNames(INamedTypeSymbol model)
        {
            var names = new HashSet<string>();

            for (INamedTypeSymbol current = model; current != null; current = current.BaseType)
            {
                foreach (ISymbol member in current.GetMembers())
                {
                    if (member.Kind == SymbolKind.Property
                        || member.Kind == SymbolKind.Field
                        || member.Kind == SymbolKind.Method)
                    {
                        names.Add(member.Name);
                    }
                }
            }

            return names;
        }
    }
}
