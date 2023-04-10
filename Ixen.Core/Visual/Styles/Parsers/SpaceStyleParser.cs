using Ixen.Core.Visual.Styles.Descriptors;
using System.Linq;
using System.Text.RegularExpressions;

namespace Ixen.Core.Visual.Styles.Parsers
{
    internal class SpaceStyleParser : StyleParser
    {
        private static Regex _regex = new Regex(@"([^ \t]+){1,4}");
        public SpaceStyleDescriptor Descriptor { get; } = new SpaceStyleDescriptor();

        public SpaceStyleParser(string content)
            : base(content)
        { }

        protected override bool Parse()
        {
            MatchCollection mc = _regex.Matches(_content);

            if (mc.Count < 1 || mc.Count > 4 || mc.Cast<Match>().Any(m => m.Success == false))
            {
                return false;
            }

            SizeStyleParser topParser, bottomParser, rightParser, leftParser;


            switch (mc.Count)
            {
                case 1:
                    topParser = new SizeStyleParser(mc[0].Value);

                    Descriptor.Top    = topParser.Descriptor;
                    Descriptor.Right  = topParser.Descriptor;
                    Descriptor.Bottom = topParser.Descriptor;
                    Descriptor.Left   = topParser.Descriptor;

                    return topParser.IsValid;

                case 2:
                    topParser   = new SizeStyleParser(mc[0].Value);
                    rightParser = new SizeStyleParser(mc[1].Value);

                    Descriptor.Top = topParser.Descriptor;
                    Descriptor.Bottom = topParser.Descriptor;
                    Descriptor.Right = rightParser.Descriptor;
                    Descriptor.Left = rightParser.Descriptor;

                    return topParser.IsValid && rightParser.IsValid;

                case 3:
                    topParser = new SizeStyleParser(mc[0].Value);
                    rightParser = new SizeStyleParser(mc[1].Value);
                    bottomParser = new SizeStyleParser(mc[2].Value);
                    
                    Descriptor.Top    = topParser.Descriptor;
                    Descriptor.Right  = rightParser.Descriptor;
                    Descriptor.Left   = rightParser.Descriptor;
                    Descriptor.Bottom = bottomParser.Descriptor;

                    return topParser.IsValid && rightParser.IsValid && bottomParser.IsValid;

                case 4:
                    topParser = new SizeStyleParser(mc[0].Value);
                    rightParser = new SizeStyleParser(mc[1].Value);
                    bottomParser = new SizeStyleParser(mc[2].Value);
                    leftParser = new SizeStyleParser(mc[3].Value);

                    Descriptor.Top = topParser.Descriptor;
                    Descriptor.Right  = rightParser.Descriptor;
                    Descriptor.Bottom = bottomParser.Descriptor;
                    Descriptor.Left   = leftParser.Descriptor;

                    return topParser.IsValid && rightParser.IsValid && bottomParser.IsValid && leftParser.IsValid;

                default:
                    return false;
            }
        }
    }
}
