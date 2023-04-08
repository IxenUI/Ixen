namespace Ixen.Core.Visual.Styles
{
    public enum LayoutType
    {
        Column,
        Row,
        Grid,
        Absolute,
        Fixed,
        Dock
    }

    public class LayoutStyle : Style
    {
        internal override string Identifier => StyleIdentifier.Layout;

        public LayoutType Type { get; set; } = LayoutType.Column;

        public LayoutStyle()
        {}

        public LayoutStyle(string content)
            : base(content)
        {}

        protected override bool Parse() => true;
    }
}
