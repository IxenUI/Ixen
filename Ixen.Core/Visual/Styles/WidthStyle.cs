namespace Ixen.Core.Visual.Styles
{
    public class WidthStyle : SizeStyle
    {
        internal override string Identifier => StyleIdentifier.Width;

        public WidthStyle()
            : base()
        { }

        public WidthStyle(string content)
            : base(content)
        { }
    }
}
