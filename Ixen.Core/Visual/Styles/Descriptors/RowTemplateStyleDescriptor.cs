namespace Ixen.Core.Visual.Styles.Descriptors
{
    public class RowTemplateStyleDescriptor : SizeTemplateStyleDescriptor
    {
        internal override string Identifier => StyleIdentifier.ROW_TEMPLATE;

        public void Set(SizeTemplateStyleDescriptor sizeTemplateDescriptor)
        {
            Value = sizeTemplateDescriptor.Value;
            AutoFill = sizeTemplateDescriptor.AutoFill;
        }

        internal override bool CanGenerateSource => true;
        internal override string ToSource()
            => $"new {nameof(RowTemplateStyleDescriptor)} {{ {Fields()}}}";
    }
}
