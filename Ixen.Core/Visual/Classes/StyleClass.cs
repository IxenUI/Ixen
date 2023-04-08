using Ixen.Core.Visual.Styles;
using System.Collections.Generic;

namespace Ixen.Core.Visual.Classes
{
    internal enum StyleClassTarget
    {
        Any,
        ElementName,
        ElementType
    }

    internal class StyleClass
    {
        public StyleClassTarget Target { get; set; }
        public string Scope { get; set; }
        public string Name { get; set; }
        public List<Style> Styles { get; set; }

        public StyleClass(StyleClassTarget target, string scope, string name, List<Style> styles)
        {
            Target = target;
            Scope = scope;
            Name = name;
            Styles = styles;
        }

        public StyleClass (StyleClassTarget target, string scope, string name)
            : this (target, scope, name, new())
        { }

        public StyleClass(string scope, string name)
            : this(StyleClassTarget.Any, scope, name, new())
        { }

        public StyleClass(string scope, string name, List<Style> styles)
            : this(StyleClassTarget.Any, scope, name, styles)
        { }

        public StyleClass(string name)
            : this(StyleClassTarget.Any, null, name, new())
        { }

        public StyleClass(string name, List<Style> styles)
            : this(StyleClassTarget.Any, null, name, styles)
        { }
    }
}
