using System.Linq;
using System.Text.RegularExpressions;

namespace Ixen.Core.Visual.Styles
{
    public class MarginStyle : Style
    {
        private static Regex _regex = new Regex(@"([^ \t]+){1,4}");

        public SizeStyle Top { get; set; }
        public SizeStyle Right { get; set; }
        public SizeStyle Bottom { get; set; }
        public SizeStyle Left { get; set; }

        public MarginStyle()
            : base()
        {
            Top    = new SizeStyle { Value = 0, Unit = SizeUnit.Pixels };
            Right  = new SizeStyle { Value = 0, Unit = SizeUnit.Pixels };
            Bottom = new SizeStyle { Value = 0, Unit = SizeUnit.Pixels };
            Left   = new SizeStyle { Value = 0, Unit = SizeUnit.Pixels };
        }

        public MarginStyle(string content)
            : base(content)
        {}

        protected override bool Parse()
        {
            MatchCollection mc = _regex.Matches(_content);

            if (mc.Count < 1 || mc.Count > 4 || mc.Cast<Match>().Any(m => m.Success == false))
            {
                return false;
            }

            switch (mc.Count)
            {
                case 1:
                    Top    = new SizeStyle(mc[0].Value);
                    Right  = new SizeStyle(mc[0].Value);
                    Bottom = new SizeStyle(mc[0].Value);
                    Left   = new SizeStyle(mc[0].Value);
                    return Top.IsValid;

                case 2:
                    Top    = new SizeStyle(mc[0].Value);
                    Bottom = new SizeStyle(mc[0].Value);
                    Right  = new SizeStyle(mc[1].Value);
                    Left   = new SizeStyle(mc[1].Value);
                    return Top.IsValid && Right.IsValid;

                case 3:
                    Top    = new SizeStyle(mc[0].Value);
                    Right  = new SizeStyle(mc[1].Value);
                    Left   = new SizeStyle(mc[1].Value);
                    Bottom = new SizeStyle(mc[2].Value);
                    return Top.IsValid && Right.IsValid && Bottom.IsValid;

                case 4:
                    Top    = new SizeStyle(mc[0].Value);
                    Right  = new SizeStyle(mc[1].Value);
                    Bottom = new SizeStyle(mc[2].Value);
                    Left   = new SizeStyle(mc[3].Value);
                    return Top.IsValid && Right.IsValid && Bottom.IsValid && Left.IsValid;

                default:
                    return false;
            }
        }
    }
}
