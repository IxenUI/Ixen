using Ixen.Core.Visual.Styles.Descriptors;
using System.Text.RegularExpressions;

namespace Ixen.Core.Visual.Styles.Parsers
{
    internal class ColorStyleParser : StyleParser
    {
        private static Regex _regex = new Regex(@"^\s*(#(?:[0-9A-Fa-f]{8}|[0-9A-Fa-f]{6}))\s*$");
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
