using Ixen.Core.Visual.Styles.Descriptors;

namespace Ixen.Core.Visual
{
    internal readonly struct FontSpec
    {
        internal readonly string Family;
        internal readonly float Size;
        internal readonly bool Bold;
        internal readonly bool Italic;
        internal readonly float LineHeight;
        internal readonly float LetterSpacing;

        internal FontSpec(string family, float size, bool bold, bool italic,
            float lineHeight = 0, float letterSpacing = 0)
        {
            Family = family;
            Size = size;
            Bold = bold;
            Italic = italic;
            LineHeight = lineHeight;
            LetterSpacing = letterSpacing;
        }

        internal float Advance(string text)
            => LetterSpacing == 0 || string.IsNullOrEmpty(text)
                ? 0
                : LetterSpacing * text.Length;

        internal static FontSpec From(VisualElementStylesHandlers handlers)
        {
            float size = handlers.FontSize.Descriptor.Value;

            if (size <= 0)
            {
                size = FontSizeStyleDescriptor.DEFAULT_SIZE;
            }

            return new FontSpec(
                handlers.FontFamily.Descriptor.Value,
                size,
                handlers.FontWeight.Descriptor.Value == FontWeight.Bold,
                handlers.FontStyle.Descriptor.Value == FontStyle.Italic,
                handlers.LineHeight.Descriptor.Resolve(size),
                handlers.LetterSpacing.Descriptor.Resolve());
        }
    }
}
