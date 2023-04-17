namespace Ixen.Core.Visual.Styles.Descriptors
{
    public abstract class StyleDescriptor
    {
        internal abstract string Identifier { get; }

        internal virtual bool CanGenerateSource { get; } = false;
        internal virtual string ToSource() => string.Empty;
    }
}
