using Ixen.Core.Visual.Styles.Descriptors;
using System.Text.RegularExpressions;

namespace Ixen.Core.Visual.Styles.Parsers
{
    internal class AnchorPlacementStyleParser : StyleParser
    {
        private static Regex _splitter = new Regex(@"[^ \t]+");

        public AnchorPlacementStyleDescriptor Descriptor { get; } = new();

        public AnchorPlacementStyleParser(string content)
            : base(content)
        { }

        protected override bool Parse()
        {
            MatchCollection parts = _splitter.Matches(_content ?? string.Empty);

            if (parts.Count < 1 || parts.Count > 3)
            {
                return false;
            }

            bool hasSide = false;
            bool hasAlign = false;
            bool hasFlag = false;

            foreach (Match part in parts)
            {
                switch (part.Value.ToLower())
                {
                    case "below":
                        if (hasSide) { return false; }
                        Descriptor.Side = AnchorSide.Below;
                        hasSide = true;
                        break;

                    case "above":
                        if (hasSide) { return false; }
                        Descriptor.Side = AnchorSide.Above;
                        hasSide = true;
                        break;

                    case "left":
                        if (hasSide) { return false; }
                        Descriptor.Side = AnchorSide.Left;
                        hasSide = true;
                        break;

                    case "right":
                        if (hasSide) { return false; }
                        Descriptor.Side = AnchorSide.Right;
                        hasSide = true;
                        break;

                    case "start":
                        if (hasAlign) { return false; }
                        Descriptor.Align = AnchorAlign.Start;
                        hasAlign = true;
                        break;

                    case "center":
                        if (hasAlign) { return false; }
                        Descriptor.Align = AnchorAlign.Center;
                        hasAlign = true;
                        break;

                    case "end":
                        if (hasAlign) { return false; }
                        Descriptor.Align = AnchorAlign.End;
                        hasAlign = true;
                        break;

                    case "noflip":
                        if (hasFlag) { return false; }
                        Descriptor.NoFlip = true;
                        hasFlag = true;
                        break;

                    default:
                        return false;
                }
            }

            return true;
        }
    }
}
