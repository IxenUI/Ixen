namespace Ixen.Core.Visual.Styles.Descriptors
{
    public enum ObjectFit
    {
        Fill,
        Contain,
        Cover,
        None,
        ScaleDown
    }

    public class ObjectFitStyleDescriptor : StyleDescriptor
    {
        internal override string Identifier => StyleIdentifier.OBJECT_FIT;

        public ObjectFit Value { get; set; } = ObjectFit.Fill;

        internal override bool CanGenerateSource => true;
        internal override string ToSource()
            => $"new {nameof(ObjectFitStyleDescriptor)} " +
                "{ " +
                    $"{nameof(Value)} = {nameof(ObjectFit)}.{Value} " +
                "}";
    }
}
