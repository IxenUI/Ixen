namespace Ixen.Core.Visual.Styles.Descriptors
{
    public class MaxWidthStyleDescriptor : BoundStyleDescriptor
    {
        internal override string Identifier => StyleIdentifier.MAX_WIDTH;

        internal override bool CanGenerateSource => true;
        internal override string ToSource()
            => $"new {nameof(MaxWidthStyleDescriptor)} " +
                "{ " +
                    $"{nameof(Unit)} = {nameof(SizeUnit)}.{Unit}, " +
                    $"{nameof(Value)} = {SourceOf(Value)} " +
                "}";
    }
}
