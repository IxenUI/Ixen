namespace Ixen.Core.Visual.Styles.Descriptors
{
    public class WidthStyleDescriptor : SizeStyleDescriptor
    {
        internal override string Identifier => StyleIdentifier.Width;

        public void Set(SizeStyleDescriptor sizeDescriptor)
        {
            Unit = sizeDescriptor.Unit;
            Value = sizeDescriptor.Value;
        }

        internal override bool CanGenerateSource => true;
        internal override string ToSource()
            => $"new {nameof(WidthStyleDescriptor)} " +
                "{ " +
                    $"{nameof(Unit)} = {nameof(SizeUnit)}.{Unit}, " +
                    $"{nameof(Value)} = {Value} " +
                "}";
    }
}
