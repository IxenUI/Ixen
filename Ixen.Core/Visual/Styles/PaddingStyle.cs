namespace Ixen.Core.Visual.Styles
{
    public class PaddingStyle : MarginStyle
    {
        internal override string Identifier => StyleIdentifier.Padding;

        public PaddingStyle()
            : base()
        { }

        public PaddingStyle(string content)
            : base(content)
        { }
    }
}
