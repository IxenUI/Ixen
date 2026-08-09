using Ixen.Core.Visual.Styles.Descriptors;

namespace Ixen.Core.Visual.Styles.Parsers
{
    internal class LayoutStyleParser : StyleParser
    {
        public LayoutStyleDescriptor Descriptor { get; } = new LayoutStyleDescriptor();

        public LayoutStyleParser(string content)
            : base(content)
        { }

        protected override bool Parse()
        {
            switch (_content?.Trim().ToLower())
            {
                case "row":
                    Descriptor.Type = LayoutType.Row;
                    return true;
                case "column":
                    Descriptor.Type = LayoutType.Column;
                    return true;
                case "grid":
                    Descriptor.Type = LayoutType.Grid;
                    return true;
                case "absolute":
                    Descriptor.Type = LayoutType.Absolute;
                    return true;
                case "fixed":
                    Descriptor.Type = LayoutType.Fixed;
                    return true;
                case "dock":
                    Descriptor.Type = LayoutType.Dock;
                    return true;
                default:
                    return false;
            }
        }
    }
}
