using Ixen.Core.Visual.Styles.Descriptors;

namespace Ixen.Core.Visual.Styles
{
    public class BackgroundStyleDescriptor : StyleDescriptor
    {
        internal override string Identifier => StyleIdentifier.Background;

        public string Color { get; set; }
        public string ImageUrl { get; set; }
        public bool RepeatX { get; set; }
        public bool RepeatY { get; set; }
    }
}
