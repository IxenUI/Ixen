using System;

namespace Ixen.Core.Visual.Styles.Descriptors
{
    [Flags]
    public enum TextDecorations
    {
        None = 0,
        Underline = 1,
        LineThrough = 2,
        Overline = 4
    }

    public class TextDecorationStyleDescriptor : StyleDescriptor
    {
        internal override string Identifier => StyleIdentifier.TEXT_DECORATION;

        public TextDecorations Value { get; set; } = TextDecorations.None;
        public bool IsDeclared { get; set; }

        internal bool Has(TextDecorations decoration) => (Value & decoration) == decoration;

        internal override bool CanGenerateSource => true;
        internal override string ToSource()
            => $"new {nameof(TextDecorationStyleDescriptor)} " +
                "{ " +
                    $"{nameof(Value)} = ({nameof(TextDecorations)}){(int)Value}, " +
                    $"{nameof(IsDeclared)} = {SourceOf(IsDeclared)} " +
                "}";
    }
}
