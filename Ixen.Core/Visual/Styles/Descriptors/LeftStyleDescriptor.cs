namespace Ixen.Core.Visual.Styles.Descriptors
{
    public class LeftStyleDescriptor : OffsetStyleDescriptor
    {
        internal override string Identifier => StyleIdentifier.LEFT;

        internal override bool CanGenerateSource => true;
        internal override string ToSource()
            => $"new {nameof(LeftStyleDescriptor)} " +
                "{ " +
                    $"{nameof(Unit)} = {nameof(SizeUnit)}.{Unit}, " +
                    $"{nameof(Value)} = {SourceOf(Value)} " +
                "}";
    }
}
