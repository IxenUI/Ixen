using Ixen.Core.Visual.Styles.Descriptors;
using System.Globalization;

namespace Ixen.Core.Visual.Styles.Parsers
{
    internal class ZIndexStyleParser : StyleParser
    {
        public ZIndexStyleDescriptor Descriptor { get; } = new();

        public ZIndexStyleParser(string content)
            : base(content)
        { }

        protected override bool Parse()
        {
            if (!int.TryParse(_content?.Trim(), NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out int value))
            {
                return false;
            }

            Descriptor.Value = value;
            return true;
        }
    }
}
