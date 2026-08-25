namespace Ixen.Core.Visual.Styles.Descriptors
{
    public class MinHeightStyleDescriptor : BoundStyleDescriptor
    {
        internal override string Identifier => StyleIdentifier.MIN_HEIGHT;

        internal override bool CanGenerateSource => true;
        internal override string ToSource()
            => $"new {nameof(MinHeightStyleDescriptor)} " +
                "{ " +
                    $"{nameof(Unit)} = {nameof(SizeUnit)}.{Unit}, " +
                    $"{nameof(Value)} = {SourceOf(Value)} " +
                "}";
    }
}
