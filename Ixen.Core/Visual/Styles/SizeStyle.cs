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
        private static Regex _regex = new Regex(@"([0-9]+(?:\.[0-9]+)?)(px|%|\*|)");
        public SizeUnit Unit { get; set; } = SizeUnit.Undefined;
        public float Value { get; set; } = 0;

        public SizeStyle() : base()
        {
            Value = 0;
            Unit = SizeUnit.Pixels;
        }

        public SizeStyle(string content)
            : base(content)
        {}

        protected override bool Parse()
        {
            Match m = _regex.Match(_content);

            if (!m.Success || m.Groups.Count < 2)
            {
                return false;
            }

            float floatValue;
            if (!float.TryParse(m.Groups[1].Value, out floatValue))
            {
                return false;
            }

            Value = floatValue;
            switch(m.Groups[2].Value)
            {
                case "px":
                case "":
                    Unit = SizeUnit.Pixels;
                    return true;

                case "%":
                    Unit = SizeUnit.Percents;
                    return true;

                case "*":
                    Unit = SizeUnit.Weight;
                    return true;

                default:
                    return false;
            }
        }
    }
}
