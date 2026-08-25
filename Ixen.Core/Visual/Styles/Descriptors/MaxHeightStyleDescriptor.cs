namespace Ixen.Core.Visual.Styles.Descriptors
{
    public class MaxHeightStyleDescriptor : BoundStyleDescriptor
    {
        internal override string Identifier => StyleIdentifier.MAX_HEIGHT;

        internal override bool CanGenerateSource => true;
        internal override string ToSource()
            => $"new {nameof(MaxHeightStyleDescriptor)} " +
                "{ " +
                    $"{nameof(Unit)} = {nameof(SizeUnit)}.{Unit}, " +
                    $"{nameof(Value)} = {SourceOf(Value)} " +
                "}";
    }
}
