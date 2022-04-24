using System.Text.RegularExpressions;

namespace Ixen.Core.Visual.Styles
{
    public enum SizeUnit
    {
        Undefined,
        Pixels,
        Percents,
        Weight
    }

    public class SizeStyle : Style
    {
        private static Regex _regex = new Regex(@"([0-9]+(?:\.[0-9]+)?)(px|%|\*)");
        public SizeUnit Unit { get; set; } = SizeUnit.Undefined;
        public float Value { get; set; } = 1;

        public SizeStyle()
        {}

        public SizeStyle(string content)
            : base(content)
        {}

        protected override void Parse()
        {
            Match m = _regex.Match(_content);

            if (!m.Success || m.Groups.Count < 2)
            {
                return;
            }

            if (float.TryParse(m.Groups[1].Value, out float floatValue))
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
                    case "*":
                        Unit = SizeUnit.Weight;
                        break;
                }
            }
        }
    }
}
