namespace Ixen.Core.Visual.Styles.Descriptors
{
    public class BottomStyleDescriptor : OffsetStyleDescriptor
    {
        internal override string Identifier => StyleIdentifier.BOTTOM;

        internal override bool CanGenerateSource => true;
        internal override string ToSource()
            => $"new {nameof(BottomStyleDescriptor)} " +
                "{ " +
                    Fields() +
                "}";
    }
}
