using System;

namespace Ixen.Core.Visual.Styles.Descriptors
{
    public class BackgroundStyleDescriptor : StyleDescriptor
    {
        internal override string Identifier => StyleIdentifier.Background;

        public string Color { get; set; } = "#000000";
        public string ImageUrl { get; set; }
        public bool RepeatX { get; set; } = false;
        public bool RepeatY { get; set; } = false;


        internal override bool CanGenerateSource => true;
        internal override string ToSource()
            => $"new {nameof(BackgroundStyleDescriptor)} " +
                "{ " +
                    (string.IsNullOrWhiteSpace(Color) ? "" : $"{nameof(Color)} = \"{Color}\", ") +
                    (string.IsNullOrWhiteSpace(ImageUrl) ? "" : $"{nameof(ImageUrl)} = \"{ImageUrl}\", ") +
                    $"{nameof(RepeatX)} = {(RepeatX ? "true" : "false")}, " +
                    $"{nameof(RepeatY)} = {(RepeatY ? "true" : "false")} " +
                "}";
    }
}
