namespace Ixen.Core.Visual.Styles.Descriptors
{
    public class TextShadowStyleDescriptor : ShadowStyleDescriptor
    {
        internal override string Identifier => StyleIdentifier.TEXT_SHADOW;

        internal override bool CanGenerateSource => true;
        internal override string ToSource()
            => $"new {nameof(TextShadowStyleDescriptor)} {{ {Fields()} }}";
    }
}
