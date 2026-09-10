using System.Collections.Generic;
using System.Linq;

namespace Ixen.Core.Visual.Styles.Descriptors
{
    public class SizeTemplateStyleDescriptor : StyleDescriptor
    {
        internal override string Identifier => StyleIdentifier.SIZE_TEMPLATE;

        public List<SizeStyleDescriptor> Value { get; set; } = new();

        public bool AutoFill { get; set; }

        internal string Fields()
            => $"{nameof(Value)} = new() {{ "
                + string.Join(", ", Value.Select(d =>
                    $"new {nameof(SizeStyleDescriptor)} {{ {d.Fields()}}}"))
                + "}, "
                + $"{nameof(AutoFill)} = {(AutoFill ? "true" : "false")} ";
    }
}
