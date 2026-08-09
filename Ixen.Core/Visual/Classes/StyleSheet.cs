using System.Collections.Generic;

namespace Ixen.Core.Visual.Classes
{
    public class ClassesSet
    {
        public string Scope { get; set; }
        public List<StyleClass> Classes { get; set; }
    }

    public class StyleSheet : ClassesSet
    {
        public StyleSheet()
        {
            Classes = new List<StyleClass>();
        }

        protected void AddClass(StyleClass styleClass)
        {
            Classes.Add(styleClass);
        }
    }
}
