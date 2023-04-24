using Ixen.Core.Visual.Styles.Descriptors;
using System.Collections.Generic;

namespace Ixen.Core.Visual.Classes
{
    public enum StyleClassTarget
    {
        ClassName,
        ElementName,
        ElementType
    }

    public class StyleClass
    {
        public StyleClassTarget Target { get; set; }
        public string SheetScope { get; set; }
        public string Scope { get; set; }
        public string Name { get; set; }
        public List<StyleDescriptor> Styles { get; set; }

        public StyleClass(StyleClassTarget target, string sheetScope, string scope, string name, List<StyleDescriptor> styles)
        {
            Target = target;
            SheetScope = sheetScope;
            Scope = scope;
            Name = name;
            Styles = styles;
        }
    }
}
