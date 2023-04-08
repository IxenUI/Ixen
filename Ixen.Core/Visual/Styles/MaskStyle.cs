namespace Ixen.Core.Visual.Styles
{
    public class MaskStyle : Style
    {
        internal override string Identifier => StyleIdentifier.Mask;

        public bool Top { get; set; } = false;
        public bool Bottom { get; set; } = false;
        public bool Right { get; set; } = false;
        public bool Left { get; set; } = false;

        public MaskStyle()
            : base()
        {}

        public MaskStyle(string content)
            : base(content)
        {}

        protected override bool Parse() => true;
    }
}
