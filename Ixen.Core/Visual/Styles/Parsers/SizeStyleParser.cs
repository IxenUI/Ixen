using Ixen.Core.Language.Xns;
using Ixen.Core.Visual.Styles.Descriptors;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace Ixen.Core.Visual.Styles.Parsers
{
    internal class SizeStyleParser : StyleParser
    {
        private static Regex _regex = new Regex(
            @"^\s*(?:(\?)|([0-9]+(?:\.[0-9]+)?)(px|%|\*|)(?:([+-][0-9]+(?:\.[0-9]+)?)px)?)\s*$");
        public SizeStyleDescriptor Descriptor { get; } = new SizeStyleDescriptor();

        public SizeStyleParser(string content)
            : base(content)
        {}

        protected override bool Parse()
        {
            if (TryFunction())
            {
                return true;
            }

            Match m = _regex.Match(_content);

            if (!m.Success)
            {
                return false;
            }

            if (m.Groups[1].Success)
            {
                Descriptor.Unit = SizeUnit.Content;
                Descriptor.Value = 0;
                return true;
            }

            if (!float.TryParse(m.Groups[2].Value, NumberStyles.Float, CultureInfo.InvariantCulture, out float floatValue))
            {
                return false;
            }

            if (m.Groups[4].Success)
            {
                if (m.Groups[3].Value != "%"
                    || !float.TryParse(m.Groups[4].Value, NumberStyles.Float,
                        CultureInfo.InvariantCulture, out float offset))
                {
                    return false;
                }

                Descriptor.Offset = offset;
            }

            Descriptor.Value = floatValue;
            switch(m.Groups[3].Value)
            {
                case "px":
                    Descriptor.Unit = SizeUnit.Pixels;
                    return true;

                case "%":
                    Descriptor.Unit = SizeUnit.Percents;
                    return true;

                case "*":
                    Descriptor.Unit = SizeUnit.Weight;
                    return true;

                case "":
                    if (Descriptor.Value == 0)
                    {
                        Descriptor.Unit = SizeUnit.Pixels;
                        return true;
                    }
                    return false;

                default:
                    return false;
            }
        }

        private bool TryFunction()
        {
            string text = _content == null ? string.Empty : _content.Trim();
            int open = text.IndexOf('(');

            if (open <= 0 || text.Length == 0 || text[text.Length - 1] != ')')
            {
                return false;
            }

            SizeFunction function = FunctionOf(text.Substring(0, open).Trim());

            if (function == SizeFunction.None)
            {
                return false;
            }

            string[] arguments = Split(text.Substring(open + 1, text.Length - open - 2));

            if (function == SizeFunction.Clamp ? arguments.Length != 3 : arguments.Length == 0)
            {
                return false;
            }

            var parts = new List<SizePart>();

            foreach (string argument in arguments)
            {
                if (!XnsCalc.TryLinear(argument, out float percents, out float pixels, out _))
                {
                    return false;
                }

                if (percents == 0 && pixels < 0)
                {
                    return false;
                }

                parts.Add(new SizePart { Value = percents, Offset = pixels });
            }

            Descriptor.Function = function;
            Descriptor.Parts = parts;
            Descriptor.Unit = SizeUnit.Percents;
            Descriptor.Value = 0;
            Descriptor.Offset = 0;

            return true;
        }

        private static SizeFunction FunctionOf(string name)
        {
            switch (name)
            {
                case "min": return SizeFunction.Min;
                case "max": return SizeFunction.Max;
                case "clamp": return SizeFunction.Clamp;
                default: return SizeFunction.None;
            }
        }

        private static string[] Split(string inner)
        {
            var arguments = new List<string>();
            var current = new StringBuilder();
            int depth = 0;

            foreach (char c in inner)
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
                    arguments.Add(current.ToString());
                    current.Clear();
                    continue;
                }

                current.Append(c);
            }

            arguments.Add(current.ToString());

            return arguments.ToArray();
        }
    }
}
