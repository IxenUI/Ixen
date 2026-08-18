using Ixen.Core.Visual.Styles.Descriptors;

namespace Ixen.Core.Visual.Styles.Parsers
{
    internal class ObjectFitStyleParser : StyleParser
    {
        public ObjectFitStyleDescriptor Descriptor { get; } = new();

        public ObjectFitStyleParser(string content)
            : base(content)
        { }

        protected override bool Parse()
        {
            switch (_content?.Trim().ToLower())
            {
                case "fill":
                case "stretch":
                    Descriptor.Value = ObjectFit.Fill;
                    return true;

                case "contain":
                    Descriptor.Value = ObjectFit.Contain;
                    return true;

                case "cover":
                    Descriptor.Value = ObjectFit.Cover;
                    return true;

                case "none":
                    Descriptor.Value = ObjectFit.None;
                    return true;

                case "scale-down":
                    Descriptor.Value = ObjectFit.ScaleDown;
                    return true;

                default:
                    return false;
            }
        }
    }
}
