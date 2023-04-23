using Ixen.Core.Visual.Styles.Descriptors;
using System.Linq;
using System.Text.RegularExpressions;

namespace Ixen.Core.Visual.Styles.Parsers
{
    internal class SizeTemplateStyleParser : StyleParser
    {
        private static Regex _regex = new Regex(@"([^ \t]+)+");
        public SizeTemplateStyleDescriptor Descriptor { get; } = new();

        public SizeTemplateStyleParser(string content)
            : base(content)
        { }

        protected override bool Parse()
        {
            MatchCollection mc = _regex.Matches(_content);

            if (mc.Count < 1 || mc.Cast<Match>().Any(m => m.Success == false))
            {
                return false;
            }

            SizeStyleParser sizeParser;
            bool valid = true;
            foreach (Match m in mc) {
                sizeParser = new SizeStyleParser(m.Value);

                if (!sizeParser.IsValid)
                {
                    valid = false;
                    break;
                }

                Descriptor.Value.Add(sizeParser.Descriptor);
            }

            return valid;
        }
    }
}
