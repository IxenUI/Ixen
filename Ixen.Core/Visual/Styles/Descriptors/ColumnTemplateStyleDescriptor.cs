namespace Ixen.Core.Visual.Styles.Descriptors
{
    public class ColumnTemplateStyleDescriptor : SizeTemplateStyleDescriptor
    {
        internal override string Identifier => StyleIdentifier.COLUMN_TEMPLATE;

        public void Set(SizeTemplateStyleDescriptor sizeTemplateDescriptor)
        {
            Value = sizeTemplateDescriptor.Value;
            AutoFill = sizeTemplateDescriptor.AutoFill;
        }

        internal override bool CanGenerateSource => true;
        internal override string ToSource()
            => $"new {nameof(ColumnTemplateStyleDescriptor)} {{ {Fields()}}}";
    }
}
