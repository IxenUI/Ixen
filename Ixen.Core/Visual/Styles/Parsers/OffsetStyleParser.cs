using Ixen.Core.Visual.Styles.Descriptors;

namespace Ixen.Core.Visual.Styles.Parsers
{
    internal class OffsetStyleParser : SizeStyleParser
    {
        public OffsetStyleParser(string content)
            : base(content)
        { }

        protected override bool Parse()
        {
            if (!base.Parse())
            {
                return false;
            }

            return base.Descriptor.Unit == SizeUnit.Pixels
                || base.Descriptor.Unit == SizeUnit.Percents;
        }
    }
}
