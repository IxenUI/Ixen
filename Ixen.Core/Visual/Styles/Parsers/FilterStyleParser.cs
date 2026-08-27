using Ixen.Core.Visual.Styles.Descriptors;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace Ixen.Core.Visual.Styles.Parsers
{
    internal class FilterStyleParser : StyleParser
    {
        internal const string NONE = "none";
        internal const string BLUR = "blur";

        private static Regex _length = new Regex(@"^([0-9]+(?:\.[0-9]+)?)(px|)$");

        public FilterStyleDescriptor Descriptor { get; } = new();

        public FilterStyleParser(string content)
            : base(content)
        { }

        protected override bool Parse()
        {
            string content = _content?.Trim();

            if (string.IsNullOrEmpty(content))
            {
                return false;
            }

            if (content.ToLower() == NONE)
            {
                return true;
            }

            List<string> calls = Split(content);

            if (calls == null || calls.Count == 0)
            {
                return false;
            }

            foreach (string call in calls)
            {
                if (!ParseCall(call))
                {
                    return false;
                }
            }

            return true;
        }

        private bool ParseCall(string call)
        {
            int open = call.IndexOf('(');

            if (open <= 0 || !call.EndsWith(")"))
            {
                return false;
            }

            string name = call.Substring(0, open).ToLower();
            string body = call.Substring(open + 1, call.Length - open - 2).Trim();

            if (name != BLUR)
            {
                return false;
            }

            Match match = _length.Match(body);

            if (!match.Success || !float.TryParse(match.Groups[1].Value, NumberStyles.Float,
                CultureInfo.InvariantCulture, out float value))
            {
                return false;
            }

            Descriptor.Operations.Add(new FilterOperation
            {
                Kind = FilterKind.Blur,
                Value = value
            });

            return true;
        }

        private static List<string> Split(string content)
        {
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

                    if (depth < 0)
                    {
                        return null;
                    }
                }

                if (depth == 0 && (c == ' ' || c == '\t'))
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

            if (depth != 0)
            {
                return null;
            }

            if (current.Length > 0)
            {
                parts.Add(current.ToString());
            }

            return parts;
        }
    }
}
