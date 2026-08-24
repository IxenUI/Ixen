namespace Ixen.Core.Visual.Styles.Descriptors
{
    public class AnchorStyleDescriptor : StyleDescriptor
    {
        internal override string Identifier => StyleIdentifier.ANCHOR;

        public string Name { get; set; }

        internal override bool CanGenerateSource => true;
        internal override string ToSource()
            => $"new {nameof(AnchorStyleDescriptor)} " +
                "{ " +
                    $"{nameof(Name)} = \"{Name}\" " +
                "}";
    }
}
