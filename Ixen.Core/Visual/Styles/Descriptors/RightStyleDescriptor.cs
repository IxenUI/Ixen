namespace Ixen.Core.Visual.Styles.Descriptors
{
    public class RightStyleDescriptor : OffsetStyleDescriptor
    {
        internal override string Identifier => StyleIdentifier.RIGHT;

        internal override bool CanGenerateSource => true;
        internal override string ToSource()
            => $"new {nameof(RightStyleDescriptor)} " +
                "{ " +
                    $"{nameof(Unit)} = {nameof(SizeUnit)}.{Unit}, " +
                    $"{nameof(Value)} = {SourceOf(Value)} " +
                "}";
    }
}
