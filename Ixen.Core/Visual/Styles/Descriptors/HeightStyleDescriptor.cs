namespace Ixen.Core.Visual.Styles.Descriptors
{
    public class HeightStyleDescriptor : SizeStyleDescriptor
    {
        internal override string Identifier => StyleIdentifier.Height;

        public void Set(SizeStyleDescriptor sizeDescriptor)
        {
            Unit = sizeDescriptor.Unit;
            Value = sizeDescriptor.Value;
        }


        internal override bool CanGenerateSource => true;
        internal override string ToSource()
            => $"new {nameof(HeightStyleDescriptor)} " +
                "{ " +
                    $"{nameof(Unit)} = {nameof(SizeUnit)}.{Unit}, " +
                    $"{nameof(Value)} = {Value} " +
                "}";
    }
}
