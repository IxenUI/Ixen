using Ixen.Core.Visual.Styles.Descriptors;
using System.Text.RegularExpressions;

namespace Ixen.Core.Visual.Styles.Parsers
{
    internal class SizeStyleParser : StyleParser
    {
        private static Regex _regex = new Regex(@"(([0-9]+(?:\.[0-9]+)?)(px|%|\*|)|\?)");
        public SizeStyleDescriptor Descriptor { get; } = new SizeStyleDescriptor();

        public SizeStyleParser(string content)
            : base(content)
        {}

        protected override bool Parse()
        {
            Match m = _regex.Match(_content);

            if (!m.Success || m.Groups.Count < 4)
            {
                return false;
            }

            if (m.Groups[1].Value == "?")
            {
                Descriptor.Unit = SizeUnit.Content;
                Descriptor.Value = 0;
                return true;
            }

            float floatValue;
            if (!float.TryParse(m.Groups[2].Value, out floatValue))
            {
                return false;
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
    }
}
