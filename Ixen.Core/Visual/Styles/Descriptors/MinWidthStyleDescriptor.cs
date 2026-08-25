namespace Ixen.Core.Visual.Styles.Descriptors
{
    public class MinWidthStyleDescriptor : BoundStyleDescriptor
    {
        internal override string Identifier => StyleIdentifier.MIN_WIDTH;

        internal override bool CanGenerateSource => true;
        internal override string ToSource()
            => $"new {nameof(MinWidthStyleDescriptor)} " +
                "{ " +
                    $"{nameof(Unit)} = {nameof(SizeUnit)}.{Unit}, " +
                    $"{nameof(Value)} = {SourceOf(Value)} " +
                "}";
    }
}
