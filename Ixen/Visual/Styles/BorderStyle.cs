using Ixen.Core;

namespace Ixen.Visual.Styles
{
    public enum BorderType
    {
        Outer,
        Inner,
        Center
    }

    public class BorderStyle : Style
    {
        public Color Color { get; set; } = Color.Transparent;
        public float Thickness { get; set; } = 1;
        public BorderType Type { get; set; } = BorderType.Outer;

        public BorderStyle()
        {}

        public BorderStyle(string content)
            : base(content)
        {}

        public override void Parse()
        {
            
        }
    }
}
