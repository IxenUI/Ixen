namespace Ixen.Core.Visual.Styles.Descriptors
{
    public class ZIndexStyleDescriptor : StyleDescriptor
    {
        internal override string Identifier => StyleIdentifier.Z_INDEX;

        public int Value { get; set; }

        internal override bool CanGenerateSource => true;
        internal override string ToSource()
            => $"new {nameof(ZIndexStyleDescriptor)} " +
                "{ " +
                    $"{nameof(Value)} = {Value} " +
                "}";
    }
}
