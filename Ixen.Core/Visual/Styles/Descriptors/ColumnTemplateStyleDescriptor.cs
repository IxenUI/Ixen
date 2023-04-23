using System.Linq;

namespace Ixen.Core.Visual.Styles.Descriptors
{
    public class ColumnTemplateStyleDescriptor : SizeTemplateStyleDescriptor
    {
        internal override string Identifier => StyleIdentifier.ColumnTemplate;

        public void Set(SizeTemplateStyleDescriptor sizeTemplateDescriptor)
        {
            Value = sizeTemplateDescriptor.Value;
        }

        internal override bool CanGenerateSource => true;
        internal override string ToSource()
            => $"new {nameof(ColumnTemplateStyleDescriptor)} " +
                "{ " +
                    $"{nameof(Value)} = new() {{ " +
                        string.Join(", ", Value.Select(d =>
                            $"new {nameof(SizeStyleDescriptor)} " +
                            "{ " +
                                $"{nameof(SizeStyleDescriptor.Unit)} = {nameof(SizeUnit)}.{d.Unit}, " +
                                $"{nameof(SizeStyleDescriptor.Value)} = {d.Value} " +
                            "}, "
                        )) +
                    "} " +
                "}";
    }
}
