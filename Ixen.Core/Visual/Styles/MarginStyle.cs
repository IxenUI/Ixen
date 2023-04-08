namespace Ixen.Core.Visual.Styles
{
    public class MarginStyle : SpaceStyle
    {
        internal override string Identifier => StyleIdentifier.Margin;

        public MarginStyle()
            : base()
        { }

        public MarginStyle(string content)
            : base(content)
        { }
    }
}
