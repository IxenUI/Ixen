using Ixen.Core.Visual.Styles.Descriptors;

namespace Ixen.Core.Visual
{
    internal readonly struct FontSpec
    {
        internal readonly string Family;
        internal readonly float Size;
        internal readonly int Weight;
        internal readonly bool Italic;
        internal readonly float LineHeight;
        internal readonly float LetterSpacing;
        internal readonly float WordSpacing;

        internal FontSpec(string family, float size, bool bold, bool italic,
            float lineHeight = 0, float letterSpacing = 0)
            : this(family, size, bold ? (int)FontWeight.Bold : (int)FontWeight.Normal, italic,
                lineHeight, letterSpacing)
        {
        }

        internal FontSpec(string family, float size, int weight, bool italic,
            float lineHeight = 0, float letterSpacing = 0, float wordSpacing = 0)
        {
            Family = family;
            Size = size;
            Weight = weight;
            Italic = italic;
            LineHeight = lineHeight;
            LetterSpacing = letterSpacing;
            WordSpacing = wordSpacing;
        }

        internal FontSpec WithWordSpacing(float wordSpacing)
            => new FontSpec(Family, Size, Weight, Italic, LineHeight, LetterSpacing, wordSpacing);

        internal static int Spaces(string text)
        {
            int count = 0;

            for (int index = 0; index < text.Length; index++)
            {
                if (text[index] == ' ')
                {
                    count++;
                }
            }

            return count;
        }

        internal bool Bold => Weight >= (int)FontWeight.SemiBold;

        internal bool SameAs(FontSpec other)
        {
            return Size == other.Size
                && Weight == other.Weight
                && Italic == other.Italic
                && LineHeight == other.LineHeight
                && LetterSpacing == other.LetterSpacing
                && WordSpacing == other.WordSpacing
                && string.Equals(Family, other.Family);
        }

        internal float Advance(string text)
        {
            if (string.IsNullOrEmpty(text) || (LetterSpacing == 0 && WordSpacing == 0))
            {
                return 0;
            }

            return LetterSpacing * text.Length + (WordSpacing == 0 ? 0 : WordSpacing * Spaces(text));
        }

        internal static FontSpec From(VisualElementStylesHandlers handlers, float scale = 1)
        {
            float size = handlers.FontSize.Descriptor.Value;

            if (size <= 0)
            {
                size = FontSizeStyleDescriptor.DEFAULT_SIZE;
            }

            if (scale > 0 && scale != 1)
            {
                size *= scale;
            }

            return new FontSpec(
                handlers.FontFamily.Descriptor.Value,
                size,
                handlers.FontWeight.Descriptor.Value == FontWeight.Unset
                    ? (int)FontWeight.Normal
                    : (int)handlers.FontWeight.Descriptor.Value,
                handlers.FontStyle.Descriptor.Value == FontStyle.Italic,
                handlers.LineHeight.Descriptor.Resolve(size),
                handlers.LetterSpacing.Descriptor.Resolve());
        }
    }
}
