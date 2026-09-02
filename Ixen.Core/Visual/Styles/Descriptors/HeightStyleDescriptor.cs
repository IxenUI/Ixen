namespace Ixen.Core.Visual.Styles.Descriptors
{
    public class HeightStyleDescriptor : SizeStyleDescriptor
    {
        internal override string Identifier => StyleIdentifier.HEIGHT;


        internal override bool CanGenerateSource => true;
        internal override string ToSource()
            => $"new {nameof(HeightStyleDescriptor)} " +
                "{ " +
                    $"{nameof(Unit)} = {nameof(SizeUnit)}.{Unit}, " +
                    $"{nameof(Value)} = {SourceOf(Value)}, " +
                    $"{nameof(Offset)} = {SourceOf(Offset)} " +
                "}";
    }
}
