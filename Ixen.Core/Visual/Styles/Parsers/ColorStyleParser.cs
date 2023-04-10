using Ixen.Core.Visual.Styles.Descriptors;
using System.Text.RegularExpressions;

namespace Ixen.Core.Visual.Styles.Parsers
{
    internal class ColorStyleParser : StyleParser
    {
        private static Regex _regex = new Regex(@"(#(?:[0-9A-F]{6}|[0-9A-F]{8}))");
        public ColorStyleDescriptor Descriptor { get; } = new ColorStyleDescriptor();

        public ColorStyleParser(string content)
            : base(content)
        { }

        protected override bool Parse()
        {
            Match m = _regex.Match(_content);

            if (!m.Success)
            {
                return false;
            }

            Descriptor.Value = m.Groups[1].Value;

            return true;
        }
    }
}
