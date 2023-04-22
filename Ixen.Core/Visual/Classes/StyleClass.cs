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
        public string Scope { get; set; }
        public string Name { get; set; }
        public List<StyleDescriptor> Styles { get; set; }

        public StyleClass(StyleClassTarget target, string scope, string name, List<StyleDescriptor> styles)
        {
            Target = target;
            Scope = scope;
            Name = name;
            Styles = styles;
        }

        public StyleClass (StyleClassTarget target, string name, List<StyleDescriptor> styles)
            : this (target, null, name, styles)
        { }

        public StyleClass(StyleClassTarget target, string name)
            : this(target, null, name, new())
        { }
    }
}
