using Ixen.Core.Visual.Styles.Descriptors;

namespace Ixen.Core.Visual.Styles.Parsers
{
    internal class TextShadowStyleParser : ShadowStyleParser
    {
        protected override int MaxLengths => 3;

        protected override bool AllowsInset => false;

        public new TextShadowStyleDescriptor Descriptor { get; } = new();

        public TextShadowStyleParser(string content)
            : base(content)
        { }

        protected override bool Parse()
        {
            if (!base.Parse())
            {
                return false;
            }

            Descriptor.Set(base.Descriptor);
            return true;
        }
    }
}
