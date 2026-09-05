namespace Ixen.Core.Visual.Styles.Parsers
{
    internal class DropShadowStyleParser : ShadowStyleParser
    {
        protected override int MaxLengths => 3;

        protected override bool AllowsInset => false;

        public DropShadowStyleParser(string content)
            : base(content)
        { }
    }
}
