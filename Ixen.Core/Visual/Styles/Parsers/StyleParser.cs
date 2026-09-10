using System.Collections.Generic;
using System.Text;

namespace Ixen.Core.Visual.Styles.Parsers
{
    internal abstract class StyleParser
    {
        protected string _content;

        public bool IsValid { get; protected set; } = true;

        public StyleParser(string content)
        {
            _content = content;
            IsValid = Parse();
        }

        protected abstract bool Parse();

        protected static string[] SplitTokens(string content)
        {
            if (string.IsNullOrEmpty(content))
            {
                return null;
            }

            var parts = new List<string>();
            var current = new StringBuilder();
            int depth = 0;

            foreach (char c in content)
            {
                if (c == '(')
                {
                    depth++;
                }
                else if (c == ')')
                {
                    depth--;
                }
                else if ((c == ' ' || c == '\t') && depth == 0)
                {
                    if (current.Length > 0)
                    {
                        parts.Add(current.ToString());
                        current.Clear();
                    }

                    continue;
                }

                current.Append(c);
            }

            if (current.Length > 0)
            {
                parts.Add(current.ToString());
            }

            return depth == 0 ? parts.ToArray() : null;
        }

        protected static string[] SplitArguments(string content)
        {
            if (content == null)
            {
                return null;
            }

            var parts = new List<string>();
            var current = new StringBuilder();
            int depth = 0;

            foreach (char c in content)
            {
                if (c == '(')
                {
                    depth++;
                }
                else if (c == ')')
                {
                    depth--;
                }
                else if (c == ',' && depth == 0)
                {
                    parts.Add(current.ToString().Trim());
                    current.Clear();

                    continue;
                }

                current.Append(c);
            }

            parts.Add(current.ToString().Trim());

            return depth == 0 ? parts.ToArray() : null;
        }

        protected static string CallBody(string value, string name)
        {
            if (value == null
                || value.Length <= name.Length + 2
                || !value.EndsWith(")")
                || !value.StartsWith(name + "("))
            {
                return null;
            }

            return value.Substring(name.Length + 1, value.Length - name.Length - 2);
        }

        protected static bool IsImageName(string value)
        {
            int dot = value.LastIndexOf('.');

            if (dot <= 0 || dot >= value.Length - 1)
            {
                return false;
            }

            for (int index = dot + 1; index < value.Length; index++)
            {
                if (!char.IsLetter(value[index]))
                {
                    return false;
                }
            }

            return true;
        }
    }
}
