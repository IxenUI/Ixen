using Ixen.Core.Visual.Styles.Descriptors;
using System.Linq;
using System.Text.RegularExpressions;

namespace Ixen.Core.Visual.Styles.Parsers
{
    internal class MaskStyleParser : StyleParser
    {
        private static Regex _regex = new Regex(@"([^ \t]+){1,4}");
        public MaskStyleDescriptor Descriptor { get; } = new MaskStyleDescriptor();

        public MaskStyleParser(string content)
            : base(content)
        { }

        protected override bool Parse()
        {
            MatchCollection mc = _regex.Matches(_content);

            if (mc.Count < 1 || mc.Count > 4 || mc.Cast<Match>().Any(m => m.Success == false))
            {
                return false;
            }

            BooleanStyleParser topParser, bottomParser, rightParser, leftParser;

            switch (mc.Count)
            {
                case 1:
                    topParser = new BooleanStyleParser(mc[0].Value);

                    Descriptor.Top = topParser.Descriptor.Value;
                    Descriptor.Right = topParser.Descriptor.Value;
                    Descriptor.Bottom = topParser.Descriptor.Value;
                    Descriptor.Left = topParser.Descriptor.Value;

                    return topParser.IsValid;

                case 2:
                    topParser = new BooleanStyleParser(mc[0].Value);
                    rightParser = new BooleanStyleParser(mc[1].Value);

                    Descriptor.Top = topParser.Descriptor.Value;
                    Descriptor.Bottom = topParser.Descriptor.Value;
                    Descriptor.Right = rightParser.Descriptor.Value;
                    Descriptor.Left = rightParser.Descriptor.Value;

                    return topParser.IsValid && rightParser.IsValid;

                case 3:
                    topParser = new BooleanStyleParser(mc[0].Value);
                    rightParser = new BooleanStyleParser(mc[1].Value);
                    bottomParser = new BooleanStyleParser(mc[2].Value);

                    Descriptor.Top = topParser.Descriptor.Value;
                    Descriptor.Right = rightParser.Descriptor.Value;
                    Descriptor.Left = rightParser.Descriptor.Value;
                    Descriptor.Bottom = bottomParser.Descriptor.Value;

                    return topParser.IsValid && rightParser.IsValid && bottomParser.IsValid;

                case 4:
                    topParser = new BooleanStyleParser(mc[0].Value);
                    rightParser = new BooleanStyleParser(mc[1].Value);
                    bottomParser = new BooleanStyleParser(mc[2].Value);
                    leftParser = new BooleanStyleParser(mc[3].Value);

                    Descriptor.Top = topParser.Descriptor.Value;
                    Descriptor.Right = rightParser.Descriptor.Value;
                    Descriptor.Bottom = bottomParser.Descriptor.Value;
                    Descriptor.Left = leftParser.Descriptor.Value;

                    return topParser.IsValid && rightParser.IsValid && bottomParser.IsValid && leftParser.IsValid;

                default:
                    return false;
            }
        }
    }
}
