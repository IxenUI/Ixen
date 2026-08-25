using Ixen.Core.Visual.Styles.Descriptors;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace Ixen.Core.Visual.Styles.Parsers
{
    internal class TransformStyleParser : StyleParser
    {
        internal const string NONE = "none";

        internal const string TRANSLATE = "translate";
        internal const string TRANSLATE_X = "translatex";
        internal const string TRANSLATE_Y = "translatey";
        internal const string SCALE = "scale";
        internal const string SCALE_X = "scalex";
        internal const string SCALE_Y = "scaley";
        internal const string ROTATE = "rotate";
        internal const string SKEW = "skew";
        internal const string SKEW_X = "skewx";
        internal const string SKEW_Y = "skewy";

        private static Regex _length = new Regex(@"^(-?[0-9]+(?:\.[0-9]+)?)(px|%|)$");
        private static Regex _factor = new Regex(@"^(-?[0-9]+(?:\.[0-9]+)?)$");
        private static Regex _angle = new Regex(@"^(-?[0-9]+(?:\.[0-9]+)?)deg$");

        public TransformStyleDescriptor Descriptor { get; } = new();

        public TransformStyleParser(string content)
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
            string body = call.Substring(open + 1, call.Length - open - 2);

            string[] arguments = body.Split(Blanks, StringSplitOptions.RemoveEmptyEntries);

            if (arguments.Length == 0)
            {
                return false;
            }

            switch (name)
            {
                case TRANSLATE:
                    return Translate(arguments, true, true);

                case TRANSLATE_X:
                    return Translate(arguments, true, false);

                case TRANSLATE_Y:
                    return Translate(arguments, false, true);

                case SCALE:
                    return Scale(arguments, true, true);

                case SCALE_X:
                    return Scale(arguments, true, false);

                case SCALE_Y:
                    return Scale(arguments, false, true);

                case ROTATE:
                    return Rotate(arguments);

                case SKEW:
                    return Skew(arguments, true, true);

                case SKEW_X:
                    return Skew(arguments, true, false);

                case SKEW_Y:
                    return Skew(arguments, false, true);

                default:
                    return false;
            }
        }

        private bool Translate(string[] arguments, bool horizontal, bool vertical)
        {
            if (arguments.Length > (horizontal && vertical ? 2 : 1))
            {
                return false;
            }

            if (!Length(arguments[0], out SizeUnit unit, out float value))
            {
                return false;
            }

            var operation = new TransformOperation { Kind = TransformKind.Translate };

            if (horizontal)
            {
                operation.XUnit = unit;
                operation.X = value;
            }
            else
            {
                operation.YUnit = unit;
                operation.Y = value;
            }

            if (arguments.Length == 2)
            {
                if (!Length(arguments[1], out SizeUnit otherUnit, out float other))
                {
                    return false;
                }

                operation.YUnit = otherUnit;
                operation.Y = other;
            }

            Descriptor.Operations.Add(operation);
            return true;
        }

        private bool Scale(string[] arguments, bool horizontal, bool vertical)
        {
            if (arguments.Length > (horizontal && vertical ? 2 : 1))
            {
                return false;
            }

            if (!Number(_factor, arguments[0], out float value))
            {
                return false;
            }

            var operation = new TransformOperation
            {
                Kind = TransformKind.Scale,
                X = horizontal ? value : 1,
                Y = vertical ? value : 1
            };

            if (arguments.Length == 2)
            {
                if (!Number(_factor, arguments[1], out float other))
                {
                    return false;
                }

                operation.Y = other;
            }

            Descriptor.Operations.Add(operation);
            return true;
        }

        private bool Rotate(string[] arguments)
        {
            if (arguments.Length != 1 || !Number(_angle, arguments[0], out float degrees))
            {
                return false;
            }

            Descriptor.Operations.Add(new TransformOperation
            {
                Kind = TransformKind.Rotate,
                X = degrees
            });

            return true;
        }

        private bool Skew(string[] arguments, bool horizontal, bool vertical)
        {
            if (arguments.Length > (horizontal && vertical ? 2 : 1)
                || !Number(_angle, arguments[0], out float value))
            {
                return false;
            }

            var operation = new TransformOperation { Kind = TransformKind.Skew };

            if (horizontal)
            {
                operation.X = value;
            }
            else
            {
                operation.Y = value;
            }

            if (arguments.Length == 2)
            {
                if (!Number(_angle, arguments[1], out float other))
                {
                    return false;
                }

                operation.Y = other;
            }

            Descriptor.Operations.Add(operation);
            return true;
        }

        private static bool Length(string argument, out SizeUnit unit, out float value)
        {
            unit = SizeUnit.Pixels;
            value = 0;

            Match match = _length.Match(argument);

            if (!match.Success || !float.TryParse(match.Groups[1].Value, NumberStyles.Float,
                CultureInfo.InvariantCulture, out value))
            {
                return false;
            }

            if (match.Groups[2].Value == "%")
            {
                unit = SizeUnit.Percents;
            }

            return true;
        }

        private static bool Number(Regex regex, string argument, out float value)
        {
            value = 0;

            Match match = regex.Match(argument);

            return match.Success && float.TryParse(match.Groups[1].Value, NumberStyles.Float,
                CultureInfo.InvariantCulture, out value);
        }

        internal static readonly char[] Blanks = new[] { ' ', '\t' };

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
