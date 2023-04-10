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
            switch (_content.ToLower())
            {
                case "row":
                    Descriptor.Type = LayoutType.Row;
                    break;
                case "column":
                    Descriptor.Type = LayoutType.Column;
                    break;
                case "grid":
                    Descriptor.Type = LayoutType.Grid;
                    break;
                case "absolute":
                    Descriptor.Type = LayoutType.Absolute;
                    break;
                case "fixed":
                    Descriptor.Type = LayoutType.Fixed;
                    break;
            }

            return true;
        }
    }
}
