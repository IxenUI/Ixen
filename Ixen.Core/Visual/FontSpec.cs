using Ixen.Core.Visual.Styles.Descriptors;

namespace Ixen.Core.Visual
{
    internal readonly struct FontSpec
    {
        internal readonly string Family;
        internal readonly float Size;
        internal readonly bool Bold;
        internal readonly bool Italic;

        internal FontSpec(string family, float size, bool bold, bool italic)
        {
            Family = family;
            Size = size;
            Bold = bold;
            Italic = italic;
        }

        internal static FontSpec From(VisualElementStylesHandlers handlers)
            => new FontSpec(
                handlers.FontFamily.Descriptor.Value,
                handlers.FontSize.Descriptor.Value,
                handlers.FontWeight.Descriptor.Value == FontWeight.Bold,
                handlers.FontStyle.Descriptor.Value == FontStyle.Italic);
    }
}
