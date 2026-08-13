using Ixen.Core.Visual.Styles.Descriptors;

namespace Ixen.Core.Visual.Styles.Parsers
{
    internal class DockStyleParser : StyleParser
    {
        public DockStyleDescriptor Descriptor { get; } = new();

        public DockStyleParser(string content)
            : base(content)
        { }

        protected override bool Parse()
        {
            switch (_content?.Trim().ToLower())
            {
                case "left":
                    Descriptor.Side = DockSide.Left;
                    return true;

                case "top":
                    Descriptor.Side = DockSide.Top;
                    return true;

                case "right":
                    Descriptor.Side = DockSide.Right;
                    return true;

                case "bottom":
                    Descriptor.Side = DockSide.Bottom;
                    return true;

                case "fill":
                    Descriptor.Side = DockSide.Fill;
                    return true;

                default:
                    return false;
            }
        }
    }
}
