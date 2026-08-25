namespace Ixen.Core.Visual.Styles.Descriptors
{
    public class BoxShadowStyleDescriptor : ShadowStyleDescriptor
    {
        internal override string Identifier => StyleIdentifier.BOX_SHADOW;

        internal override bool CanGenerateSource => true;
        internal override string ToSource()
            => $"new {nameof(BoxShadowStyleDescriptor)} {{ {Fields()} }}";
    }
}
