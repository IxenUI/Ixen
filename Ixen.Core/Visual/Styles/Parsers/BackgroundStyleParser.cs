using Ixen.Core.Visual.Styles.Descriptors;

namespace Ixen.Core.Visual.Styles.Parsers
{
    public class BackgroundStyleParser : StyleParser
    {
        public BackgroundStyleDescriptor Descriptor { get; } = new BackgroundStyleDescriptor();

        public BackgroundStyleParser(string content)
            : base(content)
        { }

        protected override bool Parse() => true;
    }
}
