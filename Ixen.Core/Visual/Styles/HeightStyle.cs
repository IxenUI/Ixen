namespace Ixen.Core.Visual.Styles
{
    public class HeightStyle : SizeStyle
    {
        internal override string Identifier => StyleIdentifier.Height;

        public HeightStyle()
            : base()
        { }

        public HeightStyle(string content)
            : base(content)
        { }
    }
}
