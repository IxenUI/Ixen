using Ixen.Core.Visual.Styles.Descriptors;
using System.Text.RegularExpressions;

namespace Ixen.Core.Visual.Styles.Parsers
{
    internal class AnchorStyleParser : StyleParser
    {
        private static Regex _name = new Regex(@"^[a-zA-Z_][a-zA-Z0-9_-]*$");

        public AnchorStyleDescriptor Descriptor { get; } = new();

        public AnchorStyleParser(string content)
            : base(content)
        { }

        protected override bool Parse()
        {
            string name = _content?.Trim();

            if (string.IsNullOrEmpty(name) || !_name.IsMatch(name))
            {
                return false;
            }

            Descriptor.Name = name;
            return true;
        }
    }
}
