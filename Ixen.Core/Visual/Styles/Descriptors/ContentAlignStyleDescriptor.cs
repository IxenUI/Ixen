namespace Ixen.Core.Visual.Styles.Descriptors
{
    public enum ContentAlign
    {
        Unset,
        Left,
        Center,
        Right
    }

    public enum ContentVAlign
    {
        Unset,
        Top,
        Middle,
        Bottom
    }

    public class ContentAlignStyleDescriptor : StyleDescriptor
    {
        internal override string Identifier => StyleIdentifier.CONTENT_ALIGN;

        public ContentAlign Horizontal { get; set; } = ContentAlign.Unset;
        public ContentVAlign Vertical { get; set; } = ContentVAlign.Unset;

        internal bool IsDeclared
            => Horizontal != ContentAlign.Unset || Vertical != ContentVAlign.Unset;

        internal override bool CanGenerateSource => true;
        internal override string ToSource()
            => $"new {nameof(ContentAlignStyleDescriptor)} " +
                "{ " +
                    $"{nameof(Horizontal)} = {nameof(ContentAlign)}.{Horizontal}, " +
                    $"{nameof(Vertical)} = {nameof(ContentVAlign)}.{Vertical} " +
                "}";
    }
}
