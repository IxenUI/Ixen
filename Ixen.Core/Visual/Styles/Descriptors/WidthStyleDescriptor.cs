namespace Ixen.Core.Visual.Styles.Descriptors
{
    public class WidthStyleDescriptor : SizeStyleDescriptor
    {
        internal override string Identifier => StyleIdentifier.WIDTH;

        internal override bool CanGenerateSource => true;
        internal override string ToSource()
            => $"new {nameof(WidthStyleDescriptor)} " +
                "{ " +
                    $"{nameof(Unit)} = {nameof(SizeUnit)}.{Unit}, " +
                    $"{nameof(Value)} = {SourceOf(Value)}, " +
                    $"{nameof(Offset)} = {SourceOf(Offset)} " +
                "}";
    }
}
