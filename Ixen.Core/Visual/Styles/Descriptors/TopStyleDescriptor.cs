namespace Ixen.Core.Visual.Styles.Descriptors
{
    public class TopStyleDescriptor : OffsetStyleDescriptor
    {
        internal override string Identifier => StyleIdentifier.TOP;

        internal override bool CanGenerateSource => true;
        internal override string ToSource()
            => $"new {nameof(TopStyleDescriptor)} " +
                "{ " +
                    $"{nameof(Unit)} = {nameof(SizeUnit)}.{Unit}, " +
                    $"{nameof(Value)} = {SourceOf(Value)} " +
                "}";
    }
}
