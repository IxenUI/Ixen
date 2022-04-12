using System.Text.RegularExpressions;

namespace Ixen.Visual.Styles
{
    public enum SizeUnit
    {
        Pixels,
        Percents
    }

    public abstract class SizeStyle : Style
    {
        private Regex _regex = new Regex(@"([0-9]+(?:\.[0-9]+)?)(px|%)");
        public SizeUnit Unit { get; set; } = SizeUnit.Pixels;
        public float Value { get; set; } = 1;

        public SizeStyle()
        {}

        public SizeStyle(string content)
            : base(content)
        {}

        public override void Parse()
        {
            Match m = _regex.Match(_content);

            if (!m.Success || m.Groups.Count < 2)
            {
                return;
            }

            if(float.TryParse(m.Groups[1].Value, out float floatValue))
            {
                Value = floatValue;
                switch(m.Groups[2].Value)
                {
                    case "px":
                        Unit = SizeUnit.Pixels;
                        break;
                    case "%":
                        Unit = SizeUnit.Percents;
                        break;
                }
            }
        }
    }
}
