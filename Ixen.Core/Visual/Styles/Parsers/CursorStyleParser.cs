using Ixen.Core.Visual.Styles.Descriptors;

namespace Ixen.Core.Visual.Styles.Parsers
{
    internal class CursorStyleParser : StyleParser
    {
        public CursorStyleDescriptor Descriptor { get; } = new();

        public CursorStyleParser(string content)
            : base(content)
        { }

        protected override bool Parse()
        {
            switch (_content?.Trim().ToLower())
            {
                case "default":
                case "arrow":
                    Descriptor.Value = CursorKind.Default;
                    return true;

                case "hand":
                case "pointer":
                    Descriptor.Value = CursorKind.Hand;
                    return true;

                case "text":
                case "caret":
                    Descriptor.Value = CursorKind.Text;
                    return true;

                case "wait":
                    Descriptor.Value = CursorKind.Wait;
                    return true;

                case "crosshair":
                    Descriptor.Value = CursorKind.Crosshair;
                    return true;

                case "ew-resize":
                    Descriptor.Value = CursorKind.ResizeHorizontal;
                    return true;

                case "ns-resize":
                    Descriptor.Value = CursorKind.ResizeVertical;
                    return true;

                default:
                    return false;
            }
        }
    }
}
