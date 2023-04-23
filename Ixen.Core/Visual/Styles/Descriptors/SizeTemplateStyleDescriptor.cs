using System.Collections.Generic;

namespace Ixen.Core.Visual.Styles.Descriptors
{
    public class SizeTemplateStyleDescriptor : StyleDescriptor
    {
        internal override string Identifier => StyleIdentifier.SizeTemplate;

        public List<SizeStyleDescriptor> Value { get; set; } = new();
    }
}
