using Ixen.Core.Visual.Styles.Descriptors;

namespace Ixen.Core.Visual.Styles.Parsers
{
    internal class BoxShadowStyleParser : ShadowStyleParser
    {
        public new BoxShadowStyleDescriptor Descriptor { get; } = new();

        public BoxShadowStyleParser(string content)
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
